using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Request;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries;

public sealed record GetAccountsQuery(AccountFilter Filter) : IRequest<IEnumerable<AccountResDto>>;

public sealed class GetAccountsHandler : IRequestHandler<GetAccountsQuery, IEnumerable<AccountResDto>>
{
    private readonly IAccountRepository _repo;
    private readonly IMapper _mapper;

    public GetAccountsHandler(IAccountRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<IEnumerable<AccountResDto>> Handle(GetAccountsQuery request, CancellationToken ct)
    {
        var entity = await _repo.SearchAsync(request.Filter, ct)
            ?? throw new KeyNotFoundException("Account not found.");

        return _mapper.Map<IEnumerable<AccountResDto>>(entity);
    }
}
