
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.BankAccount
{
    public sealed record GetBankAccountByQuery(Guid Id) : IRequest<BankAccountResDto>;

    public sealed class GetBankAccountHandler : IRequestHandler<GetBankAccountByQuery, BankAccountResDto>
    {
        private readonly IBankAccountRepository _repo;
        private readonly IMapper _mapper;

        public GetBankAccountHandler(IBankAccountRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<BankAccountResDto> Handle(GetBankAccountByQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByIdAsync(request.Id, ct)
                ?? throw new KeyNotFoundException("BankAccount not found.");

            return _mapper.Map<BankAccountResDto>(entity);
        }
    }

}