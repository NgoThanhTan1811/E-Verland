using Amazon.XRay.Recorder.Core;
using Infra.AWS.CloudWatch;
using MediatR;
using Modules.Media.Application.Interfaces;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;
using Modules.Product.Application.Mappings;
using Modules.Product.Application.Services;
using Modules.Redis.Services;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Modules.Product.Application.Commands;

public sealed record CreateProductCommand(CreateProductRequestDto Request, Guid? ShopId = null, string? ShopName = null) : IRequest<ProductDetailDto>;

public sealed class CreateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    ISkuRepository skuRepository,
    IProductDbContext dbContext,
    SKUGeneratorService skuGenerator,
    IProductSyncPublisher syncPublisher,
    ICloudWatchService cloudWatch,
    IProductCacheService productCacheService,
    IMediaFileRepository mediaFileRepository) : IRequestHandler<CreateProductCommand, ProductDetailDto>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ISkuRepository _skuRepository = skuRepository;
    private readonly IProductDbContext _dbContext = dbContext;
    private readonly SKUGeneratorService _skuGenerator = skuGenerator;
    private readonly IProductSyncPublisher _syncPublisher = syncPublisher;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;
    private readonly IProductCacheService _productCacheService = productCacheService;
    private readonly IMediaFileRepository _mediaFileRepository = mediaFileRepository;

    public async Task<ProductDetailDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        await ValidateImagePathsAsync(request.Request.ImageUrls, cancellationToken);

        var categories = new List<Domain.Category>();
        if (request.Request.CategoryIds?.Count != 0)
        {
            foreach (var categoryId in request.Request.CategoryIds!)
            {
                var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken)
                     ?? throw new KeyNotFoundException($"Category with ID '{categoryId}' not found.");

                if (_dbContext is Microsoft.EntityFrameworkCore.DbContext efDbContext)
                {
                    efDbContext.Attach(category);
                }

                categories.Add(category);
            }
        }

        var product = new Domain.Product
        {
            Name = request.Request.Name,
            Description = request.Request.Description,
            BasePrice = request.Request.BasePrice,
            VirtualPrice = request.Request.VirtualPrice,
            Slug = SlugHelper.GenerateSlug(request.Request.Name),
            ImageUrls = request.Request.ImageUrls,
            Attributes = request.Request.Attributes,
            BrandId = request.Request.BrandId,
            Status = Domain.ProductStatus.Published,
            Categories = categories,
            ShopId = request.ShopId,
            ShopName = request.ShopName
        };

        AWSXRayRecorder.Instance.BeginSubsegment("Product.DB");
        try
        {
            await _productRepository.CreateAsync(product, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            AWSXRayRecorder.Instance.AddException(ex);
            throw;
        }
        finally
        {
            AWSXRayRecorder.Instance.EndSubsegment();
        }

        // Auto-generate SKUs if variants are provided
        if (request.Request.Variants != null && request.Request.Variants.Count != 0)
        {
            if (request.Request.Stock <= 0)
                throw new InvalidOperationException("Stock must be greater than 0 when creating variant SKUs.");

            var skuOptions = request.Request.Variants.Select(v =>
                new SKUGeneratorService.SKUOption(v.Key, v.Values)).ToList();

            var generatedSkus = _skuGenerator.GenerateSKUs(skuOptions);

            foreach (var generatedSku in generatedSkus)
            {
                var sku = new Domain.SKU
                {
                    SkuCode = generatedSku.Code,
                    ProductId = product.Id,
                    Price = 0,
                    Stock = request.Request.Stock,
                    Url = string.Empty,
                    IsActive = true,
                    OptionValues = generatedSku.OptionValues
                };

                await _skuRepository.CreateAsync(sku, cancellationToken);
                product.SKUs.Add(sku);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await _syncPublisher.PublishAsync(product, "Created", cancellationToken);
        await _cloudWatch.PutMetricAsync("product.created", 1, "Count", ct: cancellationToken);

        foreach (var imagePath in request.Request.ImageUrls)
        {
            await _mediaFileRepository.ConfirmByPathAsync(imagePath, cancellationToken);
        }

        await _productCacheService.InvalidateProductAsync(product.Id.ToString("N"));
        await _productCacheService.InvalidateAllProductsAsync();

        return MapToDetailDto(product, categories);
    }

    private static ProductDetailDto MapToDetailDto(Domain.Product product, List<Domain.Category> categories)
    {
        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.VirtualPrice > 0 ? product.VirtualPrice : product.BasePrice,
            BasePrice = product.BasePrice,
            VirtualPrice = product.VirtualPrice,
            Status = product.Status,
            ImageUrls = product.ImageUrls,
            Attributes = product.Attributes,
            Brand = product.Brand == null ? null : new ProductBrandDto
            {
                Id = product.Brand.Id,
                Name = product.Brand.Name
            },
            Categories = categories.Select(c => new ProductCategoryDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList(),
            Skus = product.SKUs.Select(ProductDtoMapper.ToSkuDetailDto).ToList()
            ,
            ShopName = product.ShopName
        };
    }

    private async Task ValidateImagePathsAsync(IEnumerable<string> imagePaths, CancellationToken ct)
    {
        foreach (var imagePath in imagePaths)
        {
            if (!IsValidRelativePath(imagePath))
                throw new InvalidOperationException($"Invalid image relative path: '{imagePath}'. External URLs are not allowed.");

            var media = await _mediaFileRepository.GetByPathAsync(imagePath, ct);
            if (media == null)
                throw new InvalidOperationException($"Media path does not exist: '{imagePath}'.");
        }
    }

    private static bool IsValidRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (Uri.TryCreate(path, UriKind.Absolute, out _))
            return false;

        return path.StartsWith("products/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("avatars/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("shops/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("reviews/", StringComparison.OrdinalIgnoreCase);
    }


}
