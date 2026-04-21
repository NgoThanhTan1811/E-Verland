using Microsoft.EntityFrameworkCore;
using Modules.Order.Application.Contracts;
using Modules.Order.Domain;
using Modules.Order.Infrastructure.Persistence;

namespace Modules.Order.Infrastructure.Services;

public class OrderPaymentSyncService(OrderDbContext dbContext) : IOrderPaymentSyncService
{
    private readonly OrderDbContext _dbContext = dbContext;

    public async Task SyncPaymentAsync(Guid orderId, Guid paymentId, string paymentStatus, CancellationToken ct = default)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order is null)
        {
            return;
        }

        order.PaymentId = paymentId;
        order.PaymentStatus = paymentStatus.ToLowerInvariant() switch
        {
            "success" => Domain.PaymentStatus.Success,
            "failed" => Domain.PaymentStatus.Failed,
            "refunded" => Domain.PaymentStatus.Refunded,
            _ => Domain.PaymentStatus.Pending
        };

        await _dbContext.SaveChangesAsync(ct);
    }
}
