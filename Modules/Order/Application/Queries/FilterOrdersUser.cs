using AutoMapper;
using MediatR;
using Modules.Order.Application.Contracts;
using Modules.Order.Application.DTOs.Request;
using Modules.Order.Application.DTOs.Response;
using Modules.Order.Domain;
using SharedKernel.Pagination;

namespace Modules.Order.Application.Queries;

public sealed record FilterOrdersUserQuery(
    Guid UserId,
    FilterOrdersUserRequestDto Filter
) : IRequest<PageResult<OrderOverviewResponseDto>>;

public sealed class FilterOrdersUserHandler : IRequestHandler<FilterOrdersUserQuery, PageResult<OrderOverviewResponseDto>>
{
    private readonly IOrderRepository _repo;
    private readonly IMapper _mapper;

    public FilterOrdersUserHandler(IOrderRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<PageResult<OrderOverviewResponseDto>> Handle(FilterOrdersUserQuery request, CancellationToken ct)
    {
        var result = await _repo.GetFilteredOrdersAsync(
            request.UserId,
            request.Filter.Status,
            request.Filter.PaymentStatus,
            null,
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
