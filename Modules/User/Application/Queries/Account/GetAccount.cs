using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Queries;

public sealed record GetAccountQuery(Guid Id) : IRequest<AccountResDto>;

public sealed class GetAccountHandler(IAccountRepository repo) : IRequestHandler<GetAccountQuery, AccountResDto>
{
    private readonly IAccountRepository _repo = repo;

    public async Task<AccountResDto> Handle(GetAccountQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Account not found.");

        return entity.ToResDto();
    }
}
