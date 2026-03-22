namespace OrderManagement.Contracts.Responses
{
    public class OrderResponse
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
    }
}