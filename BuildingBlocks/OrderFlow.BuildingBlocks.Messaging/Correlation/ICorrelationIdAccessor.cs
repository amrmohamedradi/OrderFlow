namespace OrderFlow.BuildingBlocks.Messaging.Correlation;

public interface ICorrelationIdAccessor
{
    Guid? GetCorrelationId();
    void SetCorrelationId(Guid? correlationId);
}
