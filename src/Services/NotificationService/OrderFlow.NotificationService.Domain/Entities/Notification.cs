using OrderFlow.NotificationService.Domain.Common;
using OrderFlow.NotificationService.Domain.Enums;
using OrderFlow.NotificationService.Domain.Exceptions;

namespace OrderFlow.NotificationService.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid OrderId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public NotificationStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? SentAt { get; private set; }

    private Notification()
    {
    }

    private Notification(Guid orderId, string email)
    {
        OrderId = orderId;
        Email = email;
        Status = NotificationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public static Notification Create(Guid orderId, string email)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("OrderId is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        return new Notification(orderId, email);
    }

    public void MarkAsSent()
    {
        if (Status != NotificationStatus.Pending)
            throw new DomainException("Only pending notifications can be marked as sent.");

        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        if (Status != NotificationStatus.Pending)
            throw new DomainException("Only pending notifications can be marked as failed.");

        Status = NotificationStatus.Failed;
    }
}