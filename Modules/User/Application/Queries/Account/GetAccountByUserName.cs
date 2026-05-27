
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Queries.Account
{
    public sealed record GetAccountByUserNameQuery(string UserName) : IRequest<IEnumerable<AccountResDto>>;

    public sealed class GetAccountByUserNameHandler(IAccountRepository repo) : IRequestHandler<GetAccountByUserNameQuery, IEnumerable<AccountResDto>>
    {
        private readonly IAccountRepository _repo = repo;

        public async Task<IEnumerable<AccountResDto>> Handle(GetAccountByUserNameQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByUsernameAsync(request.UserName, ct)
                ?? throw new KeyNotFoundException("Account not found.");

            return [entity.ToResDto()];
        }
    }

}