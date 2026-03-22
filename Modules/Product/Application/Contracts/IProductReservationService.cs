namespace Modules.Product.Application.Contracts;

public interface IProductReservationService
{
    Task ReserveStockAsync(
        Guid paymentId,
        IEnumerable<(Guid SkuId, int Quantity)> items,
        CancellationToken ct = default);

    Task ConfirmReservationAsync(Guid paymentId, CancellationToken ct = default);

    Task ReleaseReservationAsync(Guid paymentId, CancellationToken ct = default);
}
