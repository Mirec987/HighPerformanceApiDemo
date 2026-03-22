using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Abstractions.Persistence;

public interface IAppDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Product> Products { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}