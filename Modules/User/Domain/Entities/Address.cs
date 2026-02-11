using Modules.User.Domain.Enums;
using SharedKernel.Entities;

namespace Modules.User.Domain.Entities;

public class Address : BaseEntity
{
    public Guid ProfileId { get; set; } = default!;
    public  Profile Profile { get; set; } = default!;

    public LableAddress Label { get; set; } = LableAddress.Other;
    public  string City { get; set; } = default!;
    public  string Ward { get; set; } = default!;
    public  string Detail { get; set; } = default!; 
    public  string Street { get; set; } = default!;
    public  string District { get; set; } = default!;
    public  string Province { get; set; } = default!;

    public bool IsDefault { get; set; } = false;

    private Address() { }
    public Address(Guid profileId, LableAddress label, string city, string province, string district, string ward, string street, string detail,  bool isDefault)
    {

        ProfileId = profileId;
        Label = label;
        City = city;
        Ward = ward;
        Detail = detail;
        Street = street;
        District = district;
        Province = province;
        IsDefault = isDefault;
    }

    public void SetAsDefault() => IsDefault = true;

};