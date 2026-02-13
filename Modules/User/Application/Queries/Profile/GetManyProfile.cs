
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;

namespace Modules.User.Application.Queries.Profile
{
    public sealed record GetManyProfileByQuery() : IRequest<IReadOnlyList<ProfileResDto>>;

    public sealed class GetManyProfileHandler : IRequestHandler<GetManyProfileByQuery, IReadOnlyList<ProfileResDto>>
    {
        private readonly IProfileRepository _repo;
        private readonly IMapper _mapper;

        public GetManyProfileHandler(IProfileRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ProfileResDto>> Handle(GetManyProfileByQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetAllAsync(ct)
                ?? throw new KeyNotFoundException("Profile not found.");

            return _mapper.Map<IReadOnlyList<ProfileResDto>>(entity);
        }
    }

}