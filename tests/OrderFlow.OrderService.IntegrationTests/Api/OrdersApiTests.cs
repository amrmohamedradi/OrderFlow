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

    [Fact]
    public async Task PostOrder_WithValidData_ShouldReturn201Created()
    {
                var payload = new
        {
            customerName = "Alice",
            email = "alice@example.com",
            totalPrice = 99.99m
        };

                var response = await _client.PostAsJsonAsync("/api/orders", payload);

                response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostOrder_WithValidData_ShouldReturnResponseWithCorrectFields()
    {
                var payload = new
        {
            customerName = "Bob",
            email = "bob@example.com",
            totalPrice = 150m
        };

                var response = await _client.PostAsJsonAsync("/api/orders", payload);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

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
                var payload = new
        {
            customerName = "Carol",
            email = "carol@example.com",
            totalPrice = 75m
        };

                var response = await _client.PostAsJsonAsync("/api/orders", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = content.GetProperty("id").GetGuid();

                using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var persisted = await db.Orders.FindAsync(orderId);

        persisted.Should().NotBeNull();
        persisted!.CustomerName.Should().Be("Carol");
        persisted.Email.Should().Be("carol@example.com");
        persisted.TotalPrice.Should().Be(75m);
    }

    [Fact]
    public async Task PostOrder_WithMissingCustomerName_ShouldReturnValidationError()
    {
                var payload = new
        {
            customerName = "",
            email = "dave@example.com",
            totalPrice = 10m
        };

                var response = await _client.PostAsJsonAsync("/api/orders", payload);

                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostOrder_WithInvalidEmail_ShouldReturnValidationError()
    {
                var payload = new
        {
            customerName = "Eve",
            email = "not-an-email",
            totalPrice = 10m
        };

                var response = await _client.PostAsJsonAsync("/api/orders", payload);

                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostOrder_WithZeroTotalPrice_ShouldReturnValidationError()
    {
                var payload = new
        {
            customerName = "Frank",
            email = "frank@example.com",
            totalPrice = 0m
        };

                var response = await _client.PostAsJsonAsync("/api/orders", payload);

                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
