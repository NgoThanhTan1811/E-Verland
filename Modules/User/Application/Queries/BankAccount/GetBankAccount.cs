
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Queries.BankAccount
{
    public sealed record GetBankAccountByQuery(Guid Id) : IRequest<BankAccountResDto>;

    public sealed class GetBankAccountHandler(IBankAccountRepository repo) : IRequestHandler<GetBankAccountByQuery, BankAccountResDto>
    {
        private readonly IBankAccountRepository _repo = repo;

        public async Task<BankAccountResDto> Handle(GetBankAccountByQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByIdAsync(request.Id, ct)
                ?? throw new KeyNotFoundException("BankAccount not found.");

            return entity.ToResDto();
        }
    }

}