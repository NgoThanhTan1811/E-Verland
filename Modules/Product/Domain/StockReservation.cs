using SharedKernel.Entities;

namespace Modules.Product.Domain
{
    public class StockReservation : BaseEntity
    {
        public Guid PaymentId { get; set; }
        public Guid SkuId { get; set; }
        public int Quantity { get; set; }
        public ReservationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum ReservationStatus
    {
        Reserved,
        Confirmed,
        Released
    }
}
