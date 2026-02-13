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

    public void Update(string? firstName,string? lastName,DateTime? dateOfBirth,string? avatarUrl,Gender? gender,string? bio)
    {
        if (firstName is not null)
        {
            if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("FirstName is required.", nameof(firstName));
            FirstName = firstName.Trim();
        }

        if (lastName is not null)
        {
            if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("LastName is required.", nameof(lastName));
            LastName = lastName.Trim();
        }

        if (dateOfBirth.HasValue)
        {
            var dob = dateOfBirth.Value.Date;
            if (dob > DateTime.Today) throw new ArgumentException("Date of Birth is wrong", nameof(dateOfBirth));
            DateOfBirth = dob;
        }

        if (avatarUrl is not null)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl)) throw new ArgumentException("AvatarUrl is required.", nameof(avatarUrl));
            AvatarUrl = avatarUrl.Trim();
        }

        if (gender.HasValue)
        {
            var g = gender.Value;
            if (!Enum.IsDefined(g)) throw new ArgumentException("Invalid gender.", nameof(gender));
            Gender = g;
        }

        if (bio is not null)
        {
            if (string.IsNullOrWhiteSpace(bio)) throw new ArgumentException("Bio is required.", nameof(bio));
            Bio = bio.Trim();
        }
    }



}
