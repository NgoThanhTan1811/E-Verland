
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.BankAccount
{
    public sealed record GetBankAccountByProfileIdQuery(Guid BankAccountId, Guid ProfileId) : IRequest<IReadOnlyList<BankAccountResDto>>;

    public sealed class GetBankAccountByProfileIdHandler(IBankAccountRepository repo, IMapper mapper) : IRequestHandler<GetBankAccountByProfileIdQuery, IReadOnlyList<BankAccountResDto>>
    {
        private readonly IBankAccountRepository _repo = repo;
        private readonly IMapper _mapper = mapper;

        public async Task<IReadOnlyList<BankAccountResDto>> Handle(GetBankAccountByProfileIdQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByIdForProfileAsync(request.BankAccountId, request.ProfileId, ct)
                ?? throw new KeyNotFoundException("BankAccount not found.");

            return _mapper.Map<IReadOnlyList<BankAccountResDto>>(entity);
        }
    }

}