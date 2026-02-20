using MediatR;
using Modules.Product.Application.Abtracsts;
using Modules.Product.Application.DTOs.Response;

namespace Modules.Product.Application.Queries;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDetailDto?>;

public sealed class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDetailDto?>
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDetailDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
            return null;

        return MapToDetailDto(product);
    }

    private static ProductDetailDto MapToDetailDto(Domain.Product product)
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
            Categories = product.Categories,
            Skus = product.SKUs
        };
    }
}
