using SharedKernel.Entities;
using Modules.User.Domain.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace Modules.User.Domain.Entities;

public class Account : BaseEntity
{
    [EmailAddress]
    public string Email { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string NormalizedUsername { get; private set; } = default!;
    public string NormalizedEmail { get; private set; } = default!;
    public string Password { get; set; } = default!;
    public RoleUser Role { get; set; } = RoleUser.User;
    public StatusUser Status { get; set; } = StatusUser.Active;

    public Profile Profile { get; set; } = default!;


    private Account() { } 
    public Account(string email, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required.", nameof(password));

        Email = email.Trim();
        Username = username.Trim();
        NormalizedEmail = email.Trim().ToUpperInvariant();
        NormalizedUsername = username.Trim().ToUpperInvariant();
        Password = password;
    }
}

