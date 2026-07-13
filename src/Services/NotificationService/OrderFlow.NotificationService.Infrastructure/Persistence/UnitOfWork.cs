using OrderFlow.NotificationService.Application.Interfaces.Persistence;

namespace OrderFlow.NotificationService.Infrastructure.Persistence;

public class UnitOfWork : OrderFlow.NotificationService.Application.Interfaces.Persistence.IUnitOfWork
{
    private readonly NotificationDbContext _context;

    public UnitOfWork(NotificationDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
