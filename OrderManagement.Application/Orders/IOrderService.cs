using OrderManagement.Contracts.Requests;
using OrderManagement.Contracts.Responses;

namespace OrderManagement.Application.Orders;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken ct);

    Task<PagedResponse<OrderResponse>> GetAllAsync(GetOrdersRequest request, CancellationToken ct);

    Task<OrderDetailResponse?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<OrderResponse> UpdateOrderAsync(Guid id, UpdateOrderRequest request, CancellationToken ct);
}