using AutoMapper;
using Infra.AWS.SQS;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Application.DTOs.Response;
using Modules.Shipping.Domain;
using SharedKernel.Events;

namespace Modules.Shipping.Application.Commands;

public sealed record UpdateShippingStatusCommand(Guid OrderId, ShippingStatus Status) : IRequest<ShippingOrderResponseDto>;

/// <summary>
/// Allows admin to manually advance the shipping status without calling GHN.
/// Used when shipping is managed outside GHN (e.g. internal delivery, testing).
/// </summary>
public sealed class UpdateShippingStatusHandler(
    IShippingRepository repo,
    IShippingDbContext db,
    ISQSService sqsService,
    IConfiguration configuration,
    IMapper mapper,
    ILogger<UpdateShippingStatusHandler> logger)
    : IRequestHandler<UpdateShippingStatusCommand, ShippingOrderResponseDto>
{
    private static readonly HashSet<ShippingStatus> TerminalStatuses =
    [
        ShippingStatus.Delivered,
        ShippingStatus.Returned,
        ShippingStatus.Canceled,
        ShippingStatus.Failed
    ];

    public async Task<ShippingOrderResponseDto> Handle(UpdateShippingStatusCommand request, CancellationToken ct)
    {
        var shipping = await repo.GetByOrderIdAsync(request.OrderId, ct)
            ?? throw new KeyNotFoundException("Shipping order not found");

        if (TerminalStatuses.Contains(shipping.Status))
        {
            throw new InvalidOperationException(
                $"Cannot update status: shipping is already in terminal state '{shipping.Status}'.");
        }

        var previous = shipping.Status;
        shipping.Status = request.Status;
        shipping.ProviderStatus = request.Status.ToString().ToLowerInvariant();
        shipping.LastSyncedAt = DateTime.UtcNow;

        await repo.UpdateAsync(shipping, ct);
        await db.SaveChangesAsync(ct);

        await PublishShippingStatusAsync(shipping, ct);

        logger.LogInformation(
            "Shipping status for order {OrderId} manually updated: {Previous} → {New}",
            shipping.OrderId, previous, request.Status);

        return mapper.Map<ShippingOrderResponseDto>(shipping);
    }

    private async Task PublishShippingStatusAsync(ShippingOrder shipping, CancellationToken ct)
    {
        var queueUrl = configuration["AWS:SQS:ShippingStatusQueueUrl"]
            ?? configuration["SQS:ShippingStatusQueueUrl"]
            ?? Environment.GetEnvironmentVariable("AWS_SQS_SHIPPING_STATUS_QUEUE_URL");

        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            return;
        }

        var status = shipping.ProviderStatus ?? shipping.Status.ToString();
        var evt = new ShippingStatusChanged(shipping.OrderId, shipping.ProviderOrderCode, status, DateTime.UtcNow);

        try
        {
            await sqsService.SendMessageAsync(queueUrl, evt, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish shipping status for order {OrderId}", shipping.OrderId);
        }
    }
}
