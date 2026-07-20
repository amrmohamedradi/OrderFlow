using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderFlow.BuildingBlocks.Messaging.Configuration;

namespace OrderFlow.BuildingBlocks.Messaging.HealthChecks;

public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddRabbitMqHealthCheck(
        this IHealthChecksBuilder builder,
        IConfiguration configuration,
        string name = "rabbitmq",
        HealthStatus? failureStatus = default,
        IEnumerable<string>? tags = null)
    {
        builder.Services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMQ"));
        return builder.AddCheck<RabbitMqHealthCheck>(name, failureStatus ?? HealthStatus.Unhealthy, tags ?? new[] { "ready" });
    }
}
