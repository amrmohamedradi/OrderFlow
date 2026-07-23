using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OrderFlow.BuildingBlocks.Contracts.Events;
using OrderFlow.OrderService.Application.Features.Orders.Commands.CreateOrder;
using OrderFlow.OrderService.Application.Interfaces.Messaging;
using OrderFlow.OrderService.Application.Interfaces.Persistence;
using OrderFlow.OrderService.Application.Interfaces.Repositories;
using OrderFlow.OrderService.Domain.Entities;
using OrderFlow.OrderService.Domain.Enums;
using Xunit;

namespace OrderFlow.OrderService.UnitTests.Application;

public class CreateOrderCommandHandlerTests
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _logger = Substitute.For<ILogger<CreateOrderCommandHandler>>();

        _handler = new CreateOrderCommandHandler(
            _orderRepository,
            _unitOfWork,
            _eventPublisher,
            _logger);
    }

    [Fact]
    public async Task Handle_WhenOrderIsValid_ShouldAddOrderToRepository()
    {
                var command = new CreateOrderCommand("Alice", "alice@example.com", 99.99m);

                await _handler.Handle(command, CancellationToken.None);

                await _orderRepository.Received(1).AddAsync(
            Arg.Is<Order>(o =>
                o.CustomerName == "Alice" &&
                o.Email == "alice@example.com" &&
                o.TotalPrice == 99.99m &&
                o.Status == OrderStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOrderIsValid_ShouldPublishOrderCreatedEvent()
    {
                var command = new CreateOrderCommand("Bob", "bob@example.com", 50m);

                await _handler.Handle(command, CancellationToken.None);

                await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<OrderCreatedEvent>(e =>
                e.CustomerName == "Bob" &&
                e.Email == "bob@example.com" &&
                e.TotalPrice == 50m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOrderIsValid_ShouldCallSaveChanges()
    {
                var command = new CreateOrderCommand("Carol", "carol@example.com", 25m);

                await _handler.Handle(command, CancellationToken.None);

                await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOrderIsValid_ShouldReturnResponseWithCorrectData()
    {
                var command = new CreateOrderCommand("Dave", "dave@example.com", 150m);

                var response = await _handler.Handle(command, CancellationToken.None);

                response.Should().NotBeNull();
        response.Id.Should().NotBeEmpty();
        response.CustomerName.Should().Be("Dave");
        response.Email.Should().Be("dave@example.com");
        response.TotalPrice.Should().Be(150m);
        response.Status.Should().Be(OrderStatus.Pending.ToString());
    }

    [Fact]
    public async Task Handle_WhenOrderIsValid_ShouldPublishEventBeforeSaving()
    {
                var callSequence = new List<string>();

        _eventPublisher
            .PublishAsync(Arg.Any<OrderCreatedEvent>(), Arg.Any<CancellationToken>())
            .Returns(ci => { callSequence.Add("publish"); return Task.CompletedTask; });

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => { callSequence.Add("save"); return Task.FromResult(1); });

        var command = new CreateOrderCommand("Eve", "eve@example.com", 75m);

                await _handler.Handle(command, CancellationToken.None);

                callSequence.Should().ContainInOrder("publish", "save");
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldPropagateException()
    {
                _orderRepository
            .AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        var command = new CreateOrderCommand("Frank", "frank@example.com", 10m);

                var act = () => _handler.Handle(command, CancellationToken.None);

                await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("DB unavailable");
    }

    [Fact]
    public async Task Handle_WhenEventPublisherThrows_ShouldPropagateExceptionAndNotSave()
    {
                _eventPublisher
            .PublishAsync(Arg.Any<OrderCreatedEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Outbox unavailable"));

        var command = new CreateOrderCommand("Grace", "grace@example.com", 10m);

                var act = () => _handler.Handle(command, CancellationToken.None);

                await act.Should().ThrowAsync<InvalidOperationException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
