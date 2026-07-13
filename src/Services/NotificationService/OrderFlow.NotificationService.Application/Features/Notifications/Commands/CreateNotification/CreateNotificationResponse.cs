namespace OrderFlow.NotificationService.Application.Features.Notifications.Commands.CreateNotification;

public record CreateNotificationResponse(Guid Id, string Email, string Status, DateTime CreatedAt, DateTime? SentAt);
