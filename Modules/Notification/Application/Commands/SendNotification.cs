using MediatR;
using Modules.Notification.Application.DTOs.Request;
using Modules.Notification.Application.Contracts;

namespace Modules.Notification.Application.Commands;

public sealed record SendNotificationCommand(SendNotificationRequestDto Request)
    : IRequest<Guid>;

public class SendNotificationCommandHandler(
    INotificationRepository notificationRepo,
    INotificationService notificationService)
    : IRequestHandler<SendNotificationCommand, Guid>
{
    public async Task<Guid> Handle(SendNotificationCommand request, CancellationToken ct)
    {
        var notification = new Domain.Notification(
            request.Request.UserId,
            request.Request.AdminId,
            request.Request.Title,
            request.Request.Content);

        var created = await notificationRepo.CreateAsync(notification, ct);
        await notificationRepo.SaveChangesAsync(ct);

        // Send via SSE if user is connected
        await notificationService.SendToUserAsync(request.Request.UserId, notification);

        return created.Id;
    }
}
