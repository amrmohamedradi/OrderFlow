using MassTransit;
using Microsoft.Extensions.Logging;
using OrderFlow.BuildingBlocks.Messaging.Correlation;
using OrderFlow.OrderService.Application.Interfaces.Messaging;

namespace OrderFlow.OrderService.Infrastructure.Messaging;

public class EventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(
        IPublishEndpoint publishEndpoint,
        ICorrelationIdAccessor correlationIdAccessor,
        ILogger<EventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var correlationId = _correlationIdAccessor.GetCorrelationId();
            if (correlationId.HasValue)
            {
                await _publishEndpoint.Publish(message, context =>
                {
                    context.CorrelationId = correlationId.Value;
                }, cancellationToken);
            }
            else
            {
                await _publishEndpoint.Publish(message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event: {EventName}", message?.GetType().Name);
            throw;
        }
    }
}
