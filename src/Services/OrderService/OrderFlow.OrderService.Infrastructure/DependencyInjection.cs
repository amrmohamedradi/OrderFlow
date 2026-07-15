using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.OrderService.Application.Interfaces.Messaging;
using OrderFlow.OrderService.Application.Interfaces.Persistence;
using OrderFlow.OrderService.Application.Interfaces.Repositories;
using OrderFlow.OrderService.Infrastructure.Messaging;
using OrderFlow.OrderService.Infrastructure.Persistence;
using OrderFlow.OrderService.Infrastructure.Repositories;

namespace OrderFlow.OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        
        return services;
    }
}

