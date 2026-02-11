using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using Modules.User.Application.DTOs.Request;

namespace Modules.User.Application.Validators
{
    public static class AccountValidator
    {
        public static class CreateAccount
        {
            public static ValidationResult Validation(CreateAccountReqDto dto)
            {
                if (!ValidateEmail(dto.Email))
                    return new ValidationResult("Invalid email format.", [nameof(dto.Email)]);

                if (!ValidatePassword(dto.Password))
                    return new ValidationResult("Password must be at least 6 characters long.", [nameof(dto.Password)]);

                if (!ValidateUsername(dto.Username))
                    return new ValidationResult("Username must be at least 3 characters long.", [nameof(dto.Username)]);

                return ValidationResult.Success!;
            }

            private static bool ValidateEmail(string email)
            {
                if (!string.IsNullOrWhiteSpace(email)) return new EmailAddressAttribute().IsValid(email);
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
                throw new NotImplementedException();
            }
        }
    }
}