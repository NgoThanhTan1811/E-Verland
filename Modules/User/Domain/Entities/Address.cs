using Modules.User.Domain.Enums;
using SharedKernel.Entities;

namespace Modules.User.Domain.Entities;

public class Address : BaseEntity
{
    public Guid ProfileId { get; set; } = default!;
    public Profile Profile { get; set; } = default!;

    public LableAddress Label { get; set; } = LableAddress.Other;
    public string City { get; set; } = default!;
    public string Ward { get; set; } = default!;
    public string Detail { get; set; } = default!;
    public string Street { get; set; } = default!;
    public string District { get; set; } = default!;
    public string Province { get; set; } = default!;
    public int? ProvinceId { get; set; }
    public int? DistrictId { get; set; }
    public string? WardCode { get; set; }

    public bool IsDefault { get; set; } = false;

    private Address() { }
    public Address(
        Guid profileId,
        LableAddress label,
        string city,
        string province,
        string district,
        string ward,
        string street,
        string detail,
        bool isDefault,
        int? provinceId,
        int? districtId,
        string? wardCode)
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
        ProvinceId = provinceId;
        DistrictId = districtId;
        WardCode = wardCode;
    }

    public void SetAsDefault() => IsDefault = true;

    public void Update(
        LableAddress? label,
        string? city,
        string? province,
        string? district,
        string? ward,
        string? street,
        string? detail,
        bool? isDefault,
        int? provinceId,
        int? districtId,
        string? wardCode)
    {
        if (label.HasValue) Label = label.Value;
        if (city is not null) City = city.Trim();
        if (province is not null) Province = province.Trim();
        if (district is not null) District = district.Trim();
        if (ward is not null) Ward = ward.Trim();
        if (street is not null) Street = street.Trim();
        if (detail is not null) Detail = detail.Trim();
        if (isDefault.HasValue) IsDefault = isDefault.Value;
        if (provinceId.HasValue) ProvinceId = provinceId.Value;
        if (districtId.HasValue) DistrictId = districtId.Value;
        if (wardCode is not null) WardCode = wardCode.Trim();
    }

};