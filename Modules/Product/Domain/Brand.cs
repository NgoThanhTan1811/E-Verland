using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Entities;

namespace Modules.Product.Domain
{
    public class Brand : BaseEntity
    {
        public string Name { get; set; } = default!;
        public List<Product> Products { get; set; } = [];
        public string Slug { get; set; } = default!;
    }
}