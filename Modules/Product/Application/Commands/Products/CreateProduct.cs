using Amazon.XRay.Recorder.Core;
using Infra.AWS.CloudWatch;
using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;
using Modules.Product.Application.Services;
using Modules.Redis.Services;

namespace Modules.Product.Application.Commands;

public sealed record CreateProductCommand(CreateProductRequestDto Request) : IRequest<ProductDetailDto>;

public sealed class CreateProductHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    ISkuRepository skuRepository,
    IProductDbContext dbContext,
    SKUGeneratorService skuGenerator,
    IProductSyncPublisher syncPublisher,
    ICloudWatchService cloudWatch,
    IProductCacheService productCacheService) : IRequestHandler<CreateProductCommand, ProductDetailDto>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ISkuRepository _skuRepository = skuRepository;
    private readonly IProductDbContext _dbContext = dbContext;
    private readonly SKUGeneratorService _skuGenerator = skuGenerator;
    private readonly IProductSyncPublisher _syncPublisher = syncPublisher;
    private readonly ICloudWatchService _cloudWatch = cloudWatch;
    private readonly IProductCacheService _productCacheService = productCacheService;

    public async Task<ProductDetailDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var categories = new List<Domain.Category>();
        if (request.Request.CategoryIds.Count != 0)
        {
            foreach (var categoryId in request.Request.CategoryIds)
            {
                var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken)
                     ?? throw new KeyNotFoundException($"Category with ID '{categoryId}' not found.");
                categories.Add(category);
            }
        }

        var product = new Domain.Product
        {
            Name = request.Request.Name,
            Description = request.Request.Description,
            BasePrice = request.Request.BasePrice,
            VirtualPrice = request.Request.VirtualPrice,
            Slug = request.Request.Slug,
            ImageUrls = request.Request.ImageUrls,
            Attributes = request.Request.Attributes,
            BrandId = request.Request.BrandId,
            Status = Domain.ProductStatus.Draft,
            Categories = categories
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
                    Stock = 100,
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
            Slug = product.Slug,
            Description = product.Description,
            BasePrice = product.BasePrice,
            VirtualPrice = product.VirtualPrice,
            ImageUrls = product.ImageUrls,
            Attributes = product.Attributes,
            Brand = product.Brand,
            Categories = categories,
            Skus = product.SKUs
        };
    }
}
