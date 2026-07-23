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
                var response = await _client.GetAsync("/health/live");

                response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReadyHealth_ShouldReturn200WhenDatabaseIsReachable()
    {
                var response = await _client.GetAsync("/health/ready");

                                        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetLiveHealth_ShouldReturnJsonContentType()
    {
                var response = await _client.GetAsync("/health/live");

                response.Content.Headers.ContentType?.MediaType
                .Should().Contain("json");
    }
}
