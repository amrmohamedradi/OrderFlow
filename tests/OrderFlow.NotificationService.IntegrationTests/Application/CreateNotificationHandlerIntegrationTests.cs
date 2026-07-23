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

                services.AddDbContext<TestNotificationDbContext>(options =>
            options.UseInMemoryDatabase($"NotificationTest_{Guid.NewGuid()}"));

                services.AddScoped<INotificationRepository>(sp =>
            new TestNotificationRepository(sp.GetRequiredService<TestNotificationDbContext>()));
        services.AddScoped<IUnitOfWork>(sp =>
            new TestUnitOfWork(sp.GetRequiredService<TestNotificationDbContext>()));

                services.AddApplication();

                services.AddSingleton(_emailService);

        services.AddLogging();

        _provider = services.BuildServiceProvider(true);

                                                _dbScope = _provider.CreateScope();
        _mediator = _dbScope.ServiceProvider.GetRequiredService<IMediator>();
        _db = _dbScope.ServiceProvider.GetRequiredService<TestNotificationDbContext>();
    }

    public void Dispose()
    {
        _dbScope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public async Task Handle_WhenEmailSucceeds_ShouldPersistNotificationWithSentStatus()
    {
                var orderId = Guid.NewGuid();
        var command = new CreateNotificationCommand(orderId, "alice@example.com");

                var response = await _mediator.Send(command);

                response.Status.Should().Be(NotificationStatus.Sent.ToString());
        response.SentAt.Should().NotBeNull();

                var persisted = await _db.Notifications.FindAsync(response.Id);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(NotificationStatus.Sent);
        persisted.OrderId.Should().Be(orderId);
        persisted.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Handle_WhenEmailSucceeds_ShouldCallEmailServiceWithCorrectAddress()
    {
                var command = new CreateNotificationCommand(Guid.NewGuid(), "bob@example.com");

                await _mediator.Send(command);

                await _emailService.Received(1).SendAsync(
            "bob@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailFails_ShouldPersistNotificationWithFailedStatus()
    {
                _emailService
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));

        var orderId = Guid.NewGuid();
        var command = new CreateNotificationCommand(orderId, "carol@example.com");

                var response = await _mediator.Send(command);

                response.Status.Should().Be(NotificationStatus.Failed.ToString());
        response.SentAt.Should().BeNull();

                var persisted = await _db.Notifications.FindAsync(response.Id);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(NotificationStatus.Failed);
        persisted.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task Handle_WhenEmailFails_ShouldStillPersistTheNotification()
    {
                _emailService
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP timeout"));

        var command = new CreateNotificationCommand(Guid.NewGuid(), "dave@example.com");

                var response = await _mediator.Send(command);

                var count = await _db.Notifications.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenCommandHasEmptyOrderId_ShouldThrowBeforeHandlerExecutes()
    {
                var command = new CreateNotificationCommand(Guid.Empty, "eve@example.com");

                var act = () => _mediator.Send(command);

                await act.Should().ThrowAsync<Exception>();
        var count = await _db.Notifications.CountAsync();
        count.Should().Be(0);
    }
}

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
