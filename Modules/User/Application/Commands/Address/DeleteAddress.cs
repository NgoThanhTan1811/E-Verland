
using Modules.User.Application.Interfaces.Repositories;
using MediatR;

namespace Modules.User.Application.Commands
{
    public sealed record DeleteAddressCommand(Guid AddressId) : IRequest<bool>;

    public sealed class DeleteAddressHandler : IRequestHandler<DeleteAddressCommand, bool>
    {
        private readonly IAddressRepository _repo;

        public DeleteAddressHandler(IAddressRepository repo)
        {
            _repo = repo;
        }

        public async Task<bool> Handle(DeleteAddressCommand request, CancellationToken ct)
        {
            var deleted = await _repo.DeleteAsync(request.AddressId, ct);

            if (!deleted)
                throw new KeyNotFoundException("Bank account not found.");

            return true;
        }
    }

}