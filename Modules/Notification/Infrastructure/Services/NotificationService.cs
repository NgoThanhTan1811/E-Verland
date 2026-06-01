using System.Text.Json;
using Modules.Notification.Application.Contracts;

namespace Modules.Notification.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly Dictionary<Guid, Stream> _userConnections = new();
    private readonly Lock _lockObject = new();
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public void RegisterUserConnection(Guid userId, Stream stream)
    {
        lock (_lockObject)
        {
            _userConnections[userId] = stream;
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
        Stream? stream;
        lock (_lockObject)
        {
            _userConnections.TryGetValue(userId, out stream);
        }

        if (stream != null)
        {
            try
            {
                var eventData = FormatSSEMessage(notification);
                var buffer = System.Text.Encoding.UTF8.GetBytes(eventData + "\n\n");

                // Dùng WriteAsync và FlushAsync thay cho WriteLine/Flush
                await stream.WriteAsync(buffer);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
                UnregisterUserConnection(userId);
            }
        }
        else
        {
            _logger.LogInformation("User {UserId} is not connected. Skipping SSE notification.", userId);
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
