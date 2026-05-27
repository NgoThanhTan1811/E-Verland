using Microsoft.Extensions.Logging;
using Modules.Product.Application.Contracts;

namespace Modules.Product.Infrastructure.Services;

public sealed class ProductModerationAuditLog(ILogger<ProductModerationAuditLog> logger) : IProductModerationAuditLog
{
    private readonly ILogger<ProductModerationAuditLog> _logger = logger;

    public Task WriteAsync(Guid adminId, string action, Guid productId, string reason, DateTime timestampUtc, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogInformation(
            "ProductModerationAudit admin_id={AdminId} action={Action} product_id={ProductId} reason={Reason} timestamp={TimestampUtc}",
            adminId, action, productId, reason, timestampUtc);
        return Task.CompletedTask;
    }
}
