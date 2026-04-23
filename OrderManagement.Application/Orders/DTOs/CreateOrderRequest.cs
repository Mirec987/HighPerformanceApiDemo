using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Application.Orders.DTOs;

public class CreateOrderRequest
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}