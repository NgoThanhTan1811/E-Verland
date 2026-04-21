using System.Text.Json;
using Infra.AWS.CloudWatch;
using Infra.AWS.SNS;
using MediatR;
using Microsoft.Extensions.Configuration;
using Modules.Notification.Application.Contracts;
using Modules.Notification.Application.DTOs.Request;

namespace Modules.Notification.Application.Commands;

public sealed record SendNotificationCommand(SendNotificationRequestDto Request)
    : IRequest<Guid>;

public class SendNotificationCommandHandler(
    INotificationRepository notificationRepo,
    INotificationService notificationService,
    ISNSService snsService,
    ICloudWatchService cloudWatch,
    IConfiguration configuration)
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

        await cloudWatch.PutMetricAsync("notification.sent", 1, "Count", ct: ct);

        // Publish to SNS (graceful skip if not configured)
        var topicArn = configuration["AWS:SNS:NotificationTopicArn"]
            ?? configuration["SNS:NotificationTopicArn"]
            ?? Environment.GetEnvironmentVariable("AWS_SNS_NOTIFICATION_TOPIC_ARN");
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
            await cloudWatch.PutMetricAsync("notification.sns.published", 1, "Count", ct: ct);
        }

        return created.Id;
    }
}
