using Modules.User.Domain.Entities;
using Modules.User.Domain.Enums;
using SharedKernel.Pagination;

namespace Modules.User.Application.DTOs.Request;

public record CreateAccountReqDto
{
    public required string Email;
    public required string Username;
    public required string Password;

}

public record UpdateAccountReqDto
{
    public string? Username;
    public string? Password;
    public RoleUser? Role;
    public StatusUser? Status;
}

public record UpdateMyAccountReqDto
{
    public string? Username;
    public string? Password;
}
public record AccountFilter : IPagingFilter
{
    public string? Keyword;
    public StatusUser? Status;
    public RoleUser? Role;

    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
}