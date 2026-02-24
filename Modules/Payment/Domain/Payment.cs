
using SharedKernel.Entities;

namespace Modules.Payment.Domain
{
    public class Payment : BaseEntity
    {
        public string Code { get;  set; } = null!;
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } 
        public PaymentStatus Status { get; set; } 

    }
}