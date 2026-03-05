using Modules.Redis.Infrastructure;

namespace Modules.Redis.Services;

public sealed class JwtCacheService : IJwtCacheService
{
    private readonly ICacheService _cacheService;
    private const string TokenKeyPrefix = "jwt:token:";
    private const string BlacklistKeyPrefix = "jwt:blacklist:";

    public JwtCacheService(ICacheService cacheService)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task CacheTokenAsync(string userId, string token, TimeSpan? expiration = null)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            return;

        var key = $"{TokenKeyPrefix}{userId}";
        var cacheExpiration = expiration ?? TimeSpan.FromHours(12);

        await _cacheService.SetAsync(key, token, cacheExpiration);
    }

    public async Task<string?> GetTokenAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        var key = $"{TokenKeyPrefix}{userId}";
        return await _cacheService.GetAsync<string>(key);
    }

    public async Task InvalidateTokenAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        var key = $"{TokenKeyPrefix}{userId}";
        await _cacheService.RemoveAsync(key);
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        var key = $"{BlacklistKeyPrefix}{token}";
        return await _cacheService.ExistsAsync(key);
    }

    public async Task BlacklistTokenAsync(string token, TimeSpan expiration)
    {
        if (string.IsNullOrEmpty(token))
            return;

        var key = $"{BlacklistKeyPrefix}{token}";
        await _cacheService.SetAsync<object>(key, true, expiration);
    }
}
