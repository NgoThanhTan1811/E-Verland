using System.Reflection;
using System.Text.Json;
using Modules.Notification.Application.Contracts;
using StackExchange.Redis;
using RedisOrder = StackExchange.Redis.Order;

namespace Modules.Notification.Infrastructure.Repository;

public class RedisNotificationRepository(IConnectionMultiplexer redis) : INotificationRepository
{
    private readonly IDatabase _db = redis.GetDatabase();

    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

    // DTO for Redis serialization (avoids private-setter issues)
    private sealed record NotificationRedisDto(
        Guid Id,
        Guid UserId,
        Guid AdminId,
        string Title,
        string Content,
        DateTime CreatedAtUtc,
        bool IsRead,
        DateTime? ReadAtUtc);

    private static string NotificationKey(Guid id) => $"notification:{id}";
    private static string UserNotificationsKey(Guid userId) => $"user_notifications:{userId}";

    private static NotificationRedisDto ToDto(Domain.Notification n) =>
        new(n.Id, n.UserId, n.AdminId, n.Title, n.Content, n.CreatedAtUtc, n.IsRead, n.ReadAtUtc);

    private static Domain.Notification FromDto(NotificationRedisDto dto)
    {
        // Use reflection to set private properties on the domain object
        var n = (Domain.Notification)Activator.CreateInstance(typeof(Domain.Notification), nonPublic: true)!;
        Set(n, nameof(Domain.Notification.Id), dto.Id);
        Set(n, nameof(Domain.Notification.UserId), dto.UserId);
        Set(n, nameof(Domain.Notification.AdminId), dto.AdminId);
        Set(n, nameof(Domain.Notification.Title), dto.Title);
        Set(n, nameof(Domain.Notification.Content), dto.Content);
        Set(n, nameof(Domain.Notification.CreatedAtUtc), dto.CreatedAtUtc);
        Set(n, nameof(Domain.Notification.IsRead), dto.IsRead);
        Set(n, nameof(Domain.Notification.ReadAtUtc), dto.ReadAtUtc);
        return n;
    }

    private static void Set(Domain.Notification n, string propertyName, object? value)
    {
        typeof(Domain.Notification)
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(n, value);
    }

    private static Domain.Notification? DeserializeHash(HashEntry[] entries)
    {
        if (entries.Length == 0) return null;

        var dict = entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());

        if (!dict.TryGetValue("Json", out var json) || string.IsNullOrEmpty(json))
            return null;

        var dto = JsonSerializer.Deserialize<NotificationRedisDto>(json);
        return dto is null ? null : FromDto(dto);
    }

    public async Task<Domain.Notification> CreateAsync(Domain.Notification notification, CancellationToken ct = default)
    {
        var dto = ToDto(notification);
        var json = JsonSerializer.Serialize(dto);
        var key = NotificationKey(notification.Id);

        await _db.HashSetAsync(key, [new HashEntry("Json", json)]);
        await _db.KeyExpireAsync(key, Ttl);

        var score = new DateTimeOffset(notification.CreatedAtUtc, TimeSpan.Zero).ToUnixTimeSeconds();
        await _db.SortedSetAddAsync(UserNotificationsKey(notification.UserId), notification.Id.ToString(), score);

        return notification;
    }

    public async Task<Domain.Notification?> GetByIdAsync(Guid notificationId, CancellationToken ct = default)
    {
        var entries = await _db.HashGetAllAsync(NotificationKey(notificationId));
        return DeserializeHash(entries);
    }

    public async Task<List<Domain.Notification>> GetByUserIdAsync(Guid userId, int take = 50, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        var ids = await _db.SortedSetRangeByRankAsync(UserNotificationsKey(userId), 0, take - 1, RedisOrder.Descending);

        var results = new List<Domain.Notification>(ids.Length);
        foreach (var id in ids)
        {
            var entries = await _db.HashGetAllAsync(NotificationKey(Guid.Parse((string)id!)));
            var n = DeserializeHash(entries);
            if (n is not null) results.Add(n);
        }
        return results;
    }

    public async Task<List<Domain.Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        // Get all notifications for user then filter
        var ids = await _db.SortedSetRangeByRankAsync(UserNotificationsKey(userId), 0, -1, RedisOrder.Descending);

        var results = new List<Domain.Notification>();
        foreach (var id in ids)
        {
            var entries = await _db.HashGetAllAsync(NotificationKey(Guid.Parse((string)id!)));
            var n = DeserializeHash(entries);
            if (n is not null && !n.IsRead) results.Add(n);
        }
        return results;
    }

    public async Task<Domain.Notification?> MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        var notification = await GetByIdAsync(notificationId, ct);
        if (notification is null) return null;

        notification.MarkAsRead();

        var dto = ToDto(notification);
        var json = JsonSerializer.Serialize(dto);
        var key = NotificationKey(notificationId);

        await _db.HashSetAsync(key, [new HashEntry("Json", json)]);
        // Refresh TTL
        await _db.KeyExpireAsync(key, Ttl);

        return notification;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
}
