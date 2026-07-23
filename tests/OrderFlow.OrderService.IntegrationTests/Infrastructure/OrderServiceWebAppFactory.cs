using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderFlow.OrderService.Infrastructure.Persistence;
using Xunit;

namespace OrderFlow.OrderService.IntegrationTests.Infrastructure;

public class OrderServiceWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
        private SqliteConnection? _keepAliveConnection;

    public async Task InitializeAsync()
    {
        _keepAliveConnection = new SqliteConnection("DataSource=:memory:");
        _keepAliveConnection.Open();

                        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        db.Database.EnsureCreated();
    }

    public new async Task DisposeAsync()
    {
        if (_keepAliveConnection is not null)
        {
            await _keepAliveConnection.CloseAsync();
            await _keepAliveConnection.DisposeAsync();
        }
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
                                                                                                                                                services.RemoveAll<DbContextOptions<OrderDbContext>>();
            services.RemoveAll<OrderDbContext>();

            services.AddScoped<OrderDbContext>(_ =>
            {
                var options = new DbContextOptionsBuilder<OrderDbContext>()
                    .UseSqlite(_keepAliveConnection!)
                    .Options;
                return new OrderDbContext(options);
            });

                                                            var massTransitDescriptors = services
                .Where(d =>
                    d.ServiceType.Assembly?.GetName().Name?.StartsWith("MassTransit") == true ||
                    d.ImplementationType?.Assembly?.GetName().Name?.StartsWith("MassTransit") == true)
                .ToList();
            foreach (var descriptor in massTransitDescriptors)
                services.Remove(descriptor);

            services.AddMassTransitTestHarness();
        });
    }
}
