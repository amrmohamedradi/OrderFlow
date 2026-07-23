using FluentAssertions;
using OrderFlow.OrderService.Domain.Entities;
using OrderFlow.OrderService.Domain.Enums;
using OrderFlow.OrderService.Domain.Exceptions;
using Xunit;

namespace OrderFlow.OrderService.UnitTests.Domain;

public class OrderTests
{

    [Fact]
    public void Create_WithValidData_ShouldReturnOrderWithPendingStatus()
    {
                const string customerName = "Alice";
        const string email = "alice@example.com";
        const decimal totalPrice = 99.99m;

                var order = Order.Create(customerName, email, totalPrice);

                order.Should().NotBeNull();
        order.Id.Should().NotBeEmpty();
        order.CustomerName.Should().Be(customerName);
        order.Email.Should().Be(email);
        order.TotalPrice.Should().Be(totalPrice);
        order.Status.Should().Be(OrderStatus.Pending);
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithEmptyOrNullCustomerName_ShouldThrowDomainException(string? customerName)
    {
                var act = () => Order.Create(customerName!, "alice@example.com", 10m);

                act.Should().Throw<DomainException>()
           .WithMessage("*Customer name is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithEmptyOrNullEmail_ShouldThrowDomainException(string? email)
    {
                var act = () => Order.Create("Alice", email!, 10m);

                act.Should().Throw<DomainException>()
           .WithMessage("*Email is required*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.5)]
    public void Create_WithNonPositiveTotalPrice_ShouldThrowDomainException(decimal totalPrice)
    {
                var act = () => Order.Create("Alice", "alice@example.com", totalPrice);

                act.Should().Throw<DomainException>()
           .WithMessage("*Total price must be greater than zero*");
    }

    [Fact]
    public void Confirm_WhenPending_ShouldChangeStatusToConfirmed()
    {
                var order = Order.Create("Alice", "alice@example.com", 10m);

                order.Confirm();

                order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldThrowDomainException()
    {
                var order = Order.Create("Alice", "alice@example.com", 10m);
        order.Confirm();

                var act = () => order.Confirm();

                act.Should().Throw<DomainException>()
           .WithMessage("*confirmed*pending*");
    }

    [Fact]
    public void Confirm_WhenCancelled_ShouldThrowDomainException()
    {
                var order = Order.Create("Alice", "alice@example.com", 10m);
        order.Cancel();

                var act = () => order.Confirm();

                act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_WhenPending_ShouldChangeStatusToCancelled()
    {
                var order = Order.Create("Alice", "alice@example.com", 10m);

                order.Cancel();

                order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenConfirmed_ShouldThrowDomainException()
    {
                var order = Order.Create("Alice", "alice@example.com", 10m);
        order.Confirm();

                var act = () => order.Cancel();

                act.Should().Throw<DomainException>()
           .WithMessage("*cancelled*Pending*");
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrowDomainException()
    {
                var order = Order.Create("Alice", "alice@example.com", 10m);
        order.Cancel();

                var act = () => order.Cancel();

                act.Should().Throw<DomainException>();
    }
}
