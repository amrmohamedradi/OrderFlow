using MassTransit;

namespace OrderFlow.BuildingBlocks.Messaging.Correlation;

public class CorrelationIdPublishFilter<T> : IFilter<PublishContext<T>> where T : class
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public CorrelationIdPublishFilter(ICorrelationIdAccessor correlationIdAccessor)
    {
        _correlationIdAccessor = correlationIdAccessor;
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("correlationIdPublishFilter");

    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        if (!context.CorrelationId.HasValue)
        {
            var correlationId = _correlationIdAccessor.GetCorrelationId();
            if (correlationId.HasValue)
            {
                context.CorrelationId = correlationId.Value;
            }
        }
        return next.Send(context);
    }
}
