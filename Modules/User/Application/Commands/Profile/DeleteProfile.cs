
using Modules.User.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Modules.User.Application.Commands
{
    public sealed record DeleteProfileCommand(Guid ProfileId) : IRequest<bool>;

    public sealed class DeleteProfileHandler(IProfileRepository repo, IUserDbContext db) : IRequestHandler<DeleteProfileCommand, bool>
    {
        private readonly IProfileRepository _repo = repo;
        private readonly IUserDbContext _db = db;

        public async Task<bool> Handle(DeleteProfileCommand request, CancellationToken ct)
        {
            var deleted = await _repo.DeleteAsync(request.ProfileId, ct);

            if (!deleted)
                throw new KeyNotFoundException("Profile not found.");

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