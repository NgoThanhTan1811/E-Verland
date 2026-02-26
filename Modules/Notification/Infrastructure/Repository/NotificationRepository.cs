using Microsoft.EntityFrameworkCore;
using Modules.Notification.Application.Contracts;
using Modules.Notification.Domain;
using Modules.Notification.Infrastructure.Persistence;

namespace Modules.Notification.Infrastructure.Repository;

public class NotificationRepository(NotificationDbContext dbContext) : INotificationRepository
{
    private readonly NotificationDbContext _dbContext = dbContext;

    public async Task<Domain.Notification> CreateAsync(Domain.Notification notification, CancellationToken ct = default)
    {
        var entry = await _dbContext.Notifications.AddAsync(notification, ct);
        return entry.Entity;
    }

    public async Task<Domain.Notification?> GetByIdAsync(Guid notificationId, CancellationToken ct = default)
    {
        return await _dbContext.Notifications.FindAsync([notificationId], cancellationToken: ct);
    }

    public async Task<List<Domain.Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken: ct);
    }

    public async Task<List<Domain.Notification>> GetByUserIdAsync(Guid userId, int take = 50, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);

        return await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken: ct);
    }

    public async Task<Domain.Notification?> MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _dbContext.Notifications.FirstOrDefaultAsync(
            n => n.Id == notificationId,
            cancellationToken: ct);

        if (notification != null && !notification.IsRead)
        {
            notification.MarkAsRead();
        }

        return notification;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _dbContext.SaveChangesAsync(ct);
    }
}
