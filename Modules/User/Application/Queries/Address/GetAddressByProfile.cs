
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.Address
{
    public sealed record GetAddressByProfileQuery( Guid ProfileId) : IRequest<IReadOnlyList<AddressResDto>>;

    public sealed class GetAddressByProfileHandler(IAddressRepository repo, IMapper mapper) : IRequestHandler<GetAddressByProfileQuery, IReadOnlyList<AddressResDto>>
    {
        private readonly IAddressRepository _repo = repo;
        private readonly IMapper _mapper = mapper;

        public async Task<IReadOnlyList<AddressResDto>> Handle(GetAddressByProfileQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByProfileIdAsync(request.ProfileId, ct);
            return _mapper.Map<IReadOnlyList<AddressResDto>>(entity);
        }
    }

}