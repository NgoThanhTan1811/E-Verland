using Modules.User.Domain.Enums;

namespace Modules.User.Application.DTOs.Request;

public record CreateAddressReqDto
{
    public LableAddress Label { get; set; } = LableAddress.Home;
    public string? Detail { get; set; }
    public required string Street { get; set; }
    public required int ProvinceId { get; set; }
    public required int DistrictId { get; set; }
    public required int WardId { get; set; }

}

public record UpdateAddressReqDto
{
    public LableAddress? Label { get; set; }
    public string? Detail { get; set; }
    public string? Street { get; set; }
    public bool? IsDefault { get; set; }
    public int? ProvinceId { get; set; }
    public int? DistrictId { get; set; }
    public int? WardId { get; set; }
}
