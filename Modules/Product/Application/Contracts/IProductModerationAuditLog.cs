namespace Modules.Product.Application.Contracts;

public interface IProductModerationAuditLog
{
    Task WriteAsync(Guid adminId, string action, Guid productId, string reason, DateTime timestampUtc, CancellationToken ct = default);
}
