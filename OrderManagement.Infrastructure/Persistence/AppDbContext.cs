using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence.Seed;

namespace OrderManagement.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        AppDbContextSeed.Seed(modelBuilder);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            modelBuilder.Entity<Order>()
                .Property(x => x.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedNever()
                .IsRequired();
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateSqliteRowVersions();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateSqliteRowVersions()
    {
        if (Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            return;
        }

        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Order &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var order = (Order)entry.Entity;
            order.RowVersion = RandomNumberGenerator.GetBytes(8);
        }
    }
}