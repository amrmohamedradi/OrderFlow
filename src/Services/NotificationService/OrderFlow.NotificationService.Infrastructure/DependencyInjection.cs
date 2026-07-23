using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderFlow.BuildingBlocks.Messaging.DependencyInjection;
using OrderFlow.BuildingBlocks.Messaging.HealthChecks;
using OrderFlow.NotificationService.Application.Consumers;
using OrderFlow.NotificationService.Application.Interfaces.Persistence;
using OrderFlow.NotificationService.Application.Interfaces.Repositories;
using OrderFlow.NotificationService.Application.Interfaces.Services;
using OrderFlow.NotificationService.Infrastructure.Configuration;
using OrderFlow.NotificationService.Infrastructure.Consumers;
using OrderFlow.NotificationService.Infrastructure.Persistence;
using OrderFlow.NotificationService.Infrastructure.Repositories;
using OrderFlow.NotificationService.Infrastructure.Services;

namespace OrderFlow.NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddMessaging(
        configuration,
        configure: x =>
        {
            x.AddConsumer<OrderCreateConsumer, Consumers.OrderCreateConsumerDefinition>();

            x.AddEntityFrameworkOutbox<NotificationDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();
            });
        });

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Application process is alive"), tags: new[] { "live" })
            .AddDbContextCheck<NotificationDbContext>("database", failureStatus: HealthStatus.Unhealthy, tags: new[] { "ready" })
            .AddRabbitMqHealthCheck(configuration, "rabbitmq", tags: new[] { "ready" });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
