
using MediatR;
using Modules.User.Application.DTOs.Response;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Application.Mappings;
using SharedKernel.Pagination;

namespace Modules.User.Application.Queries.Address
{
    public sealed record GetManyAddressByQuery(PagingFilter Filter) : IRequest<PageResult<AddressResDto>>;

    public sealed class GetManyAddressHandler(IAddressRepository repo) : IRequestHandler<GetManyAddressByQuery, PageResult<AddressResDto>>
    {
        private readonly IAddressRepository _repo = repo;

        public async Task<PageResult<AddressResDto>> Handle(GetManyAddressByQuery request, CancellationToken ct)
        {
            var result = await _repo.GetPagedAsync(request.Filter, ct)
                ?? throw new KeyNotFoundException("Address not found.");

            return new PageResult<AddressResDto>
            {
                Items = result.Items.Select(x => x.ToResDto()).ToList(),
                TotalItems = result.TotalItems,
                Page = result.Page,
                Limit = result.Limit
            };
        }
    }

}