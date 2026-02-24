using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Request;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using SharedKernel.Pagination;

namespace Modules.User.Application.Queries;

public sealed record GetAccountsQuery(AccountFilter Filter) : IRequest<PageResult<AccountResDto>>;

public sealed class GetAccountsHandler(IAccountRepository repo, IMapper mapper) : IRequestHandler<GetAccountsQuery, PageResult<AccountResDto>>
{
    private readonly IAccountRepository _repo = repo;
    private readonly IMapper _mapper = mapper;

    public async Task<PageResult<AccountResDto>> Handle(GetAccountsQuery request, CancellationToken ct)
    {
        var result = await _repo.SearchAsync(request.Filter, ct)
            ?? throw new KeyNotFoundException("Account not found.");

        return new PageResult<AccountResDto>
        {
            Items = _mapper.Map<IReadOnlyCollection<AccountResDto>>(result.Items),
            TotalItems = result.TotalItems,
            Page = result.Page,
            Limit = result.Limit
        };
    }
}
