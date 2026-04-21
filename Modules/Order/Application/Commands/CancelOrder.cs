using Infra.AWS.CloudWatch;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Order.Application.Contracts;
using Modules.Order.Domain;
using Modules.Product.Application.Contracts;

namespace Modules.Order.Application.Commands;

public sealed record CancelOrderCommand(
    Guid OrderId,
    Guid UserId
) : IRequest<Unit>;

public sealed class CancelOrderHandler(
    IOrderRepository repo,
    IOrderDbContext db,
    ICloudWatchService cloudWatch,
    IProductReservationService productReservationService)
    : IRequestHandler<CancelOrderCommand, Unit>
{
    private readonly IOrderRepository _repo = repo;
    private readonly IOrderDbContext _db = db;

    public async Task<Unit> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(request.OrderId, ct)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.UserId != request.UserId)
            throw new UnauthorizedAccessException("You can only cancel your own orders");

        if (order.Status == OrderStatus.Canceled)
            throw new InvalidOperationException("Order is already canceled");

        if (order.Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed order");

        if (order.Status == OrderStatus.Shipping)
            throw new InvalidOperationException("Cannot cancel an order that is being shipped");

        order.Status = OrderStatus.Canceled;

        await _repo.UpdateAsync(order, ct);

        if (order.PaymentId.HasValue)
        {
            await productReservationService.ReleaseReservationAsync(order.PaymentId.Value, ct);
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Order cancellation failed due to database error.");
        }

        await cloudWatch.PutMetricAsync("order.cancelled", 1, "Count");

        return Unit.Value;
    }
}
