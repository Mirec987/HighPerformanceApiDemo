using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence.Configurations;

public class CustomerConfig : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.Email)
            .HasMaxLength(200)
            .IsRequired();

        entity.HasIndex(x => x.Email)
            .IsUnique();
    }
}