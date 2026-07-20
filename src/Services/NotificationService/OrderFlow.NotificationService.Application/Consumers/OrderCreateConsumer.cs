using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderFlow.BuildingBlocks.Contracts.Events;
using OrderFlow.NotificationService.Application.Features.Notifications.Commands.CreateNotification;

namespace OrderFlow.NotificationService.Application.Consumers;

public class OrderCreateConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<OrderCreateConsumer> _logger;

    public OrderCreateConsumer(ISender sender, ILogger<OrderCreateConsumer> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var correlationId = context.CorrelationId ?? Guid.NewGuid();
        _logger.LogInformation(
            "Received OrderCreatedEvent. CorrelationId: {CorrelationId}, OrderId: {OrderId}, Email: {Email}",
            correlationId, context.Message.OrderId, context.Message.Email);

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            try
            {
                var command = new CreateNotificationCommand(context.Message.OrderId, context.Message.Email);
                await _sender.Send(command);

                _logger.LogInformation(
                    "CreateNotificationCommand dispatched successfully for OrderId: {OrderId}",
                    context.Message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to process OrderCreatedEvent for OrderId: {OrderId}. Retry will be attempted.",
                    context.Message.OrderId);
                throw;
            }
        }
    }
}
