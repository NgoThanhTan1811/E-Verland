
using AutoMapper;
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using SharedKernel.Pagination;

namespace Modules.User.Application.Queries.Address
{
    public sealed record GetManyAddressByQuery(PagingFilter Filter) : IRequest<PageResult<AddressResDto>>;

    public sealed class GetManyAddressHandler : IRequestHandler<GetManyAddressByQuery, PageResult<AddressResDto>>
    {
        private readonly IAddressRepository _repo;
        private readonly IMapper _mapper;

        public GetManyAddressHandler(IAddressRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<PageResult<AddressResDto>> Handle(GetManyAddressByQuery request, CancellationToken ct)
        {
            var result = await _repo.GetPagedAsync(request.Filter, ct)
                ?? throw new KeyNotFoundException("Address not found.");

            return new PageResult<AddressResDto>
            {
                Items = _mapper.Map<IReadOnlyCollection<AddressResDto>>(result.Items),
                TotalItems = result.TotalItems,
                Page = result.Page,
                Limit = result.Limit
            };
        }
    }

}