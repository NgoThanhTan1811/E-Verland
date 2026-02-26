using StackExchange.Redis;
using System.Text.Json;

namespace Modules.Redis.Infrastructure;

public sealed class CacheService : ICacheService
{
    private readonly IDatabase _db;
    private readonly IServer _server;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(12);

    public CacheService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
        _server = redis.GetServer(redis.GetEndPoints().FirstOrDefault() ?? throw new InvalidOperationException("No Redis endpoints found"));
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(key))
            return null;

        var value = await _db.StringGetAsync(key);

        if (!value.HasValue)
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch
        {
            // If deserialization fails, remove the corrupted key
            await _db.KeyDeleteAsync(key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value) where T : class
    {
        await SetAsync(key, value, _defaultExpiration);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
    {
        if (string.IsNullOrEmpty(key) || value == null)
            return;

        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiration);
    }

    public async Task RemoveAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return await _db.KeyExistsAsync(key);
    }

    public async Task ClearAllAsync()
    {
        var endpoints = _server.Multiplexer.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _server.Multiplexer.GetServer(endpoint);
            await server.FlushDatabaseAsync();
        }
    }
}
