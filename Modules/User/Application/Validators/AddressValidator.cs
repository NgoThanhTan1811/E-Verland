using System.ComponentModel.DataAnnotations;
using Modules.User.Application.DTOs.Request;
namespace Modules.User.Application.Validators
{
    public class AddressValidator
    {
        public static ValidationResult Validate(string address)
        {
            if (string.IsNullOrWhiteSpace(address) || address.Length < 5)
                return new ValidationResult("Address must be at least 5 characters long.", [nameof(address)]);

            return ValidationResult.Success!;
        }

        public static ValidationResult ValidateUpdate(string? address)
        {
            if (address != null && (string.IsNullOrWhiteSpace(address) || address.Length < 5))
                return new ValidationResult("Address must be at least 5 characters long.", [nameof(address)]);

            return ValidationResult.Success!;
        }

        public static bool ValidateAddress(string? address)
        {
            if (address != null && (string.IsNullOrWhiteSpace(address) || address.Length < 5))
                return false;

            return true;
        }
    }
}