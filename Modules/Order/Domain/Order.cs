using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Entities;

namespace Modules.Order.Domain
{
    public class Order : BaseEntity
    {
        public string Code { get; set; } = default!;
        public Guid UserId { get; set; }
        public Guid ShopId { get; set; }
        
        public decimal TotalPrice { get; set; }
        public decimal GrandTotal => TotalPrice - (Discount ?? 0);
        public decimal? Discount { get; set; }

        public List<OrderItem> Items { get; set; } = [];

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public ReceiverSnapshot Receiver { get; set; } = default!;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
        public Guid? PaymentId { get; set; }
    }
}