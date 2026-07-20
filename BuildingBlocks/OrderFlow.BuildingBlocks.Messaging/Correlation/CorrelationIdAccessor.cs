namespace OrderFlow.BuildingBlocks.Messaging.Correlation;

public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<Guid?> _correlationId = new();

    public Guid? GetCorrelationId() => _correlationId.Value;

    public void SetCorrelationId(Guid? correlationId) => _correlationId.Value = correlationId;
}
