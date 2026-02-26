using Modules.Redis.Infrastructure;

namespace Modules.Redis.Services;

/// <summary>
/// Product cache service implementation
/// </summary>
public sealed class ProductCacheService : IProductCacheService
{
    private readonly ICacheService _cacheService;
    private const string ProductKeyPrefix = "product:";
    private const string ProductsListKeyPrefix = "products:list:";

    public ProductCacheService(ICacheService cacheService)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<T> CacheProductAsync<T>(string productId, T product, TimeSpan? expiration = null) where T : class
    {
        if (string.IsNullOrEmpty(productId) || product == null)
            throw new ArgumentNullException(nameof(productId));

        var key = $"{ProductKeyPrefix}{productId}";
        var cacheExpiration = expiration ?? TimeSpan.FromHours(12);

        await _cacheService.SetAsync(key, product, cacheExpiration);
        return product;
    }

    public async Task<T?> GetProductAsync<T>(string productId) where T : class
    {
        if (string.IsNullOrEmpty(productId))
            return null;

        var key = $"{ProductKeyPrefix}{productId}";
        return await _cacheService.GetAsync<T>(key);
    }

    public async Task CacheProductsAsync<T>(string cacheKey, IEnumerable<T> products, TimeSpan? expiration = null) where T : class
    {
        if (string.IsNullOrEmpty(cacheKey) || products == null)
            return;

        var key = $"{ProductsListKeyPrefix}{cacheKey}";
        var cacheExpiration = expiration ?? TimeSpan.FromHours(12);
        var productList = products.ToList();

        await _cacheService.SetAsync(key, productList, cacheExpiration);
    }

    public async Task<IEnumerable<T>?> GetProductsAsync<T>(string cacheKey) where T : class
    {
        if (string.IsNullOrEmpty(cacheKey))
            return null;

        var key = $"{ProductsListKeyPrefix}{cacheKey}";
        return await _cacheService.GetAsync<List<T>>(key);
    }

    public async Task InvalidateProductAsync(string productId)
    {
        if (string.IsNullOrEmpty(productId))
            return;

        var key = $"{ProductKeyPrefix}{productId}";
        await _cacheService.RemoveAsync(key);
    }

    public async Task InvalidateAllProductsAsync()
    {
        // Clear all cache (in production, implement pattern-based invalidation)
        await _cacheService.ClearAllAsync();
    }
}
