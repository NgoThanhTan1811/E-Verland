using MediatR;
using Modules.Product.Application.Abtracsts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;
using Modules.Product.Application.Services;

namespace Modules.Product.Application.Commands;

public sealed record CreateProductCommand(CreateProductRequestDto Request) : IRequest<ProductDetailDto>;

public sealed class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDetailDto>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISkuRepository _skuRepository;
    private readonly IProductDbContext _dbContext;
    private readonly SKUGeneratorService _skuGenerator;

    public CreateProductHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ISkuRepository skuRepository,
        IProductDbContext dbContext,
        SKUGeneratorService skuGenerator)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _skuRepository = skuRepository;
        _dbContext = dbContext;
        _skuGenerator = skuGenerator;
    }

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
            Status = request.Request.Status,
            Categories = categories
        };

        await _productRepository.CreateAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

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
