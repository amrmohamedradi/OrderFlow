using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.OrderService.Domain.Entities;

namespace OrderFlow.OrderService.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerName)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);
        builder.Property(x => x.TotalPrice)
            .HasPrecision(18, 2);
        builder.Property(x => x.Status)
            .HasConversion<string>();
        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}
