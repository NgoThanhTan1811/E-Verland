
namespace Modules.User.Application.DTOs.Request
{
    public class CreateBankAccountReqDto
    {
        public string BankName { get; set; } = default!;
        public string BankCode { get; set; } = default!;
        public string AccountNumber { get; set; } = default!;
        public string AccountHolder { get; set; } = default!;
    }

    public class UpdateBankAccountReqDto
    {
        public string? BankName { get; set; }
        public string? BankCode { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolder { get; set; }
    }
}