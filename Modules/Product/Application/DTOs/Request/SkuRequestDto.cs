using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Pagination;

namespace Modules.Product.Application.DTOs.Request
{
    public record CreateSkuRequestDto
    {
        public string SkuCode { get; set; } = default!;
        public Guid ProductId { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Url { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public Dictionary<string, string> OptionValues { get; set; } = new();
    }

    public record UpdateSkuRequestDto
    {
        public string SkuCode { get; set; } = default!;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Url { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public Dictionary<string, string> OptionValues { get; set; } = new();
    }

    public record AddSkusToProductRequestDto
    {
        public List<ProductVariantDto> Variants { get; set; } = [];
        public int Stock { get; set; }
    }

    public record SearchSkuAdminRequestDto : IPagingFilter
    {
        public string? Keyword { get; set; }    // SkuCode
        public Guid? ProductId { get; set; }
        public bool? IsActive { get; set; }

        public int? MinStock { get; set; }
        public int? MaxStock { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 20;
    }

}