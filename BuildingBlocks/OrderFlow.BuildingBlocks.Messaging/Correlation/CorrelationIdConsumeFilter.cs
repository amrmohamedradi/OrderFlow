using MassTransit;

namespace OrderFlow.BuildingBlocks.Messaging.Correlation;

public class CorrelationIdConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public CorrelationIdConsumeFilter(ICorrelationIdAccessor correlationIdAccessor)
    {
        _correlationIdAccessor = correlationIdAccessor;
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("correlationIdConsumeFilter");

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var correlationId = context.CorrelationId ?? _correlationIdAccessor.GetCorrelationId() ?? Guid.NewGuid();
        _correlationIdAccessor.SetCorrelationId(correlationId);

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next.Send(context);
        }
    }
}
