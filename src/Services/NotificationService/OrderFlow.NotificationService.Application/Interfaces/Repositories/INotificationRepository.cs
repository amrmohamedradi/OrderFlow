using OrderFlow.NotificationService.Domain.Entities;

namespace OrderFlow.NotificationService.Application.Interfaces.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
}
