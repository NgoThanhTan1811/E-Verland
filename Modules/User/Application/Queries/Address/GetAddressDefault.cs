
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Queries.Address
{
    public sealed record GetAddressDefault(Guid AccountId) : IRequest<AddressResDto>;

    public sealed class GetAddressDefaultHandler(IAddressRepository repo) : IRequestHandler<GetAddressDefault, AddressResDto>
    {
        private readonly IAddressRepository _repo = repo;

        public async Task<AddressResDto> Handle(GetAddressDefault request, CancellationToken ct)
        {
            var entity = await _repo.GetDefaultAsync(request.AccountId, ct)
                ?? throw new KeyNotFoundException("Address not found.");

            return entity.ToResDto();
        }
    }

}