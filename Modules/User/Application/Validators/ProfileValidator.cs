using System.ComponentModel.DataAnnotations;
using Modules.User.Application.DTOs.Request;
using Modules.User.Domain.Enums;

namespace Modules.User.Application.Validators
{
    public static class ProfileValidator
    {
        public static bool ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;

            phoneNumber = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[\s\-\.]", "");
            var regex = new System.Text.RegularExpressions.Regex(@"^\+?\d{1,15}$");
            return regex.IsMatch(phoneNumber);
        }

        public static bool ValidateLastName(string lastName)
        {
            return !string.IsNullOrWhiteSpace(lastName) && lastName.Trim().Length >= 2;
        }

        public static bool ValidateFirstName(string firstName)
        {
            return !string.IsNullOrWhiteSpace(firstName) && firstName.Trim().Length >= 1;
        }

        public static bool ValidateDateOfBirth(DateTime dateOfBirth)
        {
            return dateOfBirth.Date <= DateTime.Today;
        }

        public static bool ValidateGender(Gender? gender)
        {
            return !gender.HasValue || Enum.IsDefined(typeof(Gender), gender.Value);
        }

        public static class CreateProfile
        {
            public static ValidationResult Validate(CreateProfileReqDto dto)
            {
                if (!ValidateFirstName(dto.FirstName))
                    return new ValidationResult("First name must be at least 1 character long.", [nameof(dto.FirstName)]);

                if (!ValidatePhoneNumber(dto.PhoneNumber))
                    return new ValidationResult("Invalid phone number format.", [nameof(dto.PhoneNumber)]);

                if (!ValidateLastName(dto.LastName))
                    return new ValidationResult("Last name must be at least 2 characters long.", [nameof(dto.LastName)]);

                return ValidationResult.Success!;
            }
        }

        public static class UpdateProfile
        {
            public static ValidationResult Validate(UpdateProfileReqDto dto)
            {
                if (dto.FirstName != null && !ValidateFirstName(dto.FirstName))
                    return new ValidationResult("First name must be at least 1 character long.", [nameof(dto.FirstName)]);

                if (dto.LastName != null && !ValidateLastName(dto.LastName))
                    return new ValidationResult("Last name must be at least 2 characters long.", [nameof(dto.LastName)]);

                if (dto.PhoneNumber != null && !ValidatePhoneNumber(dto.PhoneNumber))
                    return new ValidationResult("Invalid phone number format.", [nameof(dto.PhoneNumber)]);

                if (dto.DateOfBirth.HasValue && !ValidateDateOfBirth(dto.DateOfBirth.Value))
                    return new ValidationResult("Date of birth cannot be in the future.", [nameof(dto.DateOfBirth)]);

                if (dto.Gender.HasValue && !ValidateGender(dto.Gender))
                    return new ValidationResult("Invalid gender value.", [nameof(dto.Gender)]);

                return ValidationResult.Success!;
            }
        }
    }
}