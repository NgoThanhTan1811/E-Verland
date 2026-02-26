using System.ComponentModel.DataAnnotations;
using Modules.User.Application.DTOs.Request;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Modules.User.Application.Validators
{
    public static class AccountValidator
    {
        private static bool ValidateEmail(string email)
        {
            if (!string.IsNullOrWhiteSpace(email))
                return new EmailAddressAttribute().IsValid(email);
            return false;
        }

        private static bool ValidateUsername(string username)
        {
            if (!string.IsNullOrWhiteSpace(username) && username.Length >= 3)
                return true;
            return false;
        }

        private static bool ValidatePassword(string password)
        {
            if (!string.IsNullOrWhiteSpace(password) && password.Length > 3)
                return true;
            return false;
        }

        public static class CreateAccount
        {
            public static ValidationResult Validate(CreateAccountReqDto dto)
            {
                if (!ValidateEmail(dto.Email))
                    return new ValidationResult("Invalid email format.", [nameof(dto.Email)]);

                if (!ValidatePassword(dto.Password))
                    return new ValidationResult("Password must be longer than 3 characters.", [nameof(dto.Password)]);

                if (!ValidateUsername(dto.Username))
                    return new ValidationResult("Username must be at least 3 characters long.", [nameof(dto.Username)]);

                return ValidationResult.Success!;
            }
        }

        public static class UpdateAccount
        {
            public static ValidationResult Validate(UpdateAccountReqDto dto)
            {
                if (dto.Password != null && !ValidatePassword(dto.Password))
                    return new ValidationResult("Password must be longer than 3 characters.", [nameof(dto.Password)]);

                if (dto.Username != null && !ValidateUsername(dto.Username))
                    return new ValidationResult("Username must be at least 3 characters long.", [nameof(dto.Username)]);

                return ValidationResult.Success!;
            }
        }
    }
}