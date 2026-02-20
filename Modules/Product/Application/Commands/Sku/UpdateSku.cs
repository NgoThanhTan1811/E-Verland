using MediatR;
using Modules.Product.Application.Abtracsts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Commands;

public sealed record UpdateSkuCommand(Guid Id, UpdateSkuRequestDto Request) : IRequest<SkuDetailDto>;

public sealed class UpdateSkuHandler : IRequestHandler<UpdateSkuCommand, SkuDetailDto>
{
    private readonly ISkuRepository _skuRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductDbContext _dbContext;

    public UpdateSkuHandler(ISkuRepository skuRepository, IProductRepository productRepository, IProductDbContext dbContext)
    {
        _skuRepository = skuRepository;
        _productRepository = productRepository;
        _dbContext = dbContext;
    }

    public async Task<SkuDetailDto> Handle(UpdateSkuCommand request, CancellationToken cancellationToken)
    {
        var sku = await _skuRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"SKU with ID '{request.Id}' not found.");

        var product = await _productRepository.GetByIdAsync(sku.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product with ID '{sku.ProductId}' not found.");

        if (sku.SkuCode != request.Request.SkuCode)
        {
            var existingSku = await _skuRepository.GetByCodeAsync(request.Request.SkuCode, cancellationToken);
            if (existingSku != null)
                throw new InvalidOperationException($"SKU with code '{request.Request.SkuCode}' already exists.");
        }

        sku.SkuCode = request.Request.SkuCode;
        sku.Price = request.Request.Price;
        sku.Stock = request.Request.Stock;
        sku.Url = request.Request.Url;
        sku.IsActive = request.Request.IsActive;
        sku.OptionValues = request.Request.OptionValues;

        await _skuRepository.UpdateAsync(sku, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SkuDetailDto
        {
            Id = sku.Id,
            SkuCode = sku.SkuCode,
            ProductId = sku.ProductId,
            ProductName = product.Name,
            Price = sku.Price,
            Stock = sku.Stock,
            Url = sku.Url,
            IsActive = sku.IsActive,
            OptionValues = sku.OptionValues
        };
    }
}
