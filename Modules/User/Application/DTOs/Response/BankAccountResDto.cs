namespace Modules.User.Application.DTOs.Response
{
    public class BankAccountResDto
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public string BankName { get; set; } = default!;
        public string BankCode { get; set; } = default!;
        public string AccountNumber { get; set; } = default!;
        public string AccountHolder { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}