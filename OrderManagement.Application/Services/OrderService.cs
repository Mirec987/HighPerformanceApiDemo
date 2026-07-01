using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Caching;
using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Orders.Caching;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Services.Interfaces;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Services;

public class OrderService : IOrderService
{
    private readonly IAppDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IValidator<CreateOrderRequest> _validator;
    private readonly IValidator<UpdateOrderRequest> _updateOrderValidator;

    public OrderService(
        IAppDbContext dbContext,
        ICacheService cache,
        IValidator<CreateOrderRequest> validator,
        IValidator<UpdateOrderRequest> updateOrderValidator)
    {
        _dbContext = dbContext;
        _cache = cache;
        _validator = validator;
        _updateOrderValidator = updateOrderValidator;
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var customerExists = await _dbContext.Customers
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.CustomerId, ct);

        if (!customerExists)
        {
            throw new ArgumentException("Customer does not exist.");
        }

        var productIds = request.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, ct);

        if (products.Count != productIds.Count)
        {
            throw new ArgumentException("One or more products do not exist or are inactive.");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CreatedAtUtc = DateTime.UtcNow,
            Status = OrderStatus.Draft
        };

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }

        order.TotalAmount = order.Items.Sum(x => x.Quantity * x.UnitPrice);
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(ct);

        _cache.IncrementVersion(OrderCacheKeys.ListVersion);

        return new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString()
        };
    }

    public async Task<PagedResponse<OrderResponse>> GetAllAsync(GetOrdersRequest request, CancellationToken ct)
    {
        var normalizedRequest = NormalizePaging(request);
        var version = _cache.GetVersion(OrderCacheKeys.ListVersion);
        var key = OrderCacheKeys.List(normalizedRequest, version);

        return await _cache.GetOrCreateAsync(
            key,
            async token => (PagedResponse<OrderResponse>?)await LoadOrdersAsync(normalizedRequest, token),
            CachePolicy.OrderList,
            ct) ?? throw new InvalidOperationException("Order list cache factory returned null.");
    }

    public Task<OrderDetailResponse?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _cache.GetOrCreateAsync(
            OrderCacheKeys.Detail(id),
            token => LoadOrderDetailAsync(id, token),
            CachePolicy.OrderDetail,
            ct);

    public async Task<OrderResponse> UpdateOrderAsync(Guid id, UpdateOrderRequest request, CancellationToken ct)
    {
        var validationResult = await _updateOrderValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var order = await _dbContext.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (order is null)
        {
            throw new ArgumentException("Order does not exist.");
        }

        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
        {
            throw new ArgumentException("Invalid order status.");
        }

        _dbContext.Entry(order).Property(x => x.RowVersion).OriginalValue =
            Convert.FromBase64String(request.RowVersion);
        order.Status = newStatus;
        await _dbContext.SaveChangesAsync(ct);

        _cache.Remove(OrderCacheKeys.Detail(id));
        _cache.IncrementVersion(OrderCacheKeys.ListVersion);

        return new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString()
        };
    }

    private async Task<PagedResponse<OrderResponse>> LoadOrdersAsync(
        GetOrdersRequest request,
        CancellationToken ct)
    {
        var query = _dbContext.Orders.AsNoTracking().AsQueryable();

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<OrderStatus>(request.Status, true, out var status))
        {
            query = query.Where(x => x.Status == status);
        }

        query = ((request.SortBy ?? string.Empty).ToLowerInvariant(),
            (request.SortDirection ?? string.Empty).ToLowerInvariant()) switch
        {
            ("totalamount", "asc") => query.OrderBy(x => x.TotalAmount),
            ("totalamount", "desc") => query.OrderByDescending(x => x.TotalAmount),
            ("ordernumber", "asc") => query.OrderBy(x => x.OrderNumber),
            ("ordernumber", "desc") => query.OrderByDescending(x => x.OrderNumber),
            ("createdat", "asc") => query.OrderBy(x => x.CreatedAtUtc),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };

        var totalCount = await query.CountAsync(ct);
        var data = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new { x.Id, x.OrderNumber, x.TotalAmount, x.Status })
            .ToListAsync(ct);

        return new PagedResponse<OrderResponse>
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            Items = data.Select(x => new OrderResponse
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                TotalAmount = x.TotalAmount,
                Status = x.Status.ToString()
            }).ToList()
        };
    }

    private async Task<OrderDetailResponse?> LoadOrderDetailAsync(Guid id, CancellationToken ct)
    {
        var data = await _dbContext.Orders
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.OrderNumber,
                x.Status,
                x.TotalAmount,
                x.CreatedAtUtc,
                x.RowVersion,
                Customer = new { x.Customer.Id, x.Customer.FirstName, x.Customer.LastName, x.Customer.Email },
                Items = x.Items.Select(i => new
                {
                    i.ProductId,
                    ProductName = i.Product.Name,
                    i.Quantity,
                    i.UnitPrice
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (data is null)
        {
            return null;
        }

        return new OrderDetailResponse
        {
            Id = data.Id,
            OrderNumber = data.OrderNumber,
            Status = data.Status.ToString(),
            TotalAmount = data.TotalAmount,
            CreatedAtUtc = data.CreatedAtUtc,
            RowVersion = Convert.ToBase64String(data.RowVersion),
            Customer = new CustomerResponse
            {
                Id = data.Customer.Id,
                FullName = $"{data.Customer.FirstName} {data.Customer.LastName}",
                Email = data.Customer.Email
            },
            Items = data.Items.Select(i => new OrderItemDetailResponse
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.Quantity * i.UnitPrice
            }).ToList()
        };
    }

    private static GetOrdersRequest NormalizePaging(GetOrdersRequest request) => new()
    {
        CustomerId = request.CustomerId,
        Status = request.Status,
        SortBy = request.SortBy,
        SortDirection = request.SortDirection,
        Page = request.Page <= 0 ? 1 : request.Page,
        PageSize = Math.Clamp(request.PageSize <= 0 ? 20 : request.PageSize, 1, 100)
    };
}
