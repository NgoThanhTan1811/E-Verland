using Modules.Redis.Infrastructure;

namespace Modules.Redis.Services;

/// <summary>
/// Cart cache service implementation
/// </summary>
public sealed class CartCacheService : ICartCacheService
{
    private readonly ICacheService _cacheService;
    private const string CartKeyPrefix = "cart:";
    private const string CartCountKeyPrefix = "cart:count:";

    public CartCacheService(ICacheService cacheService)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<T> CacheCartAsync<T>(string userId, T cart, TimeSpan? expiration = null) where T : class
    {
        if (string.IsNullOrEmpty(userId) || cart == null)
            throw new ArgumentNullException(nameof(userId));

        var key = $"{CartKeyPrefix}{userId}";
        var cacheExpiration = expiration ?? TimeSpan.FromHours(12);

        await _cacheService.SetAsync(key, cart, cacheExpiration);
        return cart;
    }

    public async Task<T?> GetCartAsync<T>(string userId) where T : class
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        var key = $"{CartKeyPrefix}{userId}";
        return await _cacheService.GetAsync<T>(key);
    }

    public async Task InvalidateCartAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        var key = $"{CartKeyPrefix}{userId}";
        await _cacheService.RemoveAsync(key);

        // Also invalidate cart count
        var countKey = $"{CartCountKeyPrefix}{userId}";
        await _cacheService.RemoveAsync(countKey);
    }

    public async Task CacheCartCountAsync(string userId, int count, TimeSpan? expiration = null)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        var key = $"{CartCountKeyPrefix}{userId}";
        var cacheExpiration = expiration ?? TimeSpan.FromHours(12);

        await _cacheService.SetAsync<object>(key, count, cacheExpiration);
    }

    public async Task<int?> GetCartCountAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        var key = $"{CartCountKeyPrefix}{userId}";
        var result = await _cacheService.GetAsync<object>(key);
        return result != null ? (int?)result : null;
    }
}
