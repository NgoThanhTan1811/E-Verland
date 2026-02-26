namespace Modules.Notification.Application.DTOs.Request;

public record SendNotificationRequestDto(
    Guid UserId,
    Guid AdminId,
    string Title,
    string Content
);

public record BroadcastNotificationRequestDto(
    IEnumerable<Guid> UserIds,
    Guid AdminId,
    string Title,
    string Content
);
