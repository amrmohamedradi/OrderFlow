using FluentAssertions;
using FluentValidation.TestHelper;
using OrderFlow.OrderService.Application.Features.Orders.Commands.CreateOrder;
using Xunit;

namespace OrderFlow.OrderService.UnitTests.Application;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPassValidation()
    {
        // Arrange
        var command = new CreateOrderCommand("Alice", "alice@example.com", 99.99m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespaceCustomerName_ShouldFailValidation(string customerName)
    {
        // Arrange
        var command = new CreateOrderCommand(customerName, "alice@example.com", 10m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.CustomerName);
    }

    [Fact]
    public void Validate_WithCustomerNameExceedingMaxLength_ShouldFailValidation()
    {
        // Arrange
        var command = new CreateOrderCommand(new string('A', 101), "alice@example.com", 10m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.CustomerName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void Validate_WithInvalidEmail_ShouldFailValidation(string email)
    {
        // Arrange
        var command = new CreateOrderCommand("Alice", email, 10m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithNonPositiveTotalPrice_ShouldFailValidation(decimal totalPrice)
    {
        // Arrange
        var command = new CreateOrderCommand("Alice", "alice@example.com", totalPrice);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.TotalPrice);
    }

    [Fact]
    public void Validate_WithSmallPositiveTotalPrice_ShouldPassValidation()
    {
        // Arrange
        var command = new CreateOrderCommand("Alice", "alice@example.com", 0.01m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(c => c.TotalPrice);
    }
}
