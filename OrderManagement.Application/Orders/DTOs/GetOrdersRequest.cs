namespace OrderManagement.Application.Orders.DTOs;

public class GetOrdersRequest
{
    public Guid? CustomerId { get; set; }
    public string? Status { get; set; }

    public string SortBy { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}