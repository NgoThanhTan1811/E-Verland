
using Modules.User.Application.Interfaces.Repositories;
using MediatR;

namespace Modules.User.Application.Commands
{
    public sealed record DeleteProfileCommand(Guid ProfileId) : IRequest<bool>;

    public sealed class DeleteProfileHandler(IProfileRepository repo) : IRequestHandler<DeleteProfileCommand, bool>
    {
        private readonly IProfileRepository _repo = repo;

        public async Task<bool> Handle(DeleteProfileCommand request, CancellationToken ct)
        {
            var deleted = await _repo.DeleteAsync(request.ProfileId, ct);

            if (!deleted)
                throw new KeyNotFoundException("Profile not found.");

            return true;
        }
    }

}