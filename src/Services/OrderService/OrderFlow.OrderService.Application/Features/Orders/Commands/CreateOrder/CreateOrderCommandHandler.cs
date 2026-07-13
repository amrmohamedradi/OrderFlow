using MediatR;
using Microsoft.Extensions.Logging;
using OrderFlow.OrderService.Application.Interfaces.Persistence;
using OrderFlow.OrderService.Domain.Entities;
using OrderFlow.OrderService.Application.Interfaces.Repositories;
using OrderFlow.BuildingBlocks.Contracts.Events;
using MassTransit;

namespace OrderFlow.OrderService.Application.Features.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling CreateOrderCommand. CustomerName: {CustomerName}, Email: {Email}, TotalPrice: {TotalPrice}",
            request.CustomerName, request.Email, request.TotalPrice);

        var order = Order.Create(request.CustomerName, request.Email, request.TotalPrice);
        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order saved. OrderId: {OrderId}. Publishing OrderCreatedEvent...",
            order.Id);

        await _publishEndpoint.Publish(
            new OrderCreatedEvent(order.Id, order.CustomerName, order.Email, order.TotalPrice, order.CreatedAt),
            cancellationToken);

        _logger.LogInformation(
            "OrderCreatedEvent published successfully. OrderId: {OrderId}",
            order.Id);

        return new CreateOrderResponse(order.Id, order.CustomerName, order.Email, order.TotalPrice, order.Status.ToString(), order.CreatedAt);
    }
}
