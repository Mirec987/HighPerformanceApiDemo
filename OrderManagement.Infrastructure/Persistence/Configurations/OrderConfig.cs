using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class OrderConfig : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.OrderNumber)
            .HasMaxLength(50)
            .IsRequired();

        entity.HasIndex(x => x.OrderNumber)
            .IsUnique();

        entity.HasIndex(x => x.CreatedAtUtc);

        entity.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc });

        entity.HasIndex(x => new { x.Status, x.CreatedAtUtc });

        entity.Property(x => x.TotalAmount)
            .HasColumnType("decimal(18,2)");

        entity.Property(x => x.RowVersion)
            .IsConcurrencyToken()
            .IsRequired();

        entity.HasOne(x => x.Customer)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(x => x.Items)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId);
    }
}