namespace OrderManagement.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;

    public List<Order> Orders { get; set; } = new();
}