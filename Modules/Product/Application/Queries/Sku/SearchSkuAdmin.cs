using MediatR;
using Modules.Product.Application.Abtracsts;
using Modules.Product.Application.DTOs.Request;
using Modules.Product.Application.DTOs.Response;
using SharedKernel.Pagination;

namespace Modules.Product.Application.Queries;

public sealed record SearchSkuAdminQuery(SearchSkuAdminRequestDto Filter) : IRequest<PaginationResult<SkuAdminListItemDto>>;

public sealed class SearchSkuAdminHandler : IRequestHandler<SearchSkuAdminQuery, PaginationResult<SkuAdminListItemDto>>
{
    private readonly ISkuRepository _skuRepository;

    public SearchSkuAdminHandler(ISkuRepository skuRepository)
    {
        _skuRepository = skuRepository;
    }

    public async Task<PaginationResult<SkuAdminListItemDto>> Handle(SearchSkuAdminQuery request, CancellationToken cancellationToken)
    {
        var skus = await _skuRepository.GetAllWithProductAsync(cancellationToken);
        var query = skus.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.Filter.Keyword))
        {
            var keyword = request.Filter.Keyword.ToUpperInvariant();
            query = query.Where(s => s.SkuCode.ToUpper().Contains(keyword));
        }

        if (request.Filter.ProductId.HasValue)
        {
            query = query.Where(s => s.ProductId == request.Filter.ProductId);
        }

        if (request.Filter.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == request.Filter.IsActive);
        }

        if (request.Filter.MinStock.HasValue)
        {
            query = query.Where(s => s.Stock >= request.Filter.MinStock);
        }

        if (request.Filter.MaxStock.HasValue)
        {
            query = query.Where(s => s.Stock <= request.Filter.MaxStock);
        }

        if (request.Filter.MinPrice.HasValue)
        {
            query = query.Where(s => s.Price >= request.Filter.MinPrice);
        }

        if (request.Filter.MaxPrice.HasValue)
        {
            query = query.Where(s => s.Price <= request.Filter.MaxPrice);
        }

        var totalCount = query.Count();
        var skip = (request.Filter.Page - 1) * request.Filter.Limit;
        var skuList = query
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(request.Filter.Limit)
            .ToList();

        var dtos = skuList.Select(s => new SkuAdminListItemDto
        {
            Id = s.Id,
            SkuCode = s.SkuCode,
            Price = s.Price,
            Stock = s.Stock,
            OptionValues = s.OptionValues,
            ProductName = s.Product?.Name ?? string.Empty
        }).ToList();

        return new PaginationResult<SkuAdminListItemDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = request.Filter.Page,
            Limit = request.Filter.Limit,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.Filter.Limit)
        };
    }
}
