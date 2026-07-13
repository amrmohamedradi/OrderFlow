namespace OrderFlow.BuildingBlocks.Contracts.Events;

public sealed record OrderCreatedEvent(
    Guid OrderId,
    string CustomerName,
    string Email,
    decimal TotalPrice,
    DateTime CreatedAt);
