using SharedKernel.Entities;
using Modules.User.Domain.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace Modules.User.Domain.Entities;

public class Account : BaseEntity
{
    [EmailAddress]
    public required string Email { get; set; } 
    public required string Username { get; set; } 
    public string NormalizedUsername { get; private set; } = default!;
    public string NormalizedEmail { get; private set; } = default!;
    public required string Password { get; set; } 
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

