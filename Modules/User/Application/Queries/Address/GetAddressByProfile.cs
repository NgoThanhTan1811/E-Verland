
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Queries.Address
{
    public sealed record GetAddressByProfileQuery(Guid ProfileId) : IRequest<IReadOnlyList<AddressResDto>>;

    public sealed class GetAddressByProfileHandler(IAddressRepository repo) : IRequestHandler<GetAddressByProfileQuery, IReadOnlyList<AddressResDto>>
    {
        private readonly IAddressRepository _repo = repo;

        public async Task<IReadOnlyList<AddressResDto>> Handle(GetAddressByProfileQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByProfileIdAsync(request.ProfileId, ct);
            return entity.Select(x => x.ToResDto()).ToList();
        }
    }

}