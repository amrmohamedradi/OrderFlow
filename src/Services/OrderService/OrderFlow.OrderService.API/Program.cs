using OrderFlow.OrderService.API.Common.Handlers;
using OrderFlow.OrderService.Application;
using OrderFlow.OrderService.Infrastructure;
using OrderFlow.OrderService.Infrastructure.Persistence;
using OrderFlow.BuildingBlocks.Messaging.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting OrderService API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/order-.txt", rollingInterval: RollingInterval.Day));

    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen();

    builder.Services.AddApplication();

    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddProblemDetails();

    builder.Services.AddMessaging(builder.Configuration);

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var dbContext = services.GetRequiredService<OrderDbContext>();
        for (int i = 0; i < 20; i++)
        {
            try
            {
                logger.LogInformation("Applying migrations for OrderService DbContext...");
                dbContext.Database.Migrate();
                logger.LogInformation("OrderService database migrations applied successfully.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "An error occurred while migrating the database. Retrying in 5 seconds... (Attempt {Attempt}/20)", i + 1);
                if (i == 19) throw;
                System.Threading.Thread.Sleep(5000);
            }
        }
    }

    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();

    
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();

    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "OrderService API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}