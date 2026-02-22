using AutoMapper;
using MediatR;
using Modules.Order.Application.Contracts;
using Modules.Order.Application.DTOs.Response;
using SharedKernel.Pagination;

namespace Modules.Order.Application.Queries;

public sealed record GetUserOrdersQuery(
    Guid UserId,
    PagingFilter Filter
) : IRequest<PageResult<OrderOverviewResponseDto>>;

public sealed class GetUserOrdersHandler : IRequestHandler<GetUserOrdersQuery, PageResult<OrderOverviewResponseDto>>
{
    private readonly IOrderRepository _repo;
    private readonly IMapper _mapper;

    public GetUserOrdersHandler(IOrderRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<PageResult<OrderOverviewResponseDto>> Handle(GetUserOrdersQuery request, CancellationToken ct)
    {
        var result = await _repo.GetUserOrdersAsync(request.UserId, request.Filter, ct);

        return new PageResult<OrderOverviewResponseDto>
        {
            Items = result.Items.Select(_mapper.Map<OrderOverviewResponseDto>).ToList(),
            TotalItems = result.TotalItems,
            Page = result.Page,
            Limit = result.Limit
        };
    }
}
