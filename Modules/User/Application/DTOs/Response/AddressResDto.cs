using Modules.User.Domain.Enums;

namespace Modules.User.Application.DTOs.Response
{
    public class AddressResDto
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public LableAddress Label { get; set; }
        public string City { get; set; } = default!;
        public string Province { get; set; } = default!;
        public string District { get; set; } = default!;
        public string Ward { get; set; } = default!;
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public string? WardCode { get; set; }
        public string Street { get; set; } = default!;
        public string Detail { get; set; } = default!;
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}