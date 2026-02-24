
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.Address
{
    public sealed record GetAddressByIdForProfileQuery(Guid AddressId, Guid ProfileId) : IRequest<AddressResDto>;

    public sealed class GetAddressByIdForProfileHandler(IAddressRepository repo, IMapper mapper) : IRequestHandler<GetAddressByIdForProfileQuery, AddressResDto>
    {
        private readonly IAddressRepository _repo = repo;
        private readonly IMapper _mapper = mapper;

        public async Task<AddressResDto> Handle(GetAddressByIdForProfileQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByIdForProfileAsync(request.AddressId, request.ProfileId, ct);
            return _mapper.Map<AddressResDto>(entity);
        }
    }

}