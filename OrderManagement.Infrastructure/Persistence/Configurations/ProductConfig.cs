using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Sku)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.Price)
            .HasColumnType("decimal(18,2)");

        entity.HasIndex(x => x.Sku)
            .IsUnique();
    }
}