using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Contracts.Requests;
using OrderManagement.Contracts.Responses;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Orders;

public class OrderService : IOrderService
{
    private readonly IAppDbContext _dbContext;

    private readonly IValidator<CreateOrderRequest> _validator;

    private readonly IValidator<UpdateOrderRequest> _updateOrderValidator;

    public OrderService(IAppDbContext dbContext, IValidator<CreateOrderRequest> validator, IValidator<UpdateOrderRequest> updateOrderRequest)
    {
        _dbContext = dbContext;
        _validator = validator;
        _updateOrderValidator = updateOrderRequest;
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(request, ct);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        //if (request.CustomerId == Guid.Empty)
        //    throw new ArgumentException("CustomerId is required.");

        //if (request.Items is null || request.Items.Count == 0)
        //    throw new ArgumentException("Order must contain at least one item.");

        //if (request.Items.Any(x => x.ProductId == Guid.Empty))
        //    throw new ArgumentException("Each item must contain ProductId.");

        //if (request.Items.Any(x => x.Quantity <= 0))
        //    throw new ArgumentException("Quantity must be greater than 0.");

        var customerExists = await _dbContext.Customers
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.CustomerId, ct);

        if (!customerExists)
            throw new ArgumentException("Customer does not exist.");

        var productIds = request.Items
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, ct);

        if (products.Count != productIds.Count)
            throw new ArgumentException("One or more products do not exist or are inactive.");

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
        if (request.Page <= 0)
            request.Page = 1;

        if (request.PageSize <= 0)
            request.PageSize = 20;

        if (request.PageSize > 100)
            request.PageSize = 100;

        var query = _dbContext.Orders
            .AsNoTracking()
            .AsQueryable();

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<OrderStatus>(request.Status, true, out var status))
        {
            query = query.Where(x => x.Status == status);
        }

        query = (request.SortBy.ToLower(), request.SortDirection.ToLower()) switch
        {
            ("totalamount", "asc") => query.OrderBy(x => x.TotalAmount),
            ("totalamount", "desc") => query.OrderByDescending(x => x.TotalAmount),
            ("ordernumber", "asc") => query.OrderBy(x => x.OrderNumber),
            ("ordernumber", "desc") => query.OrderByDescending(x => x.OrderNumber),
            ("createdat", "asc") => query.OrderBy(x => x.CreatedAtUtc),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new OrderResponse
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                TotalAmount = x.TotalAmount,
                Status = x.Status.ToString()
            })
            .ToListAsync(ct);

        return new PagedResponse<OrderResponse>
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<OrderDetailResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new OrderDetailResponse
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                Status = x.Status.ToString(),
                TotalAmount = x.TotalAmount,
                CreatedAtUtc = x.CreatedAtUtc,
                RowVersion = Convert.ToBase64String(x.RowVersion),
                Customer = new CustomerResponse
                {
                    Id = x.Customer.Id,
                    FullName = x.Customer.FirstName + " " + x.Customer.LastName,
                    Email = x.Customer.Email
                },
                Items = x.Items.Select(i => new OrderItemDetailResponse
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.Quantity * i.UnitPrice
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<OrderResponse> UpdateOrderAsync(Guid id, UpdateOrderRequest request, CancellationToken ct)
    {
        var validationResult = await _updateOrderValidator.ValidateAsync(request, ct);

        if (!validationResult.IsValid)
            throw new FluentValidation.ValidationException(validationResult.Errors);

        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (order is null)
            throw new ArgumentException("Order does not exist.");

        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
            throw new ArgumentException("Invalid order status.");

        _dbContext.Entry(order).Property(x => x.RowVersion).OriginalValue =
            Convert.FromBase64String(request.RowVersion);

        order.Status = newStatus;

        await _dbContext.SaveChangesAsync(ct);

        //_logger.LogInformation(
        //    "Order {OrderId} status changed to {Status}",
        //    order.Id,
        //    order.Status);

        return new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString()
        };
    }
}