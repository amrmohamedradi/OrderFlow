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
                var correlationId = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders");
        request.Headers.Add(CorrelationIdMiddleware.CorrelationIdHeaderName, correlationId);
        request.Content = JsonContent.Create(new
        {
            customerName = "Alice",
            email = "alice@example.com",
            totalPrice = 10m
        });

                var response = await _client.SendAsync(request);

                response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.TryGetValues(CorrelationIdMiddleware.CorrelationIdHeaderName, out var values)
                .Should().BeTrue();
        values!.First().Should().Be(correlationId);
    }

    [Fact]
    public async Task PostOrder_WithoutCorrelationIdHeader_ShouldGenerateAndReturnCorrelationId()
    {
                var payload = new
        {
            customerName = "Bob",
            email = "bob@example.com",
            totalPrice = 20m
        };

                var response = await _client.PostAsJsonAsync("/api/orders", payload);

                response.Headers.TryGetValues(CorrelationIdMiddleware.CorrelationIdHeaderName, out var values)
                .Should().BeTrue();

        var returnedId = values!.First();
        Guid.TryParse(returnedId, out _).Should().BeTrue("the generated correlation ID should be a valid Guid");
    }

    [Fact]
    public async Task PostOrder_WithMalformedCorrelationIdHeader_ShouldGenerateNewCorrelationId()
    {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders");
        request.Headers.Add(CorrelationIdMiddleware.CorrelationIdHeaderName, "not-a-guid");
        request.Content = JsonContent.Create(new
        {
            customerName = "Carol",
            email = "carol@example.com",
            totalPrice = 30m
        });

                var response = await _client.SendAsync(request);

                response.Headers.TryGetValues(CorrelationIdMiddleware.CorrelationIdHeaderName, out var values)
                .Should().BeTrue();

        var returnedId = values!.First();
        Guid.TryParse(returnedId, out _).Should().BeTrue();
        returnedId.Should().NotBe("not-a-guid");
    }
}
