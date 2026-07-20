using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderFlow.BuildingBlocks.Messaging.DependencyInjection;
using OrderFlow.BuildingBlocks.Messaging.HealthChecks;
using OrderFlow.OrderService.Application.Interfaces.Messaging;
using OrderFlow.OrderService.Application.Interfaces.Persistence;
using OrderFlow.OrderService.Application.Interfaces.Repositories;
using OrderFlow.OrderService.Infrastructure.Messaging;
using OrderFlow.OrderService.Infrastructure.Persistence;
using OrderFlow.OrderService.Infrastructure.Repositories;

namespace OrderFlow.OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddMessaging(configuration, x =>
        {
            x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();
            });
        });

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Application process is alive"), tags: new[] { "live" })
            .AddDbContextCheck<OrderDbContext>("database", failureStatus: HealthStatus.Unhealthy, tags: new[] { "ready" })
            .AddRabbitMqHealthCheck(configuration, "rabbitmq", tags: new[] { "ready" });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IEventPublisher, EventPublisher>();

        return services;
    }
}