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
                if (!ValidateAddressField(dto.Street))
                    return new ValidationResult("Street is required and must be at least 2 characters.", [nameof(dto.Street)]);

                if (!ValidateAddressField(dto.Detail, 5))
                    return new ValidationResult("Detail is required and must be at least 5 characters.", [nameof(dto.Detail)]);

                if (dto.ProvinceId <= 0)
                    return new ValidationResult("ProvinceId is required.", [nameof(dto.ProvinceId)]);

                if (dto.DistrictId <= 0)
                    return new ValidationResult("DistrictId is required.", [nameof(dto.DistrictId)]);

                if (dto.WardId <= 0)
                    return new ValidationResult("WardId is required.", [nameof(dto.WardId)]);

                return ValidationResult.Success!;
            }
        }

        public static class UpdateAddress
        {
            public static ValidationResult Validate(UpdateAddressReqDto dto)
            {
                if (dto.Street != null && !ValidateAddressField(dto.Street))
                    return new ValidationResult("Street must be at least 2 characters.", [nameof(dto.Street)]);

                if (dto.Detail != null && !ValidateAddressField(dto.Detail, 5))
                    return new ValidationResult("Detail must be at least 5 characters.", [nameof(dto.Detail)]);

                if (dto.ProvinceId.HasValue && dto.ProvinceId <= 0)
                    return new ValidationResult("ProvinceId must be greater than 0.", [nameof(dto.ProvinceId)]);

                if (dto.DistrictId.HasValue && dto.DistrictId <= 0)
                    return new ValidationResult("DistrictId must be greater than 0.", [nameof(dto.DistrictId)]);

                if (dto.WardId.HasValue && dto.WardId <= 0)
                    return new ValidationResult("WardId must be greater than 0.", [nameof(dto.WardId)]);

                return ValidationResult.Success!;
            }
        }
    }
}