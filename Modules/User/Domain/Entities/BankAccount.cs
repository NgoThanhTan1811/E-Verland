using SharedKernel.Entities;
namespace Modules.User.Domain.Entities;

public class BankAccount : BaseEntity
{
    public Guid ProfileId { get; private set; }
    public Profile Profile { get; private set; } = default!;

    public string BankName { get; private set; } = default!;
    public string BankCode { get; private set; } = default!;
    public string AccountNumber { get; private set; } = default!;
    public string AccountHolder { get; private set; } = default!;

    private BankAccount() { } // EF

    public BankAccount(Guid profileId, string bankName, string bankCode, string accountNumber, string accountHolder)
    {
        if (profileId == Guid.Empty) throw new ArgumentException("ProfileId is required.", nameof(profileId));
        if (string.IsNullOrWhiteSpace(bankName)) throw new ArgumentException("BankName is required.", nameof(bankName));
        if (string.IsNullOrWhiteSpace(bankCode)) throw new ArgumentException("BankCode is required.", nameof(bankCode));
        if (string.IsNullOrWhiteSpace(accountNumber)) throw new ArgumentException("AccountNumber is required.", nameof(accountNumber));
        if (string.IsNullOrWhiteSpace(accountHolder)) throw new ArgumentException("AccountHolder is required.", nameof(accountHolder));

        ProfileId = profileId;
        BankName = bankName.Trim();
        BankCode = bankCode.Trim().ToUpperInvariant();
        AccountNumber = accountNumber.Trim();
        AccountHolder = accountHolder.Trim();
    }

    public void Update(string bankName, string bankCode, string accountNumber, string accountHolder)
    {
        if (string.IsNullOrWhiteSpace(bankName)) throw new ArgumentException("BankName is required.", nameof(bankName));
        if (string.IsNullOrWhiteSpace(bankCode)) throw new ArgumentException("BankCode is required.", nameof(bankCode));
        if (string.IsNullOrWhiteSpace(accountNumber)) throw new ArgumentException("AccountNumber is required.", nameof(accountNumber));
        if (string.IsNullOrWhiteSpace(accountHolder)) throw new ArgumentException("AccountHolder is required.", nameof(accountHolder));
        if (accountNumber.Any(ch => !char.IsDigit(ch)))
            throw new ArgumentException("AccountNumber must be numeric.");
            
        BankName = bankName.Trim();
        BankCode = bankCode.Trim().ToUpperInvariant();
        AccountNumber = accountNumber.Trim();
        AccountHolder = accountHolder.Trim();
    }
}
