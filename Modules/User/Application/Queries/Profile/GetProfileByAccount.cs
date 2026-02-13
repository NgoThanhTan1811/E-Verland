
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.Profile
{
    public sealed record GetProfileByAccountQuery(Guid AccountId) : IRequest<ProfileResDto>;

    public sealed class GetProfileByAccountHandler : IRequestHandler<GetProfileByAccountQuery, ProfileResDto>
    {
        private readonly IProfileRepository _repo;
        private readonly IMapper _mapper;

        public GetProfileByAccountHandler(IProfileRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<ProfileResDto> Handle(GetProfileByAccountQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByAccountIdAsync(request.AccountId, ct)
                ?? throw new KeyNotFoundException("Profile not found.");

            return _mapper.Map<ProfileResDto>(entity);
        }
    }

}