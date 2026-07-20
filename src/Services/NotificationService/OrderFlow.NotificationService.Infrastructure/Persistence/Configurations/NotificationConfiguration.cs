using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using OrderFlow.NotificationService.Domain.Entities;

namespace OrderFlow.NotificationService.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).IsRequired();
        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.Property(x => x.Email).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<string>();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.ToTable("Notifications");
    }
}
