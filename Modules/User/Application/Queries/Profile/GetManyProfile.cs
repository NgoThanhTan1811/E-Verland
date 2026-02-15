
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using SharedKernel.Pagination;

namespace Modules.User.Application.Queries.Profile
{
    public sealed record GetManyProfileByQuery(PagingFilter Filter) : IRequest<PageResult<ProfileResDto>>;

    public sealed class GetManyProfileHandler : IRequestHandler<GetManyProfileByQuery, PageResult<ProfileResDto>>
    {
        private readonly IProfileRepository _repo;
        private readonly IMapper _mapper;

        public GetManyProfileHandler(IProfileRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<PageResult<ProfileResDto>> Handle(GetManyProfileByQuery request, CancellationToken ct)
        {
            var result = await _repo.GetPagedAsync(request.Filter, ct)
                ?? throw new KeyNotFoundException("Profile not found.");

            return new PageResult<ProfileResDto>
            {
                Items = _mapper.Map<IReadOnlyCollection<ProfileResDto>>(result.Items),
                TotalItems = result.TotalItems,
                Page = result.Page,
                Limit = result.Limit
            };
        }
    }

}