using MediatR;
using Modules.Product.Application.Abtracsts;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Queries;

public sealed record GetSkuByIdQuery(Guid Id) : IRequest<SkuDetailDto?>;

public sealed class GetSkuByIdHandler : IRequestHandler<GetSkuByIdQuery, SkuDetailDto?>
{
    private readonly ISkuRepository _skuRepository;

    public GetSkuByIdHandler(ISkuRepository skuRepository)
    {
        _skuRepository = skuRepository;
    }

    public async Task<SkuDetailDto?> Handle(GetSkuByIdQuery request, CancellationToken cancellationToken)
    {
        var sku = await _skuRepository.GetByIdWithProductAsync(request.Id, cancellationToken);
        if (sku == null)
            return null;

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
