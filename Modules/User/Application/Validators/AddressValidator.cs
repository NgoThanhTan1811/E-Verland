using System.ComponentModel.DataAnnotations;
using Modules.User.Application.DTOs.Request;

namespace Modules.User.Application.Validators
{
    public static class AddressValidator
    {
        private static bool ValidateAddressField(string? value, int minLength = 2)
        {
            return value != null && !string.IsNullOrWhiteSpace(value) && value.Trim().Length >= minLength;
        }

        public static class CreateAddress
        {
            public static ValidationResult Validate(CreateAddressReqDto dto)
            {
                if (!ValidateAddressField(dto.City))
                    return new ValidationResult("City is required and must be at least 2 characters.", [nameof(dto.City)]);

                if (!ValidateAddressField(dto.Province))
                    return new ValidationResult("Province is required and must be at least 2 characters.", [nameof(dto.Province)]);

                if (!ValidateAddressField(dto.District))
                    return new ValidationResult("District is required and must be at least 2 characters.", [nameof(dto.District)]);

                if (!ValidateAddressField(dto.Ward))
                    return new ValidationResult("Ward is required and must be at least 2 characters.", [nameof(dto.Ward)]);

                if (!ValidateAddressField(dto.Street))
                    return new ValidationResult("Street is required and must be at least 2 characters.", [nameof(dto.Street)]);

                if (!ValidateAddressField(dto.Detail, 5))
                    return new ValidationResult("Detail is required and must be at least 5 characters.", [nameof(dto.Detail)]);

                if (dto.ProvinceId <= 0)
                    return new ValidationResult("ProvinceId is required.", [nameof(dto.ProvinceId)]);

                if (dto.DistrictId <= 0)
                    return new ValidationResult("DistrictId is required.", [nameof(dto.DistrictId)]);

                if (!ValidateAddressField(dto.WardCode))
                    return new ValidationResult("WardCode is required.", [nameof(dto.WardCode)]);

                return ValidationResult.Success!;
            }
        }

        public static class UpdateAddress
        {
            public static ValidationResult Validate(UpdateAddressReqDto dto)
            {
                if (dto.City != null && !ValidateAddressField(dto.City))
                    return new ValidationResult("City must be at least 2 characters.", [nameof(dto.City)]);

                if (dto.Province != null && !ValidateAddressField(dto.Province))
                    return new ValidationResult("Province must be at least 2 characters.", [nameof(dto.Province)]);

                if (dto.District != null && !ValidateAddressField(dto.District))
                    return new ValidationResult("District must be at least 2 characters.", [nameof(dto.District)]);

                if (dto.Ward != null && !ValidateAddressField(dto.Ward))
                    return new ValidationResult("Ward must be at least 2 characters.", [nameof(dto.Ward)]);

                if (dto.Street != null && !ValidateAddressField(dto.Street))
                    return new ValidationResult("Street must be at least 2 characters.", [nameof(dto.Street)]);

                if (dto.Detail != null && !ValidateAddressField(dto.Detail, 5))
                    return new ValidationResult("Detail must be at least 5 characters.", [nameof(dto.Detail)]);

                if (dto.ProvinceId.HasValue && dto.ProvinceId <= 0)
                    return new ValidationResult("ProvinceId must be greater than 0.", [nameof(dto.ProvinceId)]);

                if (dto.DistrictId.HasValue && dto.DistrictId <= 0)
                    return new ValidationResult("DistrictId must be greater than 0.", [nameof(dto.DistrictId)]);

                if (dto.WardCode != null && !ValidateAddressField(dto.WardCode))
                    return new ValidationResult("WardCode must be at least 1 character.", [nameof(dto.WardCode)]);

                return ValidationResult.Success!;
            }
        }
    }
}