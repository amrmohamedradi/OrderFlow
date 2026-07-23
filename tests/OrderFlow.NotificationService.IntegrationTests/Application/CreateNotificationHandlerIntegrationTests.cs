using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OrderFlow.NotificationService.Application;
using OrderFlow.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using OrderFlow.NotificationService.Application.Interfaces.Persistence;
using OrderFlow.NotificationService.Application.Interfaces.Repositories;
using OrderFlow.NotificationService.Application.Interfaces.Services;
using OrderFlow.NotificationService.Domain.Entities;
using OrderFlow.NotificationService.Domain.Enums;
using Xunit;

namespace OrderFlow.NotificationService.IntegrationTests.Application;

/// <summary>
/// Integration tests wiring the real MediatR pipeline (ValidationBehavior included),
/// a lightweight InMemory DbContext, and a mocked IEmailService.
/// We use a test-specific DbContext that does NOT call the MassTransit EF extensions,
/// since those require the full SQL Server provider.
/// </summary>
public class CreateNotificationHandlerIntegrationTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _dbScope;
    private readonly IMediator _mediator;
    private readonly IEmailService _emailService;
    private readonly TestNotificationDbContext _db;

    public CreateNotificationHandlerIntegrationTests()
    {
        _emailService = Substitute.For<IEmailService>();

        var services = new ServiceCollection();

        // Use the test-specific DbContext (no MassTransit EF outbox entities)
        services.AddDbContext<TestNotificationDbContext>(options =>
            options.UseInMemoryDatabase($"NotificationTest_{Guid.NewGuid()}"));

        // Register our test DbContext as the real repository target
        services.AddScoped<INotificationRepository>(sp =>
            new TestNotificationRepository(sp.GetRequiredService<TestNotificationDbContext>()));
        services.AddScoped<IUnitOfWork>(sp =>
            new TestUnitOfWork(sp.GetRequiredService<TestNotificationDbContext>()));

        // Real MediatR pipeline — includes ValidationBehavior via AddApplication()
        services.AddApplication();

        // Mocked email service — no real SMTP calls
        services.AddSingleton(_emailService);

        services.AddLogging();

        _provider = services.BuildServiceProvider(true);

        // Both IMediator and TestNotificationDbContext must be resolved from a child
        // scope, not the root provider. IMediator's pipeline resolves IValidator<T>
        // (registered as scoped) so it too must come from a scope. The InMemory
        // database store is shared across all scopes (same named store), so data
        // saved by the handler is visible through _db within the same scope.
        _dbScope = _provider.CreateScope();
        _mediator = _dbScope.ServiceProvider.GetRequiredService<IMediator>();
        _db = _dbScope.ServiceProvider.GetRequiredService<TestNotificationDbContext>();
    }

    public void Dispose()
    {
        _dbScope.Dispose();
        _provider.Dispose();
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailSucceeds_ShouldPersistNotificationWithSentStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new CreateNotificationCommand(orderId, "alice@example.com");

        // Act
        var response = await _mediator.Send(command);

        // Assert — response
        response.Status.Should().Be(NotificationStatus.Sent.ToString());
        response.SentAt.Should().NotBeNull();

        // Assert — database persistence
        var persisted = await _db.Notifications.FindAsync(response.Id);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(NotificationStatus.Sent);
        persisted.OrderId.Should().Be(orderId);
        persisted.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Handle_WhenEmailSucceeds_ShouldCallEmailServiceWithCorrectAddress()
    {
        // Arrange
        var command = new CreateNotificationCommand(Guid.NewGuid(), "bob@example.com");

        // Act
        await _mediator.Send(command);

        // Assert
        await _emailService.Received(1).SendAsync(
            "bob@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── Email failure path ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailFails_ShouldPersistNotificationWithFailedStatus()
    {
        // Arrange
        _emailService
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));

        var orderId = Guid.NewGuid();
        var command = new CreateNotificationCommand(orderId, "carol@example.com");

        // Act
        var response = await _mediator.Send(command);

        // Assert — response
        response.Status.Should().Be(NotificationStatus.Failed.ToString());
        response.SentAt.Should().BeNull();

        // Assert — notification stored despite email failure
        var persisted = await _db.Notifications.FindAsync(response.Id);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(NotificationStatus.Failed);
        persisted.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task Handle_WhenEmailFails_ShouldStillPersistTheNotification()
    {
        // Arrange
        _emailService
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP timeout"));

        var command = new CreateNotificationCommand(Guid.NewGuid(), "dave@example.com");

        // Act
        var response = await _mediator.Send(command);

        // Assert — a notification record always exists after the handler completes
        var count = await _db.Notifications.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenCommandHasEmptyOrderId_ShouldThrowBeforeHandlerExecutes()
    {
        // Arrange — ValidationBehavior should catch this before the handler runs
        var command = new CreateNotificationCommand(Guid.Empty, "eve@example.com");

        // Act
        var act = () => _mediator.Send(command);

        // Assert — validation exception thrown; no notification persisted
        await act.Should().ThrowAsync<Exception>();
        var count = await _db.Notifications.CountAsync();
        count.Should().Be(0);
    }
}

// ── Test infrastructure helpers ────────────────────────────────────────────────

/// <summary>
/// Lightweight DbContext for tests — omits the MassTransit EF extensions
/// (InboxState, OutboxMessage, OutboxState) which require SQL Server.
/// </summary>
internal sealed class TestNotificationDbContext : DbContext
{
    public TestNotificationDbContext(DbContextOptions<TestNotificationDbContext> options)
        : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Email).IsRequired();
            entity.Property(n => n.Status).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}

internal sealed class TestNotificationRepository : INotificationRepository
{
    private readonly TestNotificationDbContext _context;
    public TestNotificationRepository(TestNotificationDbContext context) => _context = context;

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        => await _context.Notifications.AddAsync(notification, cancellationToken);
}

internal sealed class TestUnitOfWork : IUnitOfWork
{
    private readonly TestNotificationDbContext _context;
    public TestUnitOfWork(TestNotificationDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
