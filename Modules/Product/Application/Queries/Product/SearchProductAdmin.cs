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

public sealed record SearchProductAdminQuery(FilterProductAdminRequestDto Filter) : IRequest<PageResult<ProductAdminListItemDto>>;

public sealed class SearchProductAdminHandler(
    IProductRepository productRepository,
    ICacheService cacheService,
    IConfiguration configuration) : IRequestHandler<SearchProductAdminQuery, PageResult<ProductAdminListItemDto>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICacheService _cacheService = cacheService;
    private readonly IConfiguration _configuration = configuration;

    public async Task<PageResult<ProductAdminListItemDto>> Handle(SearchProductAdminQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey("admin:page", request.Filter);
        var cached = await _cacheService.GetAsync<PageResult<ProductAdminListItemDto>>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var (products, totalCount) = await _productRepository.GetSearchProductsAdminAsync(request.Filter, cancellationToken);
        var productList = products.ToList();

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
        
        var result = Pagination.PaginationResult(dtos, totalCount, request.Filter);
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(ttlMinutes));

        return result;
    }

    private static string BuildCacheKey(string segment, FilterProductAdminRequestDto filter)
    {
        var normalized = JsonSerializer.Serialize(filter);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return $"{segment}:{Convert.ToHexString(bytes)}";
    }
}
