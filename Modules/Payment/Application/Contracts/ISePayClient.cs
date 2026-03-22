namespace Modules.Payment.Application.Contracts
{
    public interface ISePayClient
    {
        Task<string?> CreatePaymentLinkAsync(
            string paymentCode,
            decimal amount,
            string description,
            CancellationToken ct = default);
    }
}
