namespace Modules.Redis.Services;

/// <summary>
/// JWT token cache service
/// </summary>
public interface IJwtCacheService
{
    /// <summary>
    /// Cache JWT token
    /// </summary>
    Task CacheTokenAsync(string userId, string token, TimeSpan? expiration = null);

    /// <summary>
    /// Get cached JWT token
    /// </summary>
    Task<string?> GetTokenAsync(string userId);

    /// <summary>
    /// Invalidate JWT token (logout)
    /// </summary>
    Task InvalidateTokenAsync(string userId);

    /// <summary>
    /// Check if token is blacklisted
    /// </summary>
    Task<bool> IsTokenBlacklistedAsync(string token);

    /// <summary>
    /// Blacklist a token
    /// </summary>
    Task BlacklistTokenAsync(string token, TimeSpan expiration);
}
