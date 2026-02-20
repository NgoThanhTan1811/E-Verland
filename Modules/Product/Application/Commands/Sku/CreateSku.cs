using MediatR;
using Modules.Product.Application.Abtracsts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Commands;

public sealed record CreateSkuCommand(CreateSkuRequestDto Request) : IRequest<SkuDetailDto>;

public sealed class CreateSkuHandler : IRequestHandler<CreateSkuCommand, SkuDetailDto>
{
    private readonly ISkuRepository _skuRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductDbContext _dbContext;

    public CreateSkuHandler(ISkuRepository skuRepository, IProductRepository productRepository, IProductDbContext dbContext)
    {
        _skuRepository = skuRepository;
        _productRepository = productRepository;
        _dbContext = dbContext;
    }

    public async Task<SkuDetailDto> Handle(CreateSkuCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product with ID '{request.Request.ProductId}' not found.");

        var existingSku = await _skuRepository.GetByCodeAsync(request.Request.SkuCode, cancellationToken);
        if (existingSku != null)
            throw new InvalidOperationException($"SKU with code '{request.Request.SkuCode}' already exists.");

        var sku = new Domain.SKU
        {
            SkuCode = request.Request.SkuCode,
            ProductId = request.Request.ProductId,
            Price = request.Request.Price,
            Stock = request.Request.Stock,
            Url = request.Request.Url,
            IsActive = request.Request.IsActive,
            OptionValues = request.Request.OptionValues
        };

        await _skuRepository.CreateAsync(sku, cancellationToken);
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
