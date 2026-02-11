using System.ComponentModel.DataAnnotations;
using Modules.User.Application.DTOs.Request;

namespace Modules.User.Application.Validators
{
    public static class ProfileValidator
    {
        public static bool ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            var regex = new System.Text.RegularExpressions.Regex(@"^\+?[1-9]\d{1,14}$");
            return regex.IsMatch(phoneNumber);
        }
        public static bool ValidateLastName(string lastName)
        {
            return !string.IsNullOrWhiteSpace(lastName) && lastName.Length >= 2;
        }

        public static bool ValidateFirstName(string firstName)
        {
            return !string.IsNullOrWhiteSpace(firstName) && firstName.Length >= 1;
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
                if (dto.LastName != null && !ValidateLastName(dto.LastName))
                    return new ValidationResult("Last name must be at least 2 characters long.", [nameof(dto.LastName)]);

                if (dto.PhoneNumber != null && !ValidatePhoneNumber(dto.PhoneNumber))
                    return new ValidationResult("Invalid phone number format.", [nameof(dto.PhoneNumber)]);

                if (dto.FirstName != null && !ValidateFirstName(dto.FirstName))
                    return new ValidationResult("First name must be at least 1 character long.", [nameof(dto.FirstName)]);

                return ValidationResult.Success!;
            }

        }
    }
}