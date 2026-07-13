using OrderFlow.OrderService.Domain.Entities;
namespace OrderFlow.OrderService.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

}
