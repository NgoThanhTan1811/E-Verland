using SharedKernel.Entities;

namespace Modules.Product.Domain
{
    public class StockReservation : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Guid PaymentId { get; set; }
        public Guid SkuId { get; set; }
        public int Quantity { get; set; }
        public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public ReservationStatus Status { get; set; }
    }

    public enum ReservationStatus
    {
        Reserved,
        Confirmed,
        Released,
        Expired
    }
}
