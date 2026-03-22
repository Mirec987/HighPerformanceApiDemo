using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = default!;
    public Customer Customer { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();
}