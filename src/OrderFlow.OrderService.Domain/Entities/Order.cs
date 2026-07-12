using OrderFlow.OrderService.Domain.Enums;
namespace OrderFlow.OrderService.Domain.Entities;

public class Order
{
    public string CustomerName  { get; set; } = default!;

    public string Email { get; set; } = default!;

    public decimal TotalPrice { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
