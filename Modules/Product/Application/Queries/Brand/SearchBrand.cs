using MediatR;
using Modules.Product.Application.Contracts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;
using SharedKernel.Pagination;

namespace Modules.Product.Application.Queries;

public sealed record SearchBrandQuery(SearchBrandRequestDto Filter) : IRequest<PageResult<BrandListItemDto>>;

public sealed class SearchBrandHandler : IRequestHandler<SearchBrandQuery, PageResult<BrandListItemDto>>
{
    private readonly IBrandRepository _brandRepository;

    public SearchBrandHandler(IBrandRepository brandRepository)
    {
        _brandRepository = brandRepository;
    }

    public async Task<PageResult<BrandListItemDto>> Handle(SearchBrandQuery request, CancellationToken cancellationToken)
    {
        var query = (await _brandRepository.GetAllWithProductsAsync(cancellationToken))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Filter.Keyword))
        {
            var keyword = request.Filter.Keyword.ToUpperInvariant();
            query = query.Where(b => b.Name.ToUpper().Contains(keyword) || b.Slug.ToUpper().Contains(keyword));
        }

        var totalCount = query.Count();
        var skip = (request.Filter.Page - 1) * request.Filter.Limit;
        var brands = query
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Take(request.Filter.Limit)
            .ToList();

        var dtos = brands.Select(b => new BrandListItemDto
        {
            Id = b.Id,
            Name = b.Name,
            Slug = b.Slug,
            CreatedAt = b.CreatedAt
        }).ToList();

        return Pagination.PaginationResult(dtos, totalCount, request.Filter);
    }
}
