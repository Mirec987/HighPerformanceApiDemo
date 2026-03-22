using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class OrderItemConfig : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.UnitPrice)
            .HasColumnType("decimal(18,2)");

        entity.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId);

        entity.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}