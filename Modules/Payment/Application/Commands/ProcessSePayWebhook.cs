using Infra.AWS.CloudWatch;
using Infra.AWS.SQS;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.DTOs.Response;
using Modules.Payment.Application.Queries;
using Modules.Payment.Domain;
using SharedKernel.Events;

namespace Modules.Payment.Application.Commands;

/// <summary>
/// Command dispatched after HMAC signature is verified by the controller.
/// Contains all business logic for processing a SePay webhook event.
/// </summary>
public sealed record ProcessSePayWebhookCommand(
    string IdempotencyKey,
    string PaymentCode,
    string TransactionStatus,
    decimal Amount,
    Guid? SellerId
) : IRequest<ProcessSePayWebhookResult>;

public sealed record ProcessSePayWebhookResult(
    bool Success,
    bool Compensated = false
);

public sealed class ProcessSePayWebhookHandler(
    IMediator mediator,
    IWebhookIdempotencyService webhookIdempotency,
    ILedgerService ledgerService,
    ISellerBalanceService sellerBalanceService,
    ICloudWatchService cloudWatch,
    IConfiguration configuration,
    ILogger<ProcessSePayWebhookHandler> logger,
    ISQSService? sqsService = null
) : IRequestHandler<ProcessSePayWebhookCommand, ProcessSePayWebhookResult>
{
    public async Task<ProcessSePayWebhookResult> Handle(
        ProcessSePayWebhookCommand request,
        CancellationToken ct)
    {
        // ── Idempotency guard ────────────────────────────────────────────────
        if (await webhookIdempotency.IsProcessedAsync(request.IdempotencyKey, ct))
            return new ProcessSePayWebhookResult(Success: true);

        // ── Resolve payment ──────────────────────────────────────────────────
        var payment = await mediator.Send(new GetPaymentByCodeQuery(request.PaymentCode), ct)
            ?? throw new KeyNotFoundException($"Payment not found for code '{request.PaymentCode}'");

        // ── Skip if already in the target status ─────────────────────────────
        var alreadyInTargetStatus =
            (request.TransactionStatus == "success" && payment.Status == PaymentStatus.Success) ||
            (request.TransactionStatus == "failed" && payment.Status == PaymentStatus.Failed) ||
            (request.TransactionStatus == "refunded" && payment.Status == PaymentStatus.Refunded);

        if (alreadyInTargetStatus)
        {
            await webhookIdempotency.TryMarkAsProcessedAsync(
                request.IdempotencyKey, request.PaymentCode, request.TransactionStatus, ct);
            return new ProcessSePayWebhookResult(Success: true);
        }

        // ── Route by transaction status ──────────────────────────────────────
        return request.TransactionStatus switch
        {
            "success" => await HandleSuccessAsync(payment, request, ct),
            "failed" => await HandleFailedAsync(payment, request, ct),
            "refunded" => await HandleRefundedAsync(payment, request, ct),
            _ => throw new InvalidOperationException(
                              $"Unsupported transaction_status: {request.TransactionStatus}")
        };
    }

    // ── Success path ─────────────────────────────────────────────────────────

    private async Task<ProcessSePayWebhookResult> HandleSuccessAsync(
        PaymentResponseDto payment,
        ProcessSePayWebhookCommand request,
        CancellationToken ct)
    {
        // If the order was already canceled, compensate immediately
        var orderCanceled = await IsOrderCanceledAsync(payment.OrderId, ct);
        if (orderCanceled)
        {
            await ledgerService.RecordIncomingPaymentAsync(
                payment.OrderId, payment.Amount, "VND",
                $"incoming:{request.IdempotencyKey}", "sepay-webhook", ct);

            await ledgerService.RecordIncomingPaymentReversalAsync(
                payment.OrderId, payment.Amount, "VND",
                $"incoming-reversal:{request.IdempotencyKey}", "sepay-webhook-compensation", ct);

            await sellerBalanceService.ReversePendingBalanceAsync(
                payment.OrderId, "canceled-order-webhook", ct);

            await cloudWatch.PutMetricAsync("payment.webhook.compensated", 1, "Count", ct: ct);

            await webhookIdempotency.TryMarkAsProcessedAsync(
                request.IdempotencyKey, request.PaymentCode, "compensated", ct);

            return new ProcessSePayWebhookResult(Success: true, Compensated: true);
        }

        // Normal success flow
        var incomingPosted = false;
        try
        {
            // Confirm stock reservation via SQS (Product module handles it asynchronously)
            await PublishStockConfirmAsync(payment.Id, payment.OrderId, ct);

            incomingPosted = await ledgerService.RecordIncomingPaymentAsync(
                payment.OrderId, payment.Amount, "VND",
                $"incoming:{request.IdempotencyKey}", "sepay-webhook", ct);

            var releaseDelayDays = int.TryParse(
                configuration["Payment:Payout:ReleaseDelayDays"], out var d) ? Math.Max(1, d) : 3;

            await sellerBalanceService.EnsurePendingBalanceAsync(
                payment.OrderId,
                request.SellerId ?? Guid.Empty,
                payment.Amount,
                "VND",
                DateTime.UtcNow.AddDays(releaseDelayDays),
                ct);

            await mediator.Send(new UpdatePaymentStatusCommand(payment.Id, PaymentStatus.Success), ct);

            await webhookIdempotency.TryMarkAsProcessedAsync(
                request.IdempotencyKey, request.PaymentCode, "success", ct);

            return new ProcessSePayWebhookResult(Success: true);
        }
        catch (Exception ex)
        {
            await cloudWatch.PutMetricAsync("payment.webhook.failed", 1, "Count", ct: ct);
            logger.LogError(ex, "Success webhook processing failed for payment {PaymentCode}", request.PaymentCode);

            // Compensate
            await CompensateSuccessAsync(payment, request.IdempotencyKey, incomingPosted, ct);
            throw;
        }
    }

    private async Task CompensateSuccessAsync(
        PaymentResponseDto payment,
        string idempotencyKey,
        bool incomingPosted,
        CancellationToken ct)
    {
        try
        {
            await PublishStockReleaseAsync(payment.Id, payment.OrderId, ct);

            if (incomingPosted)
            {
                await ledgerService.RecordIncomingPaymentReversalAsync(
                    payment.OrderId, payment.Amount, "VND",
                    $"incoming-reversal:{idempotencyKey}", "sepay-webhook-compensation", ct);
            }

            await sellerBalanceService.ReversePendingBalanceAsync(
                payment.OrderId, "webhook-success-failure", ct);
        }
        catch (Exception compEx)
        {
            logger.LogError(compEx,
                "Compensation also failed for payment {OrderId}. Manual intervention required.",
                payment.OrderId);
            throw new AggregateException(
                "Payment processing failed and compensation also failed.", compEx);
        }
    }

    // ── Failed path ──────────────────────────────────────────────────────────

    private async Task<ProcessSePayWebhookResult> HandleFailedAsync(
        PaymentResponseDto payment,
        ProcessSePayWebhookCommand request,
        CancellationToken ct)
    {
        await mediator.Send(new UpdatePaymentStatusCommand(payment.Id, PaymentStatus.Failed), ct);
        await PublishStockReleaseAsync(payment.Id, payment.OrderId, ct);
        await sellerBalanceService.ReversePendingBalanceAsync(payment.OrderId, "payment-failed", ct);

        await webhookIdempotency.TryMarkAsProcessedAsync(
            request.IdempotencyKey, request.PaymentCode, "failed", ct);

        return new ProcessSePayWebhookResult(Success: true);
    }

    // ── Refunded path ────────────────────────────────────────────────────────

    private async Task<ProcessSePayWebhookResult> HandleRefundedAsync(
        PaymentResponseDto payment,
        ProcessSePayWebhookCommand request,
        CancellationToken ct)
    {
        await PublishStockReleaseAsync(payment.Id, payment.OrderId, ct);

        await ledgerService.RecordIncomingPaymentReversalAsync(
            payment.OrderId, payment.Amount, "VND",
            $"refund:{request.IdempotencyKey}", "sepay-refund", ct);

        await sellerBalanceService.ReversePendingBalanceAsync(payment.OrderId, "payment-refunded", ct);

        await mediator.Send(new UpdatePaymentStatusCommand(payment.Id, PaymentStatus.Refunded), ct);

        await webhookIdempotency.TryMarkAsProcessedAsync(
            request.IdempotencyKey, request.PaymentCode, "refunded", ct);

        return new ProcessSePayWebhookResult(Success: true);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<bool> IsOrderCanceledAsync(Guid orderId, CancellationToken ct)
    {
        try
        {
            var order = await mediator.Send(
                new Modules.Order.Application.Queries.GetOrderByIdQuery(orderId, Guid.Empty), ct);
            return order?.Status == Modules.Order.Domain.OrderStatus.Canceled;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not retrieve order {OrderId} to check cancellation status", orderId);
            return false;
        }
    }

    private async Task PublishStockConfirmAsync(Guid paymentId, Guid orderId, CancellationToken ct)
    {
        if (sqsService is null)
            throw new InvalidOperationException("SQS service is not configured for stock confirmation.");

        var queueUrl = ResolveQueueUrl("AWS:SQS:StockConfirmQueueUrl", "SQS:StockConfirmQueueUrl",
            "AWS_SQS_STOCK_CONFIRM_QUEUE_URL");
        if (string.IsNullOrWhiteSpace(queueUrl))
            throw new InvalidOperationException("Stock confirmation queue URL is not configured.");

        await sqsService.SendMessageAsync(queueUrl,
            new StockConfirmRequested(paymentId, orderId, DateTime.UtcNow), ct);
    }

    private async Task PublishStockReleaseAsync(Guid paymentId, Guid orderId, CancellationToken ct)
    {
        if (sqsService is null) return;
        var queueUrl = ResolveQueueUrl("AWS:SQS:StockReleaseQueueUrl", "SQS:StockReleaseQueueUrl",
            "AWS_SQS_STOCK_RELEASE_QUEUE_URL");
        if (string.IsNullOrWhiteSpace(queueUrl)) return;

        try
        {
            await sqsService.SendMessageAsync(queueUrl,
                new StockReleaseRequested(paymentId, orderId, DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish StockReleaseRequested for payment {PaymentId}", paymentId);
        }
    }

    private string? ResolveQueueUrl(string key1, string key2, string envVar) =>
        configuration[key1] ?? configuration[key2] ?? Environment.GetEnvironmentVariable(envVar);
}
