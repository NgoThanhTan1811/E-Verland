namespace Modules.Notification.Application.Contracts;

public interface INotificationRepository
{
    Task<Domain.Notification> CreateAsync(Domain.Notification notification, CancellationToken ct = default);

    Task<Domain.Notification?> GetByIdAsync(Guid notificationId, CancellationToken ct = default);

    Task<List<Domain.Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<List<Domain.Notification>> GetByUserIdAsync(Guid userId, int take = 50, CancellationToken ct = default);

    Task<Domain.Notification?> MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
