using Modules.User.Domain.Enums;

namespace Modules.User.Application.DTOs.Request;

public record CreateAddressReqDto
{
    public LableAddress Label; 
    public required string City;
    public required string Ward;
    public required string Detail;
    public required string Street;
    public required string District;
    public required string Province;

}

public record UpdateAddressReqDto
{
    public LableAddress? Label;
    public string? City;
    public string? Ward;
    public string? Detail;
    public string? Street;
    public string? District;
    public string? Province;
    public bool? IsDefault;
}
