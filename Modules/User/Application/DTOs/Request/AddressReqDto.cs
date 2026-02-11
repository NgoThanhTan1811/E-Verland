using Modules.User.Domain.Entities;
using Modules.User.Domain.Enums;

namespace Modules.User.Application.DTOs.Request;

public class CreateAddressReqDto
{
    public LableAddress Label { get; set; } = LableAddress.Other;
    public string City { get; set; } = default!;
    public string Ward { get; set; } = default!;
    public string Detail { get; set; } = default!;
    public string Street { get; set; } = default!;
    public string District { get; set; } = default!;
    public string Province { get; set; } = default!;

}

public class UpdateAddressReqDto
{
    public LableAddress? Label { get; set; }
    public string? City { get; set; }
    public string? Ward { get; set; }
    public string? Detail { get; set; }
    public string? Street { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public bool? IsDefault { get; set; }
}
