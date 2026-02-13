using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries;

public sealed record GetAccountQuery(Guid Id) : IRequest<AccountResDto>;

public sealed class GetAccountHandler : IRequestHandler<GetAccountQuery, AccountResDto>
{
    private readonly IAccountRepository _repo;
    private readonly IMapper _mapper;

    public GetAccountHandler(IAccountRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<AccountResDto> Handle(GetAccountQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Account not found.");

        return _mapper.Map<AccountResDto>(entity);
    }
}
