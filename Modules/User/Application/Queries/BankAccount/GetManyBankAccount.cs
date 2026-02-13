
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.BankAccount
{
    public sealed record GetManyBankAccountByQuery() : IRequest<IReadOnlyList<BankAccountResDto>>;

    public sealed class GetManyBankAccountHandler : IRequestHandler<GetManyBankAccountByQuery, IReadOnlyList<BankAccountResDto>>
    {
        private readonly IBankAccountRepository _repo;
        private readonly IMapper _mapper;

        public GetManyBankAccountHandler(IBankAccountRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<BankAccountResDto>> Handle(GetManyBankAccountByQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetAllAsync(ct)
                ?? throw new KeyNotFoundException("BankAccount not found.");

            return _mapper.Map<IReadOnlyList<BankAccountResDto>>(entity);
        }
    }

}