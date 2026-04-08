namespace Modules.Product.Application.Contracts
{
    public interface IProductSyncPublisher
    {
        Task PublishAsync(Domain.Product product, string eventType, CancellationToken ct = default);
    }
}
