namespace Modules.User.Application.DTOs.Response
{
    public class BankAccountResDto
    {
        public string BankName { get; private set; } = default!;
        public string BankCode { get; private set; } = default!;
        public string AccountNumber { get; private set; } = default!;
        public string AccountHolder { get; private set; } = default!;
    }
}