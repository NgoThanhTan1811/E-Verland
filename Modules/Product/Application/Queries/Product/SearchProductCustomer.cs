using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;
using SharedKernel.Pagination;

namespace Modules.Product.Application.Queries;

public sealed record SearchProductCustomerQuery(FilterProductCustomerRequestDto Filter) : IRequest<PageResult<ProductListItemDto>>;

public sealed class SearchProductCustomerHandler : IRequestHandler<SearchProductCustomerQuery, PageResult<ProductListItemDto>>
{
    private readonly IProductRepository _productRepository;

    public SearchProductCustomerHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<PageResult<ProductListItemDto>> Handle(SearchProductCustomerQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetSearchProductsCustomerAsync(request.Filter, cancellationToken);
        var productList = products.ToList();
        var totalCount = productList.Count;

        var dtos = productList.Select(p => new ProductListItemDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.VirtualPrice > 0 ? p.VirtualPrice : p.BasePrice,
            ImageUrl = p.ImageUrls.FirstOrDefault(),
            BrandName = p.Brand?.Name,
            BrandId = p.BrandId,
            CategoryNames = p.Categories.Select(c => c.Name).ToList(),
            CategoryId = p.Categories.FirstOrDefault()?.Id ?? Guid.Empty,
            Attributes = p.Attributes,
            SKUs = p.SKUs,
            Status = p.Status
        }).ToList();

        return Pagination.PaginationResult(dtos, totalCount, request.Filter);
    }
}
