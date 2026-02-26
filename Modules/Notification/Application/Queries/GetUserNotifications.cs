using MediatR;
using Modules.Notification.Application.DTOs.Response;
using Modules.Notification.Application.Contracts;

namespace Modules.Notification.Application.Queries;

public sealed record GetUserNotificationsQuery(Guid UserId, int Take = 50)
    : IRequest<List<NotificationResponseDto>>;

public class GetUserNotificationsQueryHandler(INotificationRepository notificationRepo)
    : IRequestHandler<GetUserNotificationsQuery, List<NotificationResponseDto>>
{
    public async Task<List<NotificationResponseDto>> Handle(GetUserNotificationsQuery request, CancellationToken ct)
    {
        var take = Math.Clamp(request.Take, 1, 100);
        var notifications = await notificationRepo.GetByUserIdAsync(request.UserId, take, ct);

        return notifications.Select(n => new NotificationResponseDto(
            n.Id,
            n.UserId,
            n.AdminId,
            n.Title,
            n.Content,
            n.CreatedAtUtc,
            n.IsRead,
            n.ReadAtUtc
        )).ToList();
    }
}
