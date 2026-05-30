using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Response;
using ProductEntity = Modules.Product.Domain.Product;

namespace Modules.Product.Application.Mappings;

public static class ProductDtoMapper
{
    private const string DefaultImageSize = "md";

    public static async Task<ProductListItemDto> ToListItemDtoAsync(this ProductEntity product, IUrlResolver urlResolver, CancellationToken ct = default)
    {
        return new ProductListItemDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.VirtualPrice > 0 ? product.VirtualPrice : product.BasePrice,
            ImageUrl = await urlResolver.ResolveAsync(product.ImageUrls.FirstOrDefault(), DefaultImageSize, ct),
            BrandName = product.Brand?.Name,
            BrandId = product.BrandId,
            CategoryNames = product.Categories.Select(c => c.Name).ToList(),
            CategoryId = product.Categories.FirstOrDefault()?.Id ?? Guid.Empty,
            Attributes = product.Attributes,
            SKUs = product.SKUs,
            Status = product.Status
        };
    }

    public static async Task<ProductDetailDto> ToDetailDtoAsync(this ProductEntity product, IUrlResolver urlResolver, CancellationToken ct = default)
    {
        var imageUrls = await Task.WhenAll(product.ImageUrls.Select(path => urlResolver.ResolveAsync(path, DefaultImageSize, ct)));

        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            BasePrice = product.BasePrice,
            VirtualPrice = product.VirtualPrice,
            ImageUrls = imageUrls.Where(url => !string.IsNullOrWhiteSpace(url)).Select(url => url!).ToList(),
            Attributes = product.Attributes,
            Brand = product.Brand,
            Categories = product.Categories,
            Skus = product.SKUs
        };
    }
}