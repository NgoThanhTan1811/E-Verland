using Infra.AWS.CloudWatch;
using Infra.AWS.SNS;
using MediatR;
using Microsoft.Extensions.Configuration;
using Modules.Notification.Application.Contracts;
using Modules.Notification.Application.DTOs.Request;

namespace Modules.Notification.Application.Commands;

public sealed record BroadcastNotificationCommand(BroadcastNotificationRequestDto Request)
    : IRequest<List<Guid>>;

public class BroadcastNotificationCommandHandler(
    INotificationRepository notificationRepo,
    INotificationService notificationService,
    ISNSService snsService,
    ICloudWatchService cloudWatch,
    IConfiguration configuration)
    : IRequestHandler<BroadcastNotificationCommand, List<Guid>>
{
    public async Task<List<Guid>> Handle(BroadcastNotificationCommand request, CancellationToken ct)
    {
        var notificationIds = new List<Guid>();
        var topicArn = configuration["SNS:NotificationTopicArn"];

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

            // Publish to SNS (graceful skip if not configured)
            if (!string.IsNullOrWhiteSpace(topicArn))
            {
                var payload = new
                {
                    notificationId = created.Id,
                    userId = created.UserId,
                    title = created.Title,
                    content = created.Content,
                    createdAt = created.CreatedAtUtc
                };

                await snsService.PublishAsync(topicArn, payload, ct: ct);
            }
        }

        await notificationRepo.SaveChangesAsync(ct);

        await cloudWatch.PutMetricAsync("notification.broadcast", 1, "Count", ct: ct);

        return notificationIds;
    }
}
