using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OrderFlow.BuildingBlocks.Messaging.Configuration;
using RabbitMQ.Client;

namespace OrderFlow.BuildingBlocks.Messaging.HealthChecks;

public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqOptions _options;

    public RabbitMqHealthCheck(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                UserName = _options.Username,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                Port = _options.Port,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            };

            if (_options.UseSsl)
            {
                factory.Ssl = new SslOption
                {
                    Enabled = true,
                    ServerName = _options.Host
                };
            }

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ connection is healthy"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new HealthCheckResult(
                context.Registration.FailureStatus,
                $"RabbitMQ connection failed: {ex.Message}",
                ex));
        }
    }
}
