using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Response;
using ProductEntity = Modules.Product.Domain.Product;

namespace Modules.Product.Application.Mappings;

public static class ProductDtoMapper
{
    private const string DefaultImageSize = "md";

    public static async Task<ProductListItemDto> ToListItemDtoAsync(this ProductEntity product, IUrlResolver urlResolver, CancellationToken ct = default)
    {
        var imageUrls = new List<string>();
        foreach (var path in product.ImageUrls)
        {
            var url = await urlResolver.ResolveAsync(path, null, ct);
            if (!string.IsNullOrWhiteSpace(url))
                imageUrls.Add(url);
        }

        return new ProductListItemDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.VirtualPrice > 0 ? product.VirtualPrice : product.BasePrice,
            ImageUrls = imageUrls,
            BrandName = product.Brand?.Name,
            BrandId = product.BrandId,
            CategoryNames = product.Categories.Select(c => c.Name).ToList(),
            CategoryId = product.Categories.FirstOrDefault()?.Id ?? Guid.Empty,
            Attributes = product.Attributes,
            SKUs = product.SKUs.Select(ToSkuDetailDto).ToList(),
            Status = product.Status
        };
    }

    public static async Task<ProductDetailDto> ToDetailDtoAsync(this ProductEntity product, IUrlResolver urlResolver, CancellationToken ct = default)
    {
        var imageUrls = new List<string?>();
        foreach (var path in product.ImageUrls)
        {
            imageUrls.Add(await urlResolver.ResolveAsync(path, DefaultImageSize, ct));
        }

        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.VirtualPrice > 0 ? product.VirtualPrice : product.BasePrice,
            ImageUrls = imageUrls.Where(url => !string.IsNullOrWhiteSpace(url)).Select(url => url!).ToList(),
            Attributes = product.Attributes,
            Brand = product.Brand == null ? null : new ProductBrandDto
            {
                Id = product.Brand.Id,
                Name = product.Brand.Name
            },
            Categories = product.Categories.Select(c => new ProductCategoryDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList(),
            Skus = product.SKUs.Select(ToSkuDetailDto).ToList()
        };
    }

    public static BrandDetailDto ToBrandDetailDto(this Modules.Product.Domain.Brand brand)
    {
        return new BrandDetailDto
        {
            Id = brand.Id,
            Name = brand.Name,
            Slug = brand.Slug,
            CreatedAt = brand.CreatedAt,
            UpdatedAt = brand.UpdatedAt
        };
    }

    public static CategoryListItemDto ToCategoryListItemDto(this Modules.Product.Domain.Category category)
    {
        return new CategoryListItemDto
        {
            Id = category.Id,
            Name = category.Name,
            ParentCategoryId = category.ParentCategoryId,
            CreatedAt = category.CreatedAt
        };
    }

    public static SkuAdminListItemDto ToSkuAdminListItemDto(this Modules.Product.Domain.SKU sku)
    {
        return new SkuAdminListItemDto
        {
            Id = sku.Id,
            SkuCode = sku.SkuCode,
            Price = sku.Price,
            Stock = sku.Stock,
            OptionValues = sku.OptionValues,
            ProductName = sku.Product?.Name ?? string.Empty
        };
    }

    public static SkuDetailDto ToSkuDetailDto(this Modules.Product.Domain.SKU sku)
    {
        return new SkuDetailDto
        {
            Id = sku.Id,
            SkuCode = sku.SkuCode,
            ProductId = sku.ProductId,
            ProductName = sku.Product?.Name ?? string.Empty,
            Price = sku.Price,
            Stock = sku.Stock,
            Url = sku.Url,
            IsActive = sku.IsActive,
            OptionValues = sku.OptionValues
        };
    }
}