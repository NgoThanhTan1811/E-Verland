using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Response;
using Modules.Redis.Services;

namespace Modules.Product.Application.Queries;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDetailDto?>;

public sealed class GetProductByIdHandler(
    IProductRepository productRepository,
    IProductCacheService productCacheService,
    IConfiguration configuration) : IRequestHandler<GetProductByIdQuery, ProductDetailDto?>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IProductCacheService _productCacheService = productCacheService;
    private readonly IConfiguration _configuration = configuration;

    public async Task<ProductDetailDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = request.Id.ToString("N");
        var cached = await _productCacheService.GetProductAsync<ProductDetailDto>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
            return null;

        var dto = MapToDetailDto(product);
        var ttlMinutes = int.TryParse(_configuration["Cache:ProductDetailTtlMinutes"], out var value)
            ? Math.Max(1, value)
            : 15;

        await _productCacheService.CacheProductAsync(cacheKey, dto, TimeSpan.FromMinutes(ttlMinutes));
        return dto;
    }

    private static ProductDetailDto MapToDetailDto(Domain.Product product)
    {
        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            BasePrice = product.BasePrice,
            VirtualPrice = product.VirtualPrice,
            ImageUrls = product.ImageUrls,
            Attributes = product.Attributes,
            Brand = product.Brand,
            Categories = product.Categories,
            Skus = product.SKUs
        };
    }
}
