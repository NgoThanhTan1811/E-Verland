
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Queries.BankAccount
{
    public sealed record GetManyBankAccountsByProfileIdQuery(Guid ProfileId) : IRequest<IReadOnlyList<BankAccountResDto>>;

    public sealed class GetManyBankAccountsByProfileIdHandler(IBankAccountRepository repo) : IRequestHandler<GetManyBankAccountsByProfileIdQuery, IReadOnlyList<BankAccountResDto>>
    {
        private readonly IBankAccountRepository _repo = repo;

        public async Task<IReadOnlyList<BankAccountResDto>> Handle(GetManyBankAccountsByProfileIdQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByProfileIdAsync(request.ProfileId, ct)
                ?? throw new KeyNotFoundException("BankAccount not found.");

            return entity.Select(x => x.ToResDto()).ToList();
        }
    }

}