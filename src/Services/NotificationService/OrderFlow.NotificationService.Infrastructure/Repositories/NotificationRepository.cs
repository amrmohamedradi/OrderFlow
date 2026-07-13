using OrderFlow.NotificationService.Application.Interfaces.Repositories;
using OrderFlow.NotificationService.Domain.Entities;
using OrderFlow.NotificationService.Infrastructure.Persistence;

namespace OrderFlow.NotificationService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
    }
}
