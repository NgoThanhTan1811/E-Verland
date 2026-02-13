
using Modules.User.Application.Interfaces.Repositories;
using MediatR;

namespace Modules.User.Application.Commands
{
    public sealed record DeleteBankAccountCommand(Guid BankAccountId, Guid ProfileId) : IRequest<bool>;

    public sealed class DeleteBankAccountHandler : IRequestHandler<DeleteBankAccountCommand, bool>
    {
        private readonly IBankAccountRepository _repo;

        public DeleteBankAccountHandler(IBankAccountRepository repo)
        {
            _repo = repo;
        }

        public async Task<bool> Handle(DeleteBankAccountCommand request, CancellationToken ct)
        {
            var deleted = await _repo.DeleteBankAccountAsync(request.BankAccountId, request.ProfileId, ct);

            if (!deleted)
                throw new KeyNotFoundException("Bank account not found.");

            return true;
        }
    }

}