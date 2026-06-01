using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;
using Modules.Product.Application.Services;

namespace Modules.Product.Application.Commands;

public sealed record AddSkusToProductCommand(Guid ProductId, List<ProductVariantDto> Variants, int Stock) : IRequest<List<SkuDetailDto>>;

public sealed class AddSkusToProductHandler(
    IProductRepository productRepository,
    ISkuRepository skuRepository,
    IProductDbContext dbContext,
    SKUGeneratorService skuGenerator) : IRequestHandler<AddSkusToProductCommand, List<SkuDetailDto>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ISkuRepository _skuRepository = skuRepository;
    private readonly IProductDbContext _dbContext = dbContext;
    private readonly SKUGeneratorService _skuGenerator = skuGenerator;

    public async Task<List<SkuDetailDto>> Handle(AddSkusToProductCommand request, CancellationToken cancellationToken)
    {
        if (request.Variants == null || request.Variants.Count == 0)
            throw new InvalidOperationException("At least one variant is required.");

        if (request.Stock <= 0)
            throw new InvalidOperationException("Stock must be greater than 0.");

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product with ID '{request.ProductId}' not found.");

        var skuOptions = request.Variants.Select(variant =>
        {
            if (string.IsNullOrWhiteSpace(variant.Key))
                throw new InvalidOperationException("Variant key is required.");

            var values = variant.Values?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            if (values.Count == 0)
                throw new InvalidOperationException($"Variant '{variant.Key}' must contain at least one value.");

            return new SKUGeneratorService.SKUOption(variant.Key.Trim(), values);
        }).ToList();

        var generatedSkus = _skuGenerator.GenerateSKUs(skuOptions);
        if (generatedSkus.Count == 0)
            throw new InvalidOperationException("Unable to generate SKUs from the provided variants.");

        var createdSkus = new List<Domain.SKU>();

        foreach (var generatedSku in generatedSkus)
        {
            var existingSku = await _skuRepository.GetByCodeAsync(generatedSku.Code, cancellationToken);
            if (existingSku != null)
                throw new InvalidOperationException($"SKU with code '{generatedSku.Code}' already exists.");

            var sku = new Domain.SKU
            {
                SkuCode = generatedSku.Code,
                ProductId = product.Id,
                Price = 0,
                Stock = request.Stock,
                Url = string.Empty,
                IsActive = true,
                OptionValues = generatedSku.OptionValues
            };

            await _skuRepository.CreateAsync(sku, cancellationToken);
            product.SKUs.Add(sku);
            createdSkus.Add(sku);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return createdSkus.Select(sku => new SkuDetailDto
        {
            Id = sku.Id,
            SkuCode = sku.SkuCode,
            ProductId = product.Id,
            ProductName = product.Name,
            Price = sku.Price,
            Stock = sku.Stock,
            Url = sku.Url,
            IsActive = sku.IsActive,
            OptionValues = sku.OptionValues
        }).ToList();
    }
}