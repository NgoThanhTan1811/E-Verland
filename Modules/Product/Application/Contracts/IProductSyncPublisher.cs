namespace Modules.Product.Application.Contracts
{
    public interface IProductSyncPublisher
    {
        Task PublishAsync(Domain.Product product, string eventType, CancellationToken ct = default);
        Task PublishModerationAsync(Domain.Product product, string action, Guid adminId, string reason, CancellationToken ct = default);
    }
}
