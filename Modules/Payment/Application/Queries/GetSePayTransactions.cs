using MediatR;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.DTOs.Response;

namespace Modules.Payment.Application.Queries;

public sealed record GetSePayTransactionsQuery(
    string? AccountNumber = null,
    DateOnly? TransactionDateMin = null,
    DateOnly? TransactionDateMax = null,
    long? SinceId = null,
    int? Limit = null,
    string? ReferenceNumber = null,
    decimal? AmountIn = null,
    decimal? AmountOut = null
) : IRequest<SePayTransactionsResponseDto>;

public sealed class GetSePayTransactionsHandler(ISePayClient sePayClient)
    : IRequestHandler<GetSePayTransactionsQuery, SePayTransactionsResponseDto>
{
    public Task<SePayTransactionsResponseDto> Handle(GetSePayTransactionsQuery request, CancellationToken ct)
        => sePayClient.GetTransactionsAsync(
            request.AccountNumber,
            request.TransactionDateMin,
            request.TransactionDateMax,
            request.SinceId,
            request.Limit,
            request.ReferenceNumber,
            request.AmountIn,
            request.AmountOut,
            ct);
}