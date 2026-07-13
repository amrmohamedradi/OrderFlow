using MediatR;

namespace OrderFlow.NotificationService.Application.Features.Notifications.Commands.CreateNotification;

public record CreateNotificationCommand(Guid OrderId, string Email) : IRequest<CreateNotificationResponse>;
