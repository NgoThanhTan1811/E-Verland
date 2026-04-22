
using Modules.User.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Modules.User.Application.Commands
{
    public sealed record DeleteAccountCommand(Guid AccountId) : IRequest<bool>;

    public sealed class DeleteAccountHandler(IAccountRepository repo, IUserDbContext db) : IRequestHandler<DeleteAccountCommand, bool>
    {
        private readonly IAccountRepository _repo = repo;
        private readonly IUserDbContext _db = db;

        public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken ct)
        {
            var deleted = await _repo.DeleteAsync(request.AccountId, ct);

            if (!deleted)
                throw new KeyNotFoundException("Account not found.");

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Delete failed due to a database constraint.");
            }

            return true;
        }
    }

}