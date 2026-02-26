using MediatR;
using Modules.Notification.Application.DTOs.Response;
using Modules.Notification.Application.Contracts;

namespace Modules.Notification.Application.Queries;

public sealed record MarkNotificationAsReadQuery(Guid NotificationId)
    : IRequest<NotificationResponseDto>;

public class MarkNotificationAsReadQueryHandler(INotificationRepository notificationRepo)
    : IRequestHandler<MarkNotificationAsReadQuery, NotificationResponseDto>
{
    public async Task<NotificationResponseDto> Handle(MarkNotificationAsReadQuery request, CancellationToken ct)
    {
        var notification = await notificationRepo.MarkAsReadAsync(request.NotificationId, ct)
            ?? throw new InvalidOperationException($"Notification {request.NotificationId} not found");

        await notificationRepo.SaveChangesAsync(ct);

        return new NotificationResponseDto(
            notification.Id,
            notification.UserId,
            notification.AdminId,
            notification.Title,
            notification.Content,
            notification.CreatedAtUtc,
            notification.IsRead,
            notification.ReadAtUtc
        );
    }
}
