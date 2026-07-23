using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderFlow.BuildingBlocks.Contracts.Events;
using OrderFlow.NotificationService.Application.Consumers;
using OrderFlow.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using Xunit;

namespace OrderFlow.NotificationService.UnitTests.Consumers;

public class OrderCreateConsumerTests : IAsyncLifetime
{
    private ITestHarness _harness = null!;
    private ISender _sender = null!;
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        _sender = Substitute.For<ISender>();

        var services = new ServiceCollection();

        services.AddSingleton(_sender);
        services.AddSingleton(Substitute.For<ILogger<OrderCreateConsumer>>());

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrderCreateConsumer>();
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task Consume_WhenOrderCreatedEventReceived_ShouldDispatchCreateNotificationCommand()
    {
                var orderId = Guid.NewGuid();
        var email = "alice@example.com";
        var @event = new OrderCreatedEvent(orderId, "Alice", email, 99.99m, DateTime.UtcNow);

                await _harness.Bus.Publish(@event);

                (await _harness.Consumed.Any<OrderCreatedEvent>()).Should().BeTrue();

                await _sender.Received(1).Send(
            Arg.Any<CreateNotificationCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WhenOrderCreatedEventReceived_ShouldPassCorrectOrderIdAndEmailToCommand()
    {
                var orderId = Guid.NewGuid();
        const string email = "bob@example.com";
        var @event = new OrderCreatedEvent(orderId, "Bob", email, 50m, DateTime.UtcNow);

                await _harness.Bus.Publish(@event);

                (await _harness.Consumed.Any<OrderCreatedEvent>()).Should().BeTrue();

                await _sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(cmd =>
                cmd.OrderId == orderId &&
                cmd.Email == email),
            Arg.Any<CancellationToken>());
    }
}
