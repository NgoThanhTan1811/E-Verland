
using Modules.User.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Modules.User.Application.Commands
{
    public sealed record DeleteAddressCommand(Guid AddressId) : IRequest<bool>;

    public sealed class DeleteAddressHandler(IAddressRepository repo, IUserDbContext db) : IRequestHandler<DeleteAddressCommand, bool>
    {
        private readonly IAddressRepository _repo = repo;
        private readonly IUserDbContext _db = db;

        public async Task<bool> Handle(DeleteAddressCommand request, CancellationToken ct)
        {
            var deleted = await _repo.DeleteAsync(request.AddressId, ct);

            if (!deleted)
                throw new KeyNotFoundException("Address not found.");

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