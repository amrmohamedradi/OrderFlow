using FluentAssertions;
using OrderFlow.NotificationService.Domain.Entities;
using OrderFlow.NotificationService.Domain.Enums;
using OrderFlow.NotificationService.Domain.Exceptions;
using Xunit;

namespace OrderFlow.NotificationService.UnitTests.Domain;

public class NotificationTests
{

    [Fact]
    public void Create_WithValidData_ShouldReturnNotificationWithPendingStatus()
    {
                var orderId = Guid.NewGuid();
        const string email = "alice@example.com";

                var notification = Notification.Create(orderId, email);

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
                var act = () => Notification.Create(Guid.Empty, "alice@example.com");

                act.Should().Throw<DomainException>()
           .WithMessage("*OrderId is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithEmptyOrNullEmail_ShouldThrowDomainException(string? email)
    {
                var act = () => Notification.Create(Guid.NewGuid(), email!);

                act.Should().Throw<DomainException>()
           .WithMessage("*Email is required*");
    }

    [Fact]
    public void MarkAsSent_WhenPending_ShouldChangeStatusToSentAndSetSentAt()
    {
                var notification = Notification.Create(Guid.NewGuid(), "alice@example.com");

                notification.MarkAsSent();

                notification.Status.Should().Be(NotificationStatus.Sent);
        notification.SentAt.Should().NotBeNull();
        notification.SentAt!.Value.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsSent_WhenAlreadySent_ShouldThrowDomainException()
    {
                var notification = Notification.Create(Guid.NewGuid(), "alice@example.com");
        notification.MarkAsSent();

                var act = () => notification.MarkAsSent();

                act.Should().Throw<DomainException>()
           .WithMessage("*Only pending notifications can be marked as sent*");
    }

    [Fact]
    public void MarkAsSent_WhenFailed_ShouldThrowDomainException()
    {
                var notification = Notification.Create(Guid.NewGuid(), "alice@example.com");
        notification.MarkAsFailed();

                var act = () => notification.MarkAsSent();

                act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkAsFailed_WhenPending_ShouldChangeStatusToFailed()
    {
                var notification = Notification.Create(Guid.NewGuid(), "alice@example.com");

                notification.MarkAsFailed();

                notification.Status.Should().Be(NotificationStatus.Failed);
        notification.SentAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadySent_ShouldThrowDomainException()
    {
                var notification = Notification.Create(Guid.NewGuid(), "alice@example.com");
        notification.MarkAsSent();

                var act = () => notification.MarkAsFailed();

                act.Should().Throw<DomainException>()
           .WithMessage("*Only pending notifications can be marked as failed*");
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadyFailed_ShouldThrowDomainException()
    {
                var notification = Notification.Create(Guid.NewGuid(), "alice@example.com");
        notification.MarkAsFailed();

                var act = () => notification.MarkAsFailed();

                act.Should().Throw<DomainException>();
    }
}
