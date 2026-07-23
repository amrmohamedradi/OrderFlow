using FluentAssertions;
using OrderFlow.OrderService.Domain.Entities;
using OrderFlow.OrderService.Domain.Enums;
using OrderFlow.OrderService.Domain.Exceptions;
using Xunit;

namespace OrderFlow.OrderService.UnitTests.Domain;

public class OrderTests
{
    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldReturnOrderWithPendingStatus()
    {
        // Arrange
        const string customerName = "Alice";
        const string email = "alice@example.com";
        const decimal totalPrice = 99.99m;

        // Act
        var order = Order.Create(customerName, email, totalPrice);

        // Assert
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
        // Act
        var act = () => Order.Create(customerName!, "alice@example.com", 10m);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*Customer name is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithEmptyOrNullEmail_ShouldThrowDomainException(string? email)
    {
        // Act
        var act = () => Order.Create("Alice", email!, 10m);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*Email is required*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.5)]
    public void Create_WithNonPositiveTotalPrice_ShouldThrowDomainException(decimal totalPrice)
    {
        // Act
        var act = () => Order.Create("Alice", "alice@example.com", totalPrice);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*Total price must be greater than zero*");
    }

    // ── Confirm ───────────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_WhenPending_ShouldChangeStatusToConfirmed()
    {
        // Arrange
        var order = Order.Create("Alice", "alice@example.com", 10m);

        // Act
        order.Confirm();

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create("Alice", "alice@example.com", 10m);
        order.Confirm();

        // Act
        var act = () => order.Confirm();

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*confirmed*pending*");
    }

    [Fact]
    public void Confirm_WhenCancelled_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create("Alice", "alice@example.com", 10m);
        order.Cancel();

        // Act
        var act = () => order.Confirm();

        // Assert
        act.Should().Throw<DomainException>();
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_WhenPending_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var order = Order.Create("Alice", "alice@example.com", 10m);

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenConfirmed_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create("Alice", "alice@example.com", 10m);
        order.Confirm();

        // Act
        var act = () => order.Cancel();

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*cancelled*Pending*");
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrowDomainException()
    {
        // Arrange
        var order = Order.Create("Alice", "alice@example.com", 10m);
        order.Cancel();

        // Act
        var act = () => order.Cancel();

        // Assert
        act.Should().Throw<DomainException>();
    }
}
