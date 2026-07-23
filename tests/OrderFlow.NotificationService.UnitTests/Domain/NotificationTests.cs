using FluentAssertions;
using OrderFlow.NotificationService.Domain.Entities;
using OrderFlow.NotificationService.Domain.Enums;
using OrderFlow.NotificationService.Domain.Exceptions;
using Xunit;

namespace OrderFlow.NotificationService.UnitTests.Domain;

public class NotificationTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldReturnNotificationWithPendingStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        const string email = "alice@example.com";

        // Act
        var notification = Notification.Create(orderId, email);

        // Assert
        notification.Should().NotBeNull();
        notification.Id.Should().NotBeEmpty();
        notification.OrderId.Should().Be(orderId);
        notification.Email.Should().Be(email);
        notification.Status.Should().Be(NotificationStatus.Pending);
        notification.SentAt.Should().BeNull();
        notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithEmptyOrderId_ShouldThrowDomainException()
    {
        // Act
        var act = () => Notification.Create(Guid.Empty, "alice@example.com");

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*OrderId is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithEmptyOrNullEmail_ShouldThrowDomainException(string? email)
    {
        // Act
        var act = () => Notification.Create(Guid.NewGuid(), email!);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*Email is required*");
    }

    // ── MarkAsSent ────────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsSent_WhenPending_ShouldChangeStatusToSentAndSetSentAt()
    {
        // Arrange
        var notification = Notification.Create(Guid.NewGuid(), "alice@example.com");

        // Act
        notification.MarkAsSent();

        // Assert
        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.SentAt.Should().NotBeNull();
        notification.SentAt!.Value.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsSent_WhenAlreadySent_ShouldThrowDomainException()
    {
        // Arrange
        var notification = Notification.Create(Guid.NewGuid(), "alice@example.com");
        notification.MarkAsSent();

        // Act
        var act = () => notification.MarkAsSent();

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*Only pending notifications can be marked as sent*");
    }

    [Fact]
    public void MarkAsSent_WhenFailed_ShouldThrowDomainException()
    {
        // Arrange
        var notification = Notification.Create(Guid.NewGuid(), "alice@example.com");
        notification.MarkAsFailed();

        // Act
        var act = () => notification.MarkAsSent();

        // Assert
        act.Should().Throw<DomainException>();
    }

    // ── MarkAsFailed ──────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsFailed_WhenPending_ShouldChangeStatusToFailed()
    {
        // Arrange
        var notification = Notification.Create(Guid.NewGuid(), "alice@example.com");

        // Act
        notification.MarkAsFailed();

        // Assert
        notification.Status.Should().Be(NotificationStatus.Failed);
        notification.SentAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadySent_ShouldThrowDomainException()
    {
        // Arrange
        var notification = Notification.Create(Guid.NewGuid(), "alice@example.com");
        notification.MarkAsSent();

        // Act
        var act = () => notification.MarkAsFailed();

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*Only pending notifications can be marked as failed*");
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadyFailed_ShouldThrowDomainException()
    {
        // Arrange
        var notification = Notification.Create(Guid.NewGuid(), "alice@example.com");
        notification.MarkAsFailed();

        // Act
        var act = () => notification.MarkAsFailed();

        // Assert
        act.Should().Throw<DomainException>();
    }
}
