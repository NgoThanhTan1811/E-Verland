namespace Modules.Notification.Application.DTOs.Response;

public record NotificationResponseDto(
    Guid Id,
    Guid UserId,
    Guid AdminId,
    string Title,
    string Content,
    DateTime CreatedAtUtc,
    bool IsRead,
    DateTime? ReadAtUtc
);

public record NotificationEventDto(
    Guid Id,
    string Title,
    string Content,
    DateTime CreatedAtUtc
);
