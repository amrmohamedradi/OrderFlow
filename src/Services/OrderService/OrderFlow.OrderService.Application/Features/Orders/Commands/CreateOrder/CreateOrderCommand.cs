using MediatR;
namespace OrderFlow.OrderService.Application.Features.Orders.Commands.CreateOrder;

public sealed record CreateOrderCommand(string CustomerName,string Email, decimal TotalPrice) : IRequest<CreateOrderResponse>;

