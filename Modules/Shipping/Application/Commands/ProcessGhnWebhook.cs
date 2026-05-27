using Infra.AWS.SQS;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Application.DTOs.External;
using Modules.Shipping.Domain;
using SharedKernel.Events;

namespace Modules.Shipping.Application.Commands;

public sealed record ProcessGhnWebhookCommand(GhnWebhookPayload Payload) : IRequest<Unit>;

public sealed class ProcessGhnWebhookHandler(
    IShippingRepository repo,
    IShippingDbContext db,
    ISQSService sqsService,
    IConfiguration configuration,
    ILogger<ProcessGhnWebhookHandler> logger)
    : IRequestHandler<ProcessGhnWebhookCommand, Unit>
{
    private readonly IShippingRepository _repo = repo;
    private readonly IShippingDbContext _db = db;
    private readonly ISQSService _sqsService = sqsService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<ProcessGhnWebhookHandler> _logger = logger;

    public async Task<Unit> Handle(ProcessGhnWebhookCommand request, CancellationToken ct)
    {
        var payload = request.Payload;
        var shipping = await _repo.GetByProviderOrderCodeAsync(payload.OrderCode, ct);

        if (shipping is null)
        {
            _logger.LogWarning("Received GHN webhook for unknown order code {OrderCode}", payload.OrderCode);
            return Unit.Value;
        }

        shipping.ProviderStatus = payload.Status;
        shipping.Status = MapStatus(payload.Status);
        shipping.LastSyncedAt = payload.Time ?? DateTime.UtcNow;

        if (payload.TotalFee.HasValue)
        {
            shipping.TotalFee = payload.TotalFee.Value;
        }

        await _repo.UpdateAsync(shipping, ct);
        await _db.SaveChangesAsync(ct);

        await PublishShippingStatusAsync(shipping, ct);

        return Unit.Value;
    }

    private async Task PublishShippingStatusAsync(ShippingOrder shipping, CancellationToken ct)
    {
        var queueUrl = _configuration["AWS:SQS:ShippingStatusQueueUrl"]
            ?? _configuration["SQS:ShippingStatusQueueUrl"]
            ?? Environment.GetEnvironmentVariable("AWS_SQS_SHIPPING_STATUS_QUEUE_URL");

        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            return;
        }

        var status = shipping.ProviderStatus ?? shipping.Status.ToString();
        var evt = new ShippingStatusChanged(shipping.OrderId, shipping.ProviderOrderCode, status, DateTime.UtcNow);

        try
        {
            await _sqsService.SendMessageAsync(queueUrl, evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish shipping status for order {OrderId}", shipping.OrderId);
        }
    }

    private static ShippingStatus MapStatus(string status)
    {
        var normalized = status?.Trim().ToLowerInvariant() ?? string.Empty;

        return normalized switch
        {
            "ready_to_pick" => ShippingStatus.Picking,
            "picking" => ShippingStatus.Picking,
            "picked" => ShippingStatus.Picked,
            "storing" => ShippingStatus.Delivering,
            "transporting" => ShippingStatus.Delivering,
            "delivering" => ShippingStatus.Delivering,
            "delivered" => ShippingStatus.Delivered,
            "return" => ShippingStatus.Returned,
            "returned" => ShippingStatus.Returned,
            "cancel" => ShippingStatus.Canceled,
            "canceled" => ShippingStatus.Canceled,
            "cancelled" => ShippingStatus.Canceled,
            "exception" => ShippingStatus.Failed,
            "lost" => ShippingStatus.Failed,
            "damage" => ShippingStatus.Failed,
            _ => ShippingStatus.Created
        };
    }
}
