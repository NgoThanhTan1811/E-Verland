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

public sealed record SearchProductAdminQuery(FilterProductAdminRequestDto Filter) : IRequest<PageResult<ProductAdminListItemDto>>;

public sealed class SearchProductAdminHandler(
    IProductRepository productRepository,
    IProductCacheService productCacheService,
    IConfiguration configuration) : IRequestHandler<SearchProductAdminQuery, PageResult<ProductAdminListItemDto>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IProductCacheService _productCacheService = productCacheService;
    private readonly IConfiguration _configuration = configuration;

    public async Task<PageResult<ProductAdminListItemDto>> Handle(SearchProductAdminQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey("admin", request.Filter);
        var cached = await _productCacheService.GetProductsAsync<ProductAdminListItemDto>(cacheKey);
        if (cached is not null)
        {
            var cachedList = cached.ToList();
            return Pagination.PaginationResult(cachedList, cachedList.Count, request.Filter);
        }

        var products = await _productRepository.GetSearchProductsAdminAsync(request.Filter, cancellationToken);
        var productList = products.ToList();
        var totalCount = productList.Count;

        var dtos = productList.Select(p => new ProductAdminListItemDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            ShopId = p.ShopId,
            ShopName = p.ShopName,
            BasePrice = p.BasePrice,
            VirtualPrice = p.VirtualPrice,
            BrandName = p.Brand?.Name,
            BrandId = p.BrandId,
            CategoryNames = p.Categories.Select(c => c.Name).ToList(),
            CategoryId = p.Categories.FirstOrDefault()?.Id ?? Guid.Empty,
            Attributes = p.Attributes,
            SKUs = p.SKUs.Select(ProductDtoMapper.ToSkuAdminListItemDto).ToList(),
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt ?? DateTime.UtcNow
        }).ToList();

        var ttlMinutes = int.TryParse(_configuration["Cache:ProductListTtlMinutes"], out var value)
            ? Math.Max(1, value)
            : 10;
        await _productCacheService.CacheProductsAsync(cacheKey, dtos, TimeSpan.FromMinutes(ttlMinutes));

        return Pagination.PaginationResult(dtos, totalCount, request.Filter);
    }

    private static string BuildCacheKey(string segment, FilterProductAdminRequestDto filter)
    {
        var normalized = JsonSerializer.Serialize(filter);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return $"{segment}:{Convert.ToHexString(bytes)}";
    }
}
