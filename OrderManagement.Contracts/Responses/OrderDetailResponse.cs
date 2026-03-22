namespace OrderManagement.Contracts.Responses;

public class OrderDetailResponse
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string RowVersion { get; set; } = null!;
    public CustomerResponse Customer { get; set; } = null!;
    public List<OrderItemDetailResponse> Items { get; set; } = new();
}

public class CustomerResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
}

public class OrderItemDetailResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string RowVersion { get; set; } = null!;
}