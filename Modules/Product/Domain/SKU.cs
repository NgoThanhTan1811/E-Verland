using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Entities;

namespace Modules.Product.Domain
{
    public class SKU : BaseEntity
    {
        public string Value { get; set; } = default!;
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = default!;
        public decimal Price { get; set; } = default!;
        public int Stock { get; set; } = default!;
        public string Url { get; set; } = default!;

    }
}