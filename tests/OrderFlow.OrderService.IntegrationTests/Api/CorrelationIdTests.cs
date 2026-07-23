using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderFlow.BuildingBlocks.Messaging.Correlation;
using OrderFlow.OrderService.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderFlow.OrderService.IntegrationTests.Api;

[Collection("OrderService Integration")]
public class CorrelationIdTests
{
    private readonly HttpClient _client;

    public CorrelationIdTests(OrderServiceWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostOrder_WithCorrelationIdHeader_ShouldEchoSameCorrelationIdInResponse()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders");
        request.Headers.Add(CorrelationIdMiddleware.CorrelationIdHeaderName, correlationId);
        request.Content = JsonContent.Create(new
        {
            customerName = "Alice",
            email = "alice@example.com",
            totalPrice = 10m
        });

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.TryGetValues(CorrelationIdMiddleware.CorrelationIdHeaderName, out var values)
                .Should().BeTrue();
        values!.First().Should().Be(correlationId);
    }

    [Fact]
    public async Task PostOrder_WithoutCorrelationIdHeader_ShouldGenerateAndReturnCorrelationId()
    {
        // Arrange – no X-Correlation-ID header
        var payload = new
        {
            customerName = "Bob",
            email = "bob@example.com",
            totalPrice = 20m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", payload);

        // Assert – the middleware generates a new correlation ID and echoes it back
        response.Headers.TryGetValues(CorrelationIdMiddleware.CorrelationIdHeaderName, out var values)
                .Should().BeTrue();

        var returnedId = values!.First();
        Guid.TryParse(returnedId, out _).Should().BeTrue("the generated correlation ID should be a valid Guid");
    }

    [Fact]
    public async Task PostOrder_WithMalformedCorrelationIdHeader_ShouldGenerateNewCorrelationId()
    {
        // Arrange – header present but not a valid Guid
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders");
        request.Headers.Add(CorrelationIdMiddleware.CorrelationIdHeaderName, "not-a-guid");
        request.Content = JsonContent.Create(new
        {
            customerName = "Carol",
            email = "carol@example.com",
            totalPrice = 30m
        });

        // Act
        var response = await _client.SendAsync(request);

        // Assert – middleware falls back to a new Guid
        response.Headers.TryGetValues(CorrelationIdMiddleware.CorrelationIdHeaderName, out var values)
                .Should().BeTrue();

        var returnedId = values!.First();
        Guid.TryParse(returnedId, out _).Should().BeTrue();
        returnedId.Should().NotBe("not-a-guid");
    }
}
