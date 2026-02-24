
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.Account
{
    public sealed record GetAccountByEmailQuery(string Email) : IRequest<IEnumerable<AccountResDto>>;

    public sealed class GetAccountByEmailHandler(IAccountRepository repo, IMapper mapper) : IRequestHandler<GetAccountByEmailQuery, IEnumerable<AccountResDto>>
    {
        private readonly IAccountRepository _repo = repo;
        private readonly IMapper _mapper = mapper;

        public async Task<IEnumerable<AccountResDto>> Handle(GetAccountByEmailQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByEmailAsync(request.Email, ct)
                ?? throw new KeyNotFoundException("Account not found.");

            return _mapper.Map<IEnumerable<AccountResDto>>(entity);
        }
    }

}