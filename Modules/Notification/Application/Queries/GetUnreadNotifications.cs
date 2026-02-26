using MediatR;
using Modules.Notification.Application.DTOs.Response;
using Modules.Notification.Application.Contracts;

namespace Modules.Notification.Application.Queries;

public sealed record GetUnreadNotificationsQuery(Guid UserId)
    : IRequest<List<NotificationResponseDto>>;

public class GetUnreadNotificationsQueryHandler(INotificationRepository notificationRepo)
    : IRequestHandler<GetUnreadNotificationsQuery, List<NotificationResponseDto>>
{
    public async Task<List<NotificationResponseDto>> Handle(GetUnreadNotificationsQuery request, CancellationToken ct)
    {
        var notifications = await notificationRepo.GetUnreadByUserIdAsync(request.UserId, ct);

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
