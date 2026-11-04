using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.BuildingBlocks.Messaging.Configuration;
using OrderFlow.BuildingBlocks.Messaging.Correlation;

namespace OrderFlow.BuildingBlocks.Messaging.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        var options = configuration
            .GetSection("RabbitMQ")
            .Get<RabbitMqOptions>()
            ?? throw new InvalidOperationException("RabbitMQ configuration is missing.");

        services.AddMassTransit(x =>
        {
            configure?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, options.Port, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);

                    if (options.UseSsl)
                    {
                        h.UseSsl(s => s.Protocol = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13);
                    }
                });

                cfg.UsePublishFilter(typeof(CorrelationIdPublishFilter<>), context);
                cfg.UseSendFilter(typeof(CorrelationIdSendFilter<>), context);
                cfg.UseConsumeFilter(typeof(CorrelationIdConsumeFilter<>), context);

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}