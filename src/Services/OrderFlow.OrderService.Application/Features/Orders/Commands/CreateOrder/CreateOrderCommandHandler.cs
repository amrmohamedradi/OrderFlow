using MediatR;
using OrderFlow.OrderService.Application.Interfaces.Persistence;
using OrderFlow.OrderService.Domain.Entities;
using OrderFlow.OrderService.Application.Interfaces.Repositories;

namespace OrderFlow.OrderService.Application.Features.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateOrderCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateOrderResponse> Handle (CreateOrderCommand request,CancellationToken cancellationToken)
    {
        var order = Order.Create(request.CustomerName, request.Email, request.TotalPrice);
        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateOrderResponse(order.Id, order.CustomerName, order.Email, order.TotalPrice, order.Status.ToString(), order.CreatedAt);
    }
}
