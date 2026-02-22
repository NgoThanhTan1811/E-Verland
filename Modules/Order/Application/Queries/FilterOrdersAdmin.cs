using AutoMapper;
using MediatR;
using Modules.Order.Application.Contracts;
using Modules.Order.Application.DTOs.Request;
using Modules.Order.Application.DTOs.Response;
using Modules.Order.Domain;
using SharedKernel.Pagination;

namespace Modules.Order.Application.Queries;

public sealed record FilterOrdersAdminQuery(
    FilterOrdersAdminRequestDto Filter
) : IRequest<PageResult<OrderOverviewResponseDto>>;

public sealed class FilterOrdersAdminHandler : IRequestHandler<FilterOrdersAdminQuery, PageResult<OrderOverviewResponseDto>>
{
    private readonly IOrderRepository _repo;
    private readonly IMapper _mapper;

    public FilterOrdersAdminHandler(IOrderRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<PageResult<OrderOverviewResponseDto>> Handle(FilterOrdersAdminQuery request, CancellationToken ct)
    {
        var result = await _repo.GetFilteredOrdersAsync(
            request.Filter.UserId,
            request.Filter.Status,
            request.Filter.PaymentStatus,
            request.Filter.PaymentMethod,
            request.Filter.FromDate,
            request.Filter.ToDate,
            new PagingFilter
            {
                Page = request.Filter.Page ?? 1,
                Limit = request.Filter.Limit ?? 20
            },
            ct
        );

        return new PageResult<OrderOverviewResponseDto>
        {
            Items = result.Items.Select(_mapper.Map<OrderOverviewResponseDto>).ToList(),
            TotalItems = result.TotalItems,
            Page = result.Page,
            Limit = result.Limit
        };
    }
}
