using Microsoft.EntityFrameworkCore;
using OrderFlow.OrderService.Domain.Entities;


namespace OrderFlow.OrderService.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options): base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
