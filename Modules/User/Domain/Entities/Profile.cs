using SharedKernel.Entities;
using Modules.User.Domain.Enums;

namespace Modules.User.Domain.Entities;

public class Profile : BaseEntity
{
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = default!;

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Other;
    public string? Bio { get; set; } = "Xin chào.";
    public List<Address> Addresses { get; set; } = [];
    public List<BankAccount>? BankAccounts { get; set; } = [];

    private Profile() { } 

    public Profile(Guid accountId, string firstName, string lastName, DateTime dateOfBirth)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId is required.", nameof(accountId));
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("FirstName is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("LastName is required.", nameof(lastName));

        AccountId = accountId;
        DateOfBirth = dateOfBirth;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }


}
