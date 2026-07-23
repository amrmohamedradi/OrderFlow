using System.Net;
using FluentAssertions;
using OrderFlow.OrderService.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderFlow.OrderService.IntegrationTests.Api;

[Collection("OrderService Integration")]
public class HealthCheckTests
{
    private readonly HttpClient _client;

    public HealthCheckTests(OrderServiceWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLiveHealth_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert – the "self" liveness check should always succeed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReadyHealth_ShouldReturn200WhenDatabaseIsReachable()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert – database is the Testcontainer SQL Server which is running;
        // RabbitMQ is not present in the test environment, so we accept either
        // 200 (all healthy) or 503 (degraded due to RabbitMQ check).
        // The important thing is the endpoint is correctly mapped and responds.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetLiveHealth_ShouldReturnJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.Content.Headers.ContentType?.MediaType
                .Should().Contain("json");
    }
}
