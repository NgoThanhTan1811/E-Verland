using AutoMapper;
using MediatR;
using Modules.Order.Application.Contracts;
using Modules.Order.Application.DTOs.Response;

namespace Modules.Order.Application.Queries;

public sealed record GetOrderByIdQuery(
    Guid OrderId,
    Guid UserId
) : IRequest<OrderDetailResponseDto>;

public sealed class GetOrderByIdHandler(IOrderRepository repo, IMapper mapper) : IRequestHandler<GetOrderByIdQuery, OrderDetailResponseDto>
{
    private readonly IOrderRepository _repo = repo;
    private readonly IMapper _mapper = mapper;

    public async Task<OrderDetailResponseDto> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(request.OrderId, ct)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.UserId != request.UserId && order.ShopId != request.UserId)
            throw new UnauthorizedAccessException("You can only view your own orders");

        return _mapper.Map<OrderDetailResponseDto>(order);
    }
}
