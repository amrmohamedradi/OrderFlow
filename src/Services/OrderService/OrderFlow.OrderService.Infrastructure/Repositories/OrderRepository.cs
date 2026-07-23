using OrderFlow.OrderService.Domain.Entities;
using OrderFlow.OrderService.Infrastructure.Persistence;
using OrderFlow.OrderService.Application.Interfaces.Repositories;
namespace OrderFlow.OrderService.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }
}
