using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Entities;

namespace Modules.Cart.Domain
{
    public class Cart : BaseEntity
    {
        public Guid UserId { get; set; }
        public int TotalItems { get; set; }
        public List<CartItem> Items { get; set; } = [];
        
    }
}