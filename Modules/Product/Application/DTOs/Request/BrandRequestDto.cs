using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedKernel.Pagination;

namespace Modules.Product.Application.DTOs.Request
{
    public record CreateBrandRequestDto
    {
        public string Name { get; set; } = default!;
    }

    public record UpdateBrandRequestDto
    {
        public string Name { get; set; } = default!;
    }

    public record SearchBrandRequestDto : IPagingFilter
    {
        public string? Keyword { get; set; } = null!;
        public int Page { get; set; }
        public int Limit { get; set; }
    }
}