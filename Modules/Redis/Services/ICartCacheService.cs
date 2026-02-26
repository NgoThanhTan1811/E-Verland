namespace Modules.Redis.Services;

/// <summary>
/// Cart cache service interface
/// </summary>
public interface ICartCacheService
{
    /// <summary>
    /// Cache a cart
    /// </summary>
    Task<T> CacheCartAsync<T>(string userId, T cart, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Get cached cart
    /// </summary>
    Task<T?> GetCartAsync<T>(string userId) where T : class;

    /// <summary>
    /// Invalidate cart cache
    /// </summary>
    Task InvalidateCartAsync(string userId);

    /// <summary>
    /// Cache cart count
    /// </summary>
    Task CacheCartCountAsync(string userId, int count, TimeSpan? expiration = null);

    /// <summary>
    /// Get cached cart count
    /// </summary>
    Task<int?> GetCartCountAsync(string userId);
}
