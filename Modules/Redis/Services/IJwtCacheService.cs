namespace Modules.Redis.Services;


public interface IJwtCacheService
{

    Task CacheTokenAsync(string userId, string token, TimeSpan? expiration = null);

    Task<string?> GetTokenAsync(string userId);

    Task InvalidateTokenAsync(string userId);

    Task<bool> IsTokenBlacklistedAsync(string token);

    Task BlacklistTokenAsync(string token, TimeSpan expiration);
}
