using OrderFlow.OrderService.Domain.Enums;
using OrderFlow.OrderService.Domain.Common;
using OrderFlow.OrderService.Domain.Exceptions;
namespace OrderFlow.OrderService.Domain.Entities;

public class Order : BaseEntity
{
    public string CustomerName  { get; private set; } = default!;

    public string Email { get; private set; } = default!;

    public decimal TotalPrice { get; private set; }

    public OrderStatus Status { get; private set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    
    private Order(){}

    private Order(string customerName , string email , decimal totalPrice)
    {
        CustomerName = customerName;
        Email = email;
        TotalPrice = totalPrice;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public static Order Create(string customerName , string email , decimal totalPrice)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Customer name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        if (totalPrice <= 0)
            throw new DomainException("Total price must be greater than zero.");
        return new Order(customerName , email , totalPrice);
    }

    public void Confirm()
    {
        if(Status != OrderStatus.Pending)
            throw new DomainException("Order can only be confirmed if it is in pending state.");
        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if(Status != OrderStatus.Pending)
            throw new DomainException("Order can only be cancelled if it is in Pending state.");
        Status = OrderStatus.Cancelled;
    }
}
