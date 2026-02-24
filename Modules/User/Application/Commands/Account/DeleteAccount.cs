
using Modules.User.Application.Interfaces.Repositories;
using MediatR;

namespace Modules.User.Application.Commands
{
    public sealed record DeleteAccountCommand(Guid AccountId) : IRequest<bool>;

    public sealed class DeleteAccountHandler(IAccountRepository repo) : IRequestHandler<DeleteAccountCommand, bool>
    {
        private readonly IAccountRepository _repo = repo;

        public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken ct)
        {
            var deleted = await _repo.DeleteAsync(request.AccountId, ct);

            if (!deleted)
                throw new KeyNotFoundException("Bank account not found.");

            return true;
        }
    }

}