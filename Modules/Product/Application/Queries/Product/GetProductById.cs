using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Response;
using Modules.Product.Application.Mappings;
using Modules.Redis.Services;

namespace Modules.Product.Application.Queries;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDetailDto?>;

public sealed class GetProductByIdHandler(
    IProductRepository productRepository,
    IProductCacheService productCacheService,
    IConfiguration configuration,
    IUrlResolver urlResolver) : IRequestHandler<GetProductByIdQuery, ProductDetailDto?>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IProductCacheService _productCacheService = productCacheService;
    private readonly IConfiguration _configuration = configuration;
    private readonly IUrlResolver _urlResolver = urlResolver;

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

        var dto = await product.ToDetailDtoAsync(_urlResolver, cancellationToken);
        var ttlMinutes = int.TryParse(_configuration["Cache:ProductDetailTtlMinutes"], out var value)
            ? Math.Max(1, value)
            : 15;

        await _productCacheService.CacheProductAsync(cacheKey, dto, TimeSpan.FromMinutes(ttlMinutes));
        return dto;
    }
}
