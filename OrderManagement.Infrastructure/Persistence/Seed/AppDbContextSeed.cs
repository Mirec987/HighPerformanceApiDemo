using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Seed;

public static class AppDbContextSeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        var customer1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var customer2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var product1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var product2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var product3Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        modelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = customer1Id,
                FirstName = "Ján",
                LastName = "Novák",
                Email = "jan.novak@example.com"
            },
            new Customer
            {
                Id = customer2Id,
                FirstName = "Petra",
                LastName = "Kováčová",
                Email = "petra.kovacova@example.com"
            });

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = product1Id,
                Name = "Laptop",
                Sku = "LAP-001",
                Price = 1200,
                IsActive = true
            },
            new Product
            {
                Id = product2Id,
                Name = "Mouse",
                Sku = "MOU-001",
                Price = 25,
                IsActive = true
            },
            new Product
            {
                Id = product3Id,
                Name = "Keyboard",
                Sku = "KEY-001",
                Price = 80,
                IsActive = true
            });
    }
}