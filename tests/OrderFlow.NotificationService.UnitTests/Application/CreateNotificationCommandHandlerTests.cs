using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OrderFlow.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using OrderFlow.NotificationService.Application.Interfaces.Persistence;
using OrderFlow.NotificationService.Application.Interfaces.Repositories;
using OrderFlow.NotificationService.Application.Interfaces.Services;
using OrderFlow.NotificationService.Domain.Entities;
using Xunit;

namespace OrderFlow.NotificationService.UnitTests.Application;

public class CreateNotificationCommandHandlerTests
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<CreateNotificationCommandHandler> _logger;
    private readonly CreateNotificationCommandHandler _handler;

    public CreateNotificationCommandHandlerTests()
    {
        _notificationRepository = Substitute.For<INotificationRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _emailService = Substitute.For<IEmailService>();
        _logger = Substitute.For<ILogger<CreateNotificationCommandHandler>>();

        _handler = new CreateNotificationCommandHandler(
            _notificationRepository,
            _unitOfWork,
            _logger,
            _emailService);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailSucceeds_ShouldMarkNotificationAsSentAndPersist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new CreateNotificationCommand(orderId, "alice@example.com");

        Notification? capturedNotification = null;
        await _notificationRepository
            .AddAsync(Arg.Do<Notification>(n => capturedNotification = n), Arg.Any<CancellationToken>());

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        response.Status.Should().Be("Sent");
        response.SentAt.Should().NotBeNull();
        response.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Handle_WhenEmailSucceeds_ShouldCallEmailServiceWithCorrectParameters()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new CreateNotificationCommand(orderId, "bob@example.com");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _emailService.Received(1).SendAsync(
            "bob@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailSucceeds_ShouldCallSaveChanges()
    {
        // Arrange
        var command = new CreateNotificationCommand(Guid.NewGuid(), "carol@example.com");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailSucceeds_ShouldReturnResponseWithSentStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new CreateNotificationCommand(orderId, "dave@example.com");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().NotBeEmpty();
        response.Email.Should().Be("dave@example.com");
        response.Status.Should().Be("Sent");
    }

    // ── Email failure path ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailServiceThrows_ShouldMarkNotificationAsFailed()
    {
        // Arrange
        _emailService
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SmtpException("SMTP connection refused"));

        var command = new CreateNotificationCommand(Guid.NewGuid(), "eve@example.com");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Status.Should().Be("Failed");
        response.SentAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenEmailServiceThrows_ShouldStillCallSaveChanges()
    {
        // Arrange — email failure must not prevent the notification from being persisted
        _emailService
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SmtpException("SMTP unavailable"));

        var command = new CreateNotificationCommand(Guid.NewGuid(), "frank@example.com");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailServiceThrows_ShouldAddNotificationToRepository()
    {
        // Arrange
        _emailService
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SmtpException("SMTP unavailable"));

        var command = new CreateNotificationCommand(Guid.NewGuid(), "grace@example.com");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — notification was still persisted (with Failed status)
        await _notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n => n.Email == "grace@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        _notificationRepository
            .AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB write failed"));

        var command = new CreateNotificationCommand(Guid.NewGuid(), "henry@example.com");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("DB write failed");
    }
}

// Minimal exception type to simulate SMTP failures without referencing System.Net.Mail
file sealed class SmtpException : Exception
{
    public SmtpException(string message) : base(message) { }
}
