
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.Address
{
    public sealed record GetManyAddressByQuery() : IRequest<IReadOnlyList<AddressResDto>>;

    public sealed class GetManyAddressHandler : IRequestHandler<GetManyAddressByQuery, IReadOnlyList<AddressResDto>>
    {
        private readonly IAddressRepository _repo;
        private readonly IMapper _mapper;

        public GetManyAddressHandler(IAddressRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<AddressResDto>> Handle(GetManyAddressByQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetAllAsync(ct)
                ?? throw new KeyNotFoundException("Address not found.");

            return _mapper.Map<IReadOnlyList<AddressResDto>>(entity);
        }
    }

}