using Modules.Payment.Application.DTOs.Response;

namespace Modules.Payment.Application.Contracts
{
    public interface ISePayClient
    {
        Task<string?> CreatePaymentLinkAsync(
            string paymentCode,
            decimal amount,
            string description,
            CancellationToken ct = default);

        Task<SePayTransactionsResponseDto> GetTransactionsAsync(
            string? accountNumber = null,
            DateOnly? transactionDateMin = null,
            DateOnly? transactionDateMax = null,
            long? sinceId = null,
            int? limit = null,
            string? referenceNumber = null,
            decimal? amountIn = null,
            decimal? amountOut = null,
            CancellationToken ct = default);
    }
}
