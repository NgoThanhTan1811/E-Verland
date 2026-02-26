namespace Modules.Redis.Infrastructure;

/// <summary>
/// Cache service interface for Redis operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get value from cache
    /// </summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Set value in cache with default expiration (24 hours)
    /// </summary>
    Task SetAsync<T>(string key, T value) where T : class;

    /// <summary>
    /// Set value in cache with custom expiration time
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;

    /// <summary>
    /// Remove value from cache
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Clear all cache
    /// </summary>
    Task ClearAllAsync();
}
