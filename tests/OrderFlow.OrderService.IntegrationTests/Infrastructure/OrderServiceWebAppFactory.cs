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

/// <summary>
/// Shared WebApplicationFactory fixture backed by an in-process SQLite database.
///
/// Strategy:
/// - SQLite replaces SQL Server — no Docker or external process needed.
/// - We keep a persistent connection open for the lifetime of the fixture
///   so the in-memory SQLite database is not destroyed between requests.
/// - All MassTransit services are replaced with an in-memory transport (no RabbitMQ).
/// - EF Core schema is created via EnsureCreated() (no migrations needed for SQLite).
///
/// NOTE: The MassTransit EF Outbox entities (InboxState, OutboxMessage, OutboxState)
/// are registered in OrderDbContext.OnModelCreating. These are automatically created
/// by EnsureCreated() in SQLite, so the Outbox schema is present for tests.
/// </summary>
public class OrderServiceWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Keep the connection alive for the full test lifecycle so the in-memory DB persists
    private SqliteConnection? _keepAliveConnection;

    public async Task InitializeAsync()
    {
        _keepAliveConnection = new SqliteConnection("DataSource=:memory:");
        _keepAliveConnection.Open();

        // Accessing Services forces the host to build now (which runs ConfigureWebHost,
        // setting up the SQLite DbContext). We then create the schema once for the fixture.
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
            // ── Replace SQL Server DbContext with SQLite ──────────────────────
            // We do NOT use services.AddDbContext<OrderDbContext>(UseSqlite) here.
            // AddDbContext registers EF Core's Sqlite provider services in the app DI,
            // but AddInfrastructure already registered SqlServer provider services there.
            // EF Core 10 rejects having two database providers in the same app DI
            // container (it raises "multiple database providers registered").
            //
            // The fix: register OrderDbContext via a direct factory that builds a
            // standalone DbContextOptionsBuilder. EF Core then creates its own isolated
            // internal service provider purely from the options extensions (SQLite),
            // never consulting the app DI for provider services.
            services.RemoveAll<DbContextOptions<OrderDbContext>>();
            services.RemoveAll<OrderDbContext>();

            services.AddScoped<OrderDbContext>(_ =>
            {
                var options = new DbContextOptionsBuilder<OrderDbContext>()
                    .UseSqlite(_keepAliveConnection!)
                    .Options;
                return new OrderDbContext(options);
            });

            // ── Replace MassTransit with in-memory transport ─────────────────
            // Remove ALL MassTransit-sourced descriptors so AddMassTransitTestHarness
            // can register a clean in-memory bus without conflicting with the
            // production RabbitMQ configuration added by AddInfrastructure.
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
