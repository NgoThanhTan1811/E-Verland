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

public sealed record ActivateShippingOrderCommand(Guid OrderId) : IRequest<ShippingOrderResponseDto>;

/// <summary>
/// Marks a shipping draft as Pending (ready to pick up) without calling GHN.
/// Actual shipper dispatch is handled manually by the admin via UpdateShippingStatusCommand.
/// </summary>
public sealed class ActivateShippingOrderHandler(
    IShippingRepository repo,
    IShippingDbContext db,
    ISQSService sqsService,
    IConfiguration configuration,
    IMapper mapper,
    ILogger<ActivateShippingOrderHandler> logger)
    : IRequestHandler<ActivateShippingOrderCommand, ShippingOrderResponseDto>
{
    private readonly IShippingRepository _repo = repo;
    private readonly IShippingDbContext _db = db;
    private readonly ISQSService _sqsService = sqsService;
    private readonly IConfiguration _configuration = configuration;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<ActivateShippingOrderHandler> _logger = logger;

    public async Task<ShippingOrderResponseDto> Handle(ActivateShippingOrderCommand request, CancellationToken ct)
    {
        var shipping = await _repo.GetByOrderIdAsync(request.OrderId, ct)
            ?? throw new KeyNotFoundException("Shipping draft not found");

        // Already activated — return current state
        if (shipping.Status != ShippingStatus.Draft)
        {
            return _mapper.Map<ShippingOrderResponseDto>(shipping);
        }

        if (shipping.Status == ShippingStatus.Canceled)
        {
            throw new InvalidOperationException("Shipping order was canceled");
        }

        shipping.Status = ShippingStatus.Pending;
        shipping.ProviderStatus = "pending";
        shipping.LastSyncedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(shipping, ct);
        await _db.SaveChangesAsync(ct);

        await PublishShippingStatusAsync(shipping, ct);

        _logger.LogInformation("Shipping order {OrderId} activated (Draft → Pending, no GHN call)", shipping.OrderId);

        return _mapper.Map<ShippingOrderResponseDto>(shipping);
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
}
