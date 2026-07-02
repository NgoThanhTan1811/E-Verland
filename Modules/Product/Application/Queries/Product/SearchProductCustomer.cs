using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;
using Modules.Product.Application.Mappings;
using Modules.Redis.Infrastructure;
using SharedKernel.Pagination;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Modules.Product.Application.Queries;

public sealed record SearchProductCustomerQuery(FilterProductCustomerRequestDto Filter) : IRequest<PageResult<ProductListItemDto>>;

public sealed class SearchProductCustomerHandler(
    IProductRepository productRepository,
    ICacheService cacheService,
    IConfiguration configuration,
    IUrlResolver urlResolver) : IRequestHandler<SearchProductCustomerQuery, PageResult<ProductListItemDto>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICacheService _cacheService = cacheService;
    private readonly IConfiguration _configuration = configuration;
    private readonly IUrlResolver _urlResolver = urlResolver;

    public async Task<PageResult<ProductListItemDto>> Handle(SearchProductCustomerQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey("customer:page", request.Filter);
        var cached = await _cacheService.GetAsync<PageResult<ProductListItemDto>>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var (products, totalCount) = await _productRepository.GetSearchProductsCustomerAsync(request.Filter, cancellationToken);
        var productList = products.ToList();

        var dtos = new List<ProductListItemDto>(productList.Count);
        foreach (var product in productList)
        {
            dtos.Add(await product.ToListItemDtoAsync(_urlResolver, cancellationToken));
        }

        var ttlMinutes = int.TryParse(_configuration["Cache:ProductListTtlMinutes"], out var value)
            ? Math.Max(1, value)
            : 10;
        
        var result = Pagination.PaginationResult(dtos, totalCount, request.Filter);
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(ttlMinutes));

        return result;
    }

    private static string BuildCacheKey(string segment, FilterProductCustomerRequestDto filter)
    {
        var normalized = JsonSerializer.Serialize(filter);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return $"{segment}:{Convert.ToHexString(bytes)}";
    }
}
