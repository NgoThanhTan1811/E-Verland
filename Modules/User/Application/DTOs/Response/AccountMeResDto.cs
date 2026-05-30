namespace Modules.User.Application.DTOs.Response;

public class AccountMeResDto
{
    public AccountResDto Account { get; set; } = default!;
    public ProfileResDto? Profile { get; set; }
    public IReadOnlyList<AddressResDto> Addresses { get; set; } = [];
    public IReadOnlyList<BankAccountResDto> BankAccounts { get; set; } = [];
}