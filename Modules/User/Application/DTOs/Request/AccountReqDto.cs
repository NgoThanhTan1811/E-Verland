using Modules.User.Domain.Entities;
using Modules.User.Domain.Enums;

namespace Modules.User.Application.DTOs.Request;

public record CreateAccountReqDto
{
    public required string Email ;
    public required string Username ;
    public required string Password ;

}

public record UpdateAccountReqDto
{
    public string? Username ;
    public string? Password ;
    public RoleUser? Role ;
    public StatusUser? Status ;
}
public record AccountFilter
{
    public string? Keyword ;
    public StatusUser? Status ;
    public RoleUser? Role ;

    public int Page  = 1;
    public int Limit  = 20;
}