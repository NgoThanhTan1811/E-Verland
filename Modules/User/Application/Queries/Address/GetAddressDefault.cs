
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.Address
{
    public sealed record GetAddressDefault(Guid AccountId) : IRequest<AddressResDto>;

    public sealed class GetAddressDefaultHandler : IRequestHandler<GetAddressDefault, AddressResDto>
    {
        private readonly IAddressRepository _repo;
        private readonly IMapper _mapper;

        public GetAddressDefaultHandler(IAddressRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<AddressResDto> Handle(GetAddressDefault request, CancellationToken ct)
        {
            var entity = await _repo.GetDefaultAsync(request.AccountId, ct)
                ?? throw new KeyNotFoundException("Address not found.");

            return _mapper.Map<AddressResDto>(entity);
        }
    }

}