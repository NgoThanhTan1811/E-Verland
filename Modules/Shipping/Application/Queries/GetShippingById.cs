using AutoMapper;
using MediatR;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Application.DTOs.Response;

namespace Modules.Shipping.Application.Queries;

public sealed record GetShippingByIdQuery(Guid ShippingId) : IRequest<ShippingOrderResponseDto>;

public sealed class GetShippingByIdHandler(IShippingRepository repo, IMapper mapper)
    : IRequestHandler<GetShippingByIdQuery, ShippingOrderResponseDto>
{
    private readonly IShippingRepository _repo = repo;
    private readonly IMapper _mapper = mapper;

    public async Task<ShippingOrderResponseDto> Handle(GetShippingByIdQuery request, CancellationToken ct)
    {
        var shipping = await _repo.GetByIdAsync(request.ShippingId, ct)
            ?? throw new KeyNotFoundException("Shipping order not found");

        return _mapper.Map<ShippingOrderResponseDto>(shipping);
    }
}
