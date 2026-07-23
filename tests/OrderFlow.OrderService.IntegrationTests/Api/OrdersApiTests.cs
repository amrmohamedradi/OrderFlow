using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.OrderService.IntegrationTests.Infrastructure;
using OrderFlow.OrderService.Infrastructure.Persistence;
using Xunit;

namespace OrderFlow.OrderService.IntegrationTests.Api;

[Collection("OrderService Integration")]
public class OrdersApiTests
{
    private readonly HttpClient _client;
    private readonly OrderServiceWebAppFactory _factory;

    public OrdersApiTests(OrderServiceWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Create Order — happy path ─────────────────────────────────────────────

    [Fact]
    public async Task PostOrder_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        var payload = new
        {
            customerName = "Alice",
            email = "alice@example.com",
            totalPrice = 99.99m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostOrder_WithValidData_ShouldReturnResponseWithCorrectFields()
    {
        // Arrange
        var payload = new
        {
            customerName = "Bob",
            email = "bob@example.com",
            totalPrice = 150m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", payload);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        content.GetProperty("customerName").GetString().Should().Be("Bob");
        content.GetProperty("email").GetString().Should().Be("bob@example.com");
        content.GetProperty("totalPrice").GetDecimal().Should().Be(150m);
        content.GetProperty("status").GetString().Should().Be("Pending");
        content.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostOrder_WithValidData_ShouldPersistOrderInDatabase()
    {
        // Arrange
        var payload = new
        {
            customerName = "Carol",
            email = "carol@example.com",
            totalPrice = 75m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = content.GetProperty("id").GetGuid();

        // Assert — verify the order exists in the actual database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var persisted = await db.Orders.FindAsync(orderId);

        persisted.Should().NotBeNull();
        persisted!.CustomerName.Should().Be("Carol");
        persisted.Email.Should().Be("carol@example.com");
        persisted.TotalPrice.Should().Be(75m);
    }

    // ── Create Order — validation errors ─────────────────────────────────────

    [Fact]
    public async Task PostOrder_WithMissingCustomerName_ShouldReturnValidationError()
    {
        // Arrange
        var payload = new
        {
            customerName = "",
            email = "dave@example.com",
            totalPrice = 10m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", payload);

        // Assert – FluentValidation pipeline returns 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostOrder_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        var payload = new
        {
            customerName = "Eve",
            email = "not-an-email",
            totalPrice = 10m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostOrder_WithZeroTotalPrice_ShouldReturnValidationError()
    {
        // Arrange
        var payload = new
        {
            customerName = "Frank",
            email = "frank@example.com",
            totalPrice = 0m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
