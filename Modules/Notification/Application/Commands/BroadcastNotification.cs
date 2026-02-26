using MediatR;
using Modules.Notification.Application.DTOs.Request;
using Modules.Notification.Application.DTOs.Response;
using Modules.Notification.Application.Contracts;

namespace Modules.Notification.Application.Commands;

public sealed record BroadcastNotificationCommand(BroadcastNotificationRequestDto Request)
    : IRequest<List<Guid>>;

public class BroadcastNotificationCommandHandler(
    INotificationRepository notificationRepo,
    INotificationService notificationService)
    : IRequestHandler<BroadcastNotificationCommand, List<Guid>>
{
    public async Task<List<Guid>> Handle(BroadcastNotificationCommand request, CancellationToken ct)
    {
        var notificationIds = new List<Guid>();

        foreach (var userId in request.Request.UserIds)
        {
            var notification = new Domain.Notification(
                userId,
                request.Request.AdminId,
                request.Request.Title,
                request.Request.Content);

            var created = await notificationRepo.CreateAsync(notification, ct);
            notificationIds.Add(created.Id);

            // Send via SSE if user is connected
            await notificationService.SendToUserAsync(userId, notification);
        }

        await notificationRepo.SaveChangesAsync(ct);
        return notificationIds;
    }
}
