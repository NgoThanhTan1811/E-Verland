using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Order.Application.Contracts;
using Modules.Order.Application.DTOs.Response;
using Modules.Order.Domain;

namespace Modules.Order.Application.Commands;

public sealed record UpdateOrderStatusCommand(
    Guid OrderId,
    OrderStatus Status
) : IRequest<OrderOverviewResponseDto>;

public sealed class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, OrderOverviewResponseDto>
{
    private readonly IOrderRepository _repo;
    private readonly IOrderDbContext _db;
    private readonly IMapper _mapper;

    public UpdateOrderStatusHandler(IOrderRepository repo, IOrderDbContext db, IMapper mapper)
    {
        _repo = repo;
        _db = db;
        _mapper = mapper;
    }

    public async Task<OrderOverviewResponseDto> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(request.OrderId, ct)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.Status == OrderStatus.Canceled)
            throw new InvalidOperationException("Cannot update a canceled order");

        if (order.Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot update a completed order");

        order.Status = request.Status;

        await _repo.UpdateAsync(order, ct);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Order status update failed due to database error.");
        }

        return _mapper.Map<OrderOverviewResponseDto>(order);
    }
}
