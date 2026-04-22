
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.Mappings;

namespace Modules.User.Application.Queries.Profile
{
    public sealed record GetProfileByAccountQuery(Guid AccountId) : IRequest<ProfileResDto>;

    public sealed class GetProfileByAccountHandler(IProfileRepository repo) : IRequestHandler<GetProfileByAccountQuery, ProfileResDto>
    {
        private readonly IProfileRepository _repo = repo;

        public async Task<ProfileResDto> Handle(GetProfileByAccountQuery request, CancellationToken ct)
        {
            var entity = await _repo.GetByAccountIdAsync(request.AccountId, ct)
                ?? throw new KeyNotFoundException("Profile not found.");

            return entity.ToResDto();
        }
    }

}