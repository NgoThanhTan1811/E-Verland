using System.ComponentModel.DataAnnotations;
using Modules.User.Application.DTOs.Request;

namespace Modules.User.Application.Validators
{
    public static class BankAccountValidator
    {
        public static class CreateBankAccount
        {
            public static ValidationResult Validate(CreateBankAccountReqDto dto)
            {
                if (string.IsNullOrWhiteSpace(dto.AccountNumber) || dto.AccountNumber.Length < 10)
                    return new ValidationResult("Account number must be at least 10 characters long.", [nameof(dto.AccountNumber)]);

                if (string.IsNullOrWhiteSpace(dto.BankName))
                    return new ValidationResult("Bank name is required.", [nameof(dto.BankName)]);

                if (string.IsNullOrWhiteSpace(dto.AccountHolder))
                    return new ValidationResult("Account holder name is required.", [nameof(dto.AccountHolder)]);

                if (string.IsNullOrWhiteSpace(dto.BankCode) || dto.BankCode.Length < 3)
                    return new ValidationResult("Bank code must be at least 3 characters long.", [nameof(dto.BankCode)]);

                return ValidationResult.Success!;
            }

        }
        public static class UpdateBankAccount
        {
            public static ValidationResult Validate(UpdateBankAccountReqDto dto)
            {
                if (dto.AccountNumber != null && (string.IsNullOrWhiteSpace(dto.AccountNumber) || dto.AccountNumber.Length < 10))
                    return new ValidationResult("Account number must be at least 10 characters long.", [nameof(dto.AccountNumber)]);

                if (dto.BankName != null && string.IsNullOrWhiteSpace(dto.BankName))
                    return new ValidationResult("Bank name is required.", [nameof(dto.BankName)]);

                if (dto.AccountHolder != null && string.IsNullOrWhiteSpace(dto.AccountHolder))
                    return new ValidationResult("Account holder name is required.", [nameof(dto.AccountHolder)]);

                if (dto.BankCode != null && (string.IsNullOrWhiteSpace(dto.BankCode) || dto.BankCode.Length < 3))
                    return new ValidationResult("Bank code must be at least 3 characters long.", [nameof(dto.BankCode)]);

                return ValidationResult.Success!;
            }
        }
    }
}