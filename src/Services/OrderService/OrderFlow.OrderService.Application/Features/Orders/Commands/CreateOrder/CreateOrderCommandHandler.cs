using MediatR;
using Microsoft.Extensions.Logging;
using OrderFlow.OrderService.Application.Interfaces.Persistence;
using OrderFlow.OrderService.Domain.Entities;
using OrderFlow.OrderService.Application.Interfaces.Repositories;
using OrderFlow.BuildingBlocks.Contracts.Events;
using OrderFlow.OrderService.Application.Interfaces.Messaging;

namespace OrderFlow.OrderService.Application.Features.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling CreateOrderCommand. CustomerName: {CustomerName}, Email: {Email}, TotalPrice: {TotalPrice}",
            request.CustomerName, request.Email, request.TotalPrice);

        var order = Order.Create(request.CustomerName, request.Email, request.TotalPrice);
        await _orderRepository.AddAsync(order, cancellationToken);

        await _eventPublisher.PublishAsync(
            new OrderCreatedEvent(order.Id, order.CustomerName, order.Email, order.TotalPrice, order.CreatedAt),
            cancellationToken);

        _logger.LogInformation(
            "OrderCreatedEvent published successfully. OrderId: {OrderId}",
            order.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order and Outbox event saved successfully. OrderId: {OrderId}",
            order.Id);

        return new CreateOrderResponse(order.Id, order.CustomerName, order.Email, order.TotalPrice, order.Status.ToString(), order.CreatedAt);
    }
}
