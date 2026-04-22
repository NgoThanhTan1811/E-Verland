
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Queries.Address
{
    public sealed record GetAddressByIdForProfileQuery(Guid AddressId, Guid ProfileId) : IRequest<AddressResDto>;

    public sealed class GetAddressByIdForProfileHandler(IAddressRepository repo) : IRequestHandler<GetAddressByIdForProfileQuery, AddressResDto>
    {
        private readonly IAddressRepository _repo = repo;

        public async Task<AddressResDto> Handle(GetAddressByIdForProfileQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByIdForProfileAsync(request.AddressId, request.ProfileId, ct)
                ?? throw new KeyNotFoundException("Address not found.");

            return entity.ToResDto();
        }
    }

}