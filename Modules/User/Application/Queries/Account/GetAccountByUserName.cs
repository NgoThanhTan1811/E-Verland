
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.Account
{
    public sealed record GetAccountByUserNameQuery(string UserName) : IRequest<IEnumerable<AccountResDto>>;

    public sealed class GetAccountByUserNameHandler : IRequestHandler<GetAccountByUserNameQuery, IEnumerable<AccountResDto>>
    {
        private readonly IAccountRepository _repo;
        private readonly IMapper _mapper;

        public GetAccountByUserNameHandler(IAccountRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AccountResDto>> Handle(GetAccountByUserNameQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByUsernameAsync(request.UserName, ct)
                ?? throw new KeyNotFoundException("Account not found.");

            return _mapper.Map<IEnumerable<AccountResDto>>(entity);
        }
    }

}