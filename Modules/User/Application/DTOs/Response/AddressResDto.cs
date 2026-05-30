using Modules.User.Domain.Enums;

namespace Modules.User.Application.DTOs.Response
{
    public class AddressResDto
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public LableAddress Label { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public int? WardId { get; set; }
        public string Street { get; set; } = default!;
        public string Detail { get; set; } = default!;
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}