using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.Product.Application.DTOs.Response
{
    public record SkuAdminListItemDto
    {
        public Guid Id { get; set; }
        public string SkuCode { get; set; } = default!;
        public decimal Price { get; set; }
        public int Stock { get; set; }

        public Dictionary<string, string> OptionValues { get; set; } = new();
        public string ProductName { get; set; } = default!;
    }

    public record SkuDetailDto
    {
        public Guid Id { get; set; }
        public string SkuCode { get; set; } = default!;
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Url { get; set; } = default!;
        public bool IsActive { get; set; }
        public Dictionary<string, string> OptionValues { get; set; } = new();
    }

}