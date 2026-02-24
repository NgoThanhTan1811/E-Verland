
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.Profile
{
    public sealed record GetProfileByQuery(Guid Id) : IRequest<ProfileResDto>;

    public sealed class GetProfileHandler(IProfileRepository repo, IMapper mapper) : IRequestHandler<GetProfileByQuery, ProfileResDto>
    {
        private readonly IProfileRepository _repo = repo;
        private readonly IMapper _mapper = mapper;

        public async Task<ProfileResDto> Handle(GetProfileByQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByIdAsync(request.Id, ct)
                ?? throw new KeyNotFoundException("Profile not found.");

            return _mapper.Map<ProfileResDto>(entity);
        }
    }

}