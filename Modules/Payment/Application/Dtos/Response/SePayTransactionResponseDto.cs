using System.Text.Json.Serialization;

namespace Modules.Payment.Application.DTOs.Response;

public sealed record SePayTransactionResponseDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("bank_brand_name")] string BankBrandName,
    [property: JsonPropertyName("account_number")] string AccountNumber,
    [property: JsonPropertyName("transaction_date")] string TransactionDate,
    [property: JsonPropertyName("amount_out")] string AmountOut,
    [property: JsonPropertyName("amount_in")] string AmountIn,
    [property: JsonPropertyName("accumulated")] string Accumulated,
    [property: JsonPropertyName("transaction_content")] string TransactionContent,
    [property: JsonPropertyName("reference_number")] string ReferenceNumber,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("sub_account")] string? SubAccount,
    [property: JsonPropertyName("bank_account_id")] string? BankAccountId
);

public sealed record SePayTransactionMessagesResponseDto(
    [property: JsonPropertyName("success")] bool Success
);

public sealed record SePayTransactionsResponseDto(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("messages")] SePayTransactionMessagesResponseDto? Messages,
    [property: JsonPropertyName("transactions")] IReadOnlyList<SePayTransactionResponseDto> Transactions
);