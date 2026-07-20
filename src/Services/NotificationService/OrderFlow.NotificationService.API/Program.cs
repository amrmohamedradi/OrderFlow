using OrderFlow.NotificationService.API.Common.Handlers;
using OrderFlow.NotificationService.Application;
using OrderFlow.NotificationService.Infrastructure;
using OrderFlow.NotificationService.Infrastructure.Persistence;
using OrderFlow.BuildingBlocks.Messaging.Correlation;
using OrderFlow.BuildingBlocks.Messaging.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting NotificationService API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("logs/notification-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen();

    builder.Services.AddApplication();

    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddProblemDetails();

    builder.Services.AddCorrelationId();

    var app = builder.Build();

    app.UseCorrelationId();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var dbContext = services.GetRequiredService<NotificationDbContext>();
        for (int i = 0; i < 20; i++)
        {
            try
            {
                logger.LogInformation("Applying migrations for NotificationService DbContext...");
                dbContext.Database.Migrate();
                logger.LogInformation("NotificationService database migrations applied successfully.");
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

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),
        ResponseWriter = HealthCheckResponseWriter.WriteResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready") || check.Name.Equals("masstransit", StringComparison.OrdinalIgnoreCase),
        ResponseWriter = HealthCheckResponseWriter.WriteResponse
    });

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "NotificationService API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}