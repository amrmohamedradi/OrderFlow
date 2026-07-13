
namespace OrderFlow.OrderService.Application.Features.Orders.Commands.CreateOrder;

public sealed record CreateOrderResponse(
    Guid Id,
    string CustomerName,
    string Email,
    decimal TotalPrice,
    string Status,
    DateTime CreatedAt);
