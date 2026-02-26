using System.Text.Json;
using Modules.Notification.Application.Contracts;

namespace Modules.Notification.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly Dictionary<Guid, StreamWriter> _userConnections = new();
    private readonly Lock _lockObject = new();
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public void RegisterUserConnection(Guid userId, StreamWriter writer)
    {
        lock (_lockObject)
        {
            _userConnections[userId] = writer;
            _logger.LogInformation("User {UserId} connected to SSE notifications", userId);
        }
    }

    public void UnregisterUserConnection(Guid userId)
    {
        lock (_lockObject)
        {
            if (_userConnections.Remove(userId))
            {
                _logger.LogInformation("User {UserId} disconnected from SSE notifications", userId);
            }
        }
    }

    public async Task SendToUserAsync(Guid userId, Domain.Notification notification)
    {
        lock (_lockObject)
        {
            if (!_userConnections.TryGetValue(userId, out var writer))
            {
                _logger.LogDebug("User {UserId} is not connected, notification will be stored for later retrieval", userId);
                return;
            }

            try
            {
                var eventData = FormatSSEMessage(notification);
                writer.WriteLine(eventData);
                writer.Flush();
                _logger.LogInformation("Notification sent to user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
                _userConnections.Remove(userId);
            }
        }
    }

    public async Task BroadcastToUsersAsync(IEnumerable<Guid> userIds, Domain.Notification notification)
    {
        var tasks = userIds.Select(userId => SendToUserAsync(userId, notification));
        await Task.WhenAll(tasks);
    }

    public bool IsUserConnected(Guid userId)
    {
        lock (_lockObject)
        {
            return _userConnections.ContainsKey(userId);
        }
    }

    public IEnumerable<Guid> GetConnectedUsers()
    {
        lock (_lockObject)
        {
            return _userConnections.Keys.ToList();
        }
    }

    private static string FormatSSEMessage(Domain.Notification notification)
    {
        var sseData = new
        {
            id = notification.Id,
            title = notification.Title,
            content = notification.Content,
            createdAt = notification.CreatedAtUtc,
            adminId = notification.AdminId
        };

        var json = JsonSerializer.Serialize(sseData);
        return $"data: {json}\n\n";
    }
}
