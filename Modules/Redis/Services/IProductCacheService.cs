namespace Modules.Redis.Services;

/// <summary>
/// Product cache service interface
/// </summary>
public interface IProductCacheService
{
    /// <summary>
    /// Cache a product
    /// </summary>
    Task<T> CacheProductAsync<T>(string productId, T product, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Get cached product
    /// </summary>
    Task<T?> GetProductAsync<T>(string productId) where T : class;

    /// <summary>
    /// Cache list of products
    /// </summary>
    Task CacheProductsAsync<T>(string cacheKey, IEnumerable<T> products, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Get cached products list
    /// </summary>
    Task<IEnumerable<T>?> GetProductsAsync<T>(string cacheKey) where T : class;

    /// <summary>
    /// Invalidate product cache
    /// </summary>
    Task InvalidateProductAsync(string productId);

    /// <summary>
    /// Invalidate all products cache
    /// </summary>
    Task InvalidateAllProductsAsync();
}
