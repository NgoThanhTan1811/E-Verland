using AutoMapper;
using MediatR;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Application.DTOs.Response;

namespace Modules.Shipping.Application.Queries;

public sealed record GetShippingByOrderIdQuery(Guid OrderId) : IRequest<ShippingOrderResponseDto>;

public sealed class GetShippingByOrderIdHandler(IShippingRepository repo, IMapper mapper)
    : IRequestHandler<GetShippingByOrderIdQuery, ShippingOrderResponseDto>
{
    private readonly IShippingRepository _repo = repo;
    private readonly IMapper _mapper = mapper;

    public async Task<ShippingOrderResponseDto> Handle(GetShippingByOrderIdQuery request, CancellationToken ct)
    {
        var shipping = await _repo.GetByOrderIdAsync(request.OrderId, ct)
            ?? throw new KeyNotFoundException("Shipping order not found");

        return _mapper.Map<ShippingOrderResponseDto>(shipping);
    }
}
