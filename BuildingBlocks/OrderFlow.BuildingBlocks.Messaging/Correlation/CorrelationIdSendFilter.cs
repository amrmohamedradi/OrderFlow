using MassTransit;

namespace OrderFlow.BuildingBlocks.Messaging.Correlation;

public class CorrelationIdSendFilter<T> : IFilter<SendContext<T>> where T : class
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public CorrelationIdSendFilter(ICorrelationIdAccessor correlationIdAccessor)
    {
        _correlationIdAccessor = correlationIdAccessor;
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("correlationIdSendFilter");

    public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
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
