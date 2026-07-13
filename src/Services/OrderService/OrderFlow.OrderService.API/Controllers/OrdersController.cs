using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.OrderService.Application.Features.Orders.Commands.CreateOrder;

namespace OrderFlow.OrderService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> PostOrder(CreateOrderCommand command)
    {
        var response = await _sender.Send(command);
        return Created($"api/orders/{response.Id}", response);
    }
}
