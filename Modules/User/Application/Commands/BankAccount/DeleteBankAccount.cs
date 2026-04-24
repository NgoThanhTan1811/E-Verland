
using Modules.User.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Modules.User.Application.Commands
{
    public sealed record DeleteBankAccountCommand(Guid BankAccountId, Guid ProfileId) : IRequest<bool>;

    public sealed class DeleteBankAccountHandler(IBankAccountRepository repo, IUserDbContext db) : IRequestHandler<DeleteBankAccountCommand, bool>
    {
        private readonly IBankAccountRepository _repo = repo;
        private readonly IUserDbContext _db = db;

        public async Task<bool> Handle(DeleteBankAccountCommand request, CancellationToken ct)
        {
            var deleted = await _repo.DeleteBankAccountAsync(request.BankAccountId, request.ProfileId, ct);

            if (!deleted)
                throw new KeyNotFoundException("Bank account not found.");

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