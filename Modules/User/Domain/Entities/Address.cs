using Modules.User.Domain.Enums;
using SharedKernel.Entities;

namespace Modules.User.Domain.Entities;

public class Address : BaseEntity
{
    public Guid ProfileId { get; set; }
    public required Profile Profile { get; set; } 

    public LableAddress Label { get; set; } = LableAddress.Other;
    public required string City { get; set; } 
    public required string Ward { get; set; } 
    public required string Detail { get; set; } 
    public required string Street { get; set; } 
    public required string District { get; set; } 
    public required string Province { get; set; } 

    public bool IsDefault { get; set; }

    private Address() { }
    public Address(Guid profileId, LableAddress label, string city, string ward, string detail, string street, string district, string province, bool isDefault)
    {

        ProfileId = profileId;
        Label = label;
        City = city;
        Ward = ward;
        Detail = detail;
        Street = street;
        District = district;
        Province = province;
        IsDefault = false;
    }

    public void SetAsDefault() => IsDefault = true;

};