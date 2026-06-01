using Modules.User.Application.DTOs.Response;
using Modules.User.Domain.Entities;

namespace Modules.User.Application.Mappings;

public static class UserManualMapper
{
    public static AccountResDto ToResDto(this Account entity)
    {
        return new AccountResDto
        {
            Id = entity.Id,
            Email = entity.Email,
            Username = entity.Username,
            NormalizedUsername = entity.NormalizedUsername,
            NormalizedEmail = entity.NormalizedEmail,
            Role = entity.Role,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static AddressResDto ToResDto(this Address entity)
    {
        return new AddressResDto
        {
            Id = entity.Id,
            ProfileId = entity.ProfileId,
            Label = entity.Label,
            ProvinceId = entity.ProvinceId,
            DistrictId = entity.DistrictId,
            WardId = int.TryParse(entity.WardCode, out var wardId) ? wardId : null,
            Street = entity.Street,
            Detail = entity.Detail,
            IsDefault = entity.IsDefault,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static BankAccountResDto ToResDto(this BankAccount entity)
    {
        return new BankAccountResDto
        {
            Id = entity.Id,
            ProfileId = entity.ProfileId,
            BankName = entity.BankName,
            BankCode = entity.BankCode,
            AccountNumber = entity.AccountNumber,
            AccountHolder = entity.AccountHolder,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static ProfileResDto ToResDto(this Profile entity)
    {
        return new ProfileResDto
        {
            Id = entity.Id,
            AccountId = entity.AccountId,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Bio = entity.Bio,
            PhoneNumber = entity.PhoneNumber,
            Gender = entity.Gender,
            AvatarUrl = entity.AvatarUrl,
            DateOfBirth =  DateTime.TryParse(entity.DateOfBirth?.ToString(), out var dob) ? dob : (DateTime?)null,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}