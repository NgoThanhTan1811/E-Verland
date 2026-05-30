using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;
using Modules.Product.Application.Mappings;
using Modules.Redis.Services;
using SharedKernel.Pagination;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Modules.Product.Application.Queries;

public sealed record SearchProductCustomerQuery(FilterProductCustomerRequestDto Filter) : IRequest<PageResult<ProductListItemDto>>;

public sealed class SearchProductCustomerHandler(
    IProductRepository productRepository,
    IProductCacheService productCacheService,
    IConfiguration configuration,
    IUrlResolver urlResolver) : IRequestHandler<SearchProductCustomerQuery, PageResult<ProductListItemDto>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IProductCacheService _productCacheService = productCacheService;
    private readonly IConfiguration _configuration = configuration;
    private readonly IUrlResolver _urlResolver = urlResolver;

    public async Task<PageResult<ProductListItemDto>> Handle(SearchProductCustomerQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey("customer", request.Filter);
        var cached = await _productCacheService.GetProductsAsync<ProductListItemDto>(cacheKey);
        if (cached is not null)
        {
            var cachedList = cached.ToList();
            return Pagination.PaginationResult(cachedList, cachedList.Count, request.Filter);
        }

        var products = await _productRepository.GetSearchProductsCustomerAsync(request.Filter, cancellationToken);
        var productList = products.ToList();
        var totalCount = productList.Count;

        var dtos = new List<ProductListItemDto>(productList.Count);
        foreach (var product in productList)
        {
            dtos.Add(await product.ToListItemDtoAsync(_urlResolver, cancellationToken));
        }

        var ttlMinutes = int.TryParse(_configuration["Cache:ProductListTtlMinutes"], out var value)
            ? Math.Max(1, value)
            : 10;
        await _productCacheService.CacheProductsAsync(cacheKey, dtos, TimeSpan.FromMinutes(ttlMinutes));

        return Pagination.PaginationResult(dtos, totalCount, request.Filter);
    }

    private static string BuildCacheKey(string segment, FilterProductCustomerRequestDto filter)
    {
        var normalized = JsonSerializer.Serialize(filter);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return $"{segment}:{Convert.ToHexString(bytes)}";
    }
}
