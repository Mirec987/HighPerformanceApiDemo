namespace OrderManagement.Application.Orders.DTOs
{
    public class UpdateOrderRequest
    {
        public string Status { get; set; } = null!;
        public string RowVersion { get; set; } = null!;
    }
}