using Modules.User.Domain.Entities;
using Modules.User.Domain.Enums;

namespace Modules.User.Application.DTOs.Request;

public class CreateAccountReqDto
{
    public string Email { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;

}

public class UpdateAccountReqDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public RoleUser? Role { get; set; }
    public StatusUser? Status { get; set; }
}
public class AccountFilter
{
    public string? Keyword { get; set; }
    public StatusUser? Status { get; set; }
    public RoleUser? Role { get; set; }

    public int? Page { get; set; } = 1;
    public int? Limit { get; set; } = 20;
}