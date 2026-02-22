using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;
using SharedKernel.Pagination;

namespace Modules.Product.Application.Queries;

public sealed record SearchProductAdminQuery(FilterProductAdminRequestDto Filter) : IRequest<PageResult<ProductAdminListItemDto>>;

public sealed class SearchProductAdminHandler : IRequestHandler<SearchProductAdminQuery, PageResult<ProductAdminListItemDto>>
{
    private readonly IProductRepository _productRepository;

    public SearchProductAdminHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<PageResult<ProductAdminListItemDto>> Handle(SearchProductAdminQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetSearchProductsAdminAsync(request.Filter, cancellationToken);
        var productList = products.ToList();
        var totalCount = productList.Count;

        var dtos = productList.Select(p => new ProductAdminListItemDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            BasePrice = p.BasePrice,
            VirtualPrice = p.VirtualPrice,
            BrandName = p.Brand?.Name,
            BrandId = p.BrandId,
            CategoryNames = p.Categories.Select(c => c.Name).ToList(),
            CategoryId = p.Categories.FirstOrDefault()?.Id ?? Guid.Empty,
            Attributes = p.Attributes,
            SKUs = p.SKUs,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt ?? DateTime.UtcNow
        }).ToList();

        return Pagination.PaginationResult(dtos, totalCount, request.Filter);
    }
}
