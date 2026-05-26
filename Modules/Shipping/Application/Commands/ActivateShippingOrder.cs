using AutoMapper;
using Infra.AWS.SQS;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Application.DTOs.External;
using Modules.Shipping.Application.DTOs.Response;
using Modules.Shipping.Domain;
using SharedKernel.Events;

namespace Modules.Shipping.Application.Commands;

public sealed record ActivateShippingOrderCommand(Guid OrderId) : IRequest<ShippingOrderResponseDto>;

public sealed class ActivateShippingOrderHandler(
    IShippingRepository repo,
    IShippingDbContext db,
    IGhnClient ghnClient,
    ISQSService sqsService,
    IConfiguration configuration,
    IMapper mapper,
    ILogger<ActivateShippingOrderHandler> logger)
    : IRequestHandler<ActivateShippingOrderCommand, ShippingOrderResponseDto>
{
    private readonly IShippingRepository _repo = repo;
    private readonly IShippingDbContext _db = db;
    private readonly IGhnClient _ghnClient = ghnClient;
    private readonly ISQSService _sqsService = sqsService;
    private readonly IConfiguration _configuration = configuration;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<ActivateShippingOrderHandler> _logger = logger;

    public async Task<ShippingOrderResponseDto> Handle(ActivateShippingOrderCommand request, CancellationToken ct)
    {
        var shipping = await _repo.GetByOrderIdAsync(request.OrderId, ct)
            ?? throw new KeyNotFoundException("Shipping draft not found");

        if (!string.IsNullOrWhiteSpace(shipping.ProviderOrderCode))
        {
            return _mapper.Map<ShippingOrderResponseDto>(shipping);
        }

        if (shipping.Status == ShippingStatus.Canceled)
        {
            throw new InvalidOperationException("Shipping order was canceled");
        }

        if (shipping.ToAddress.DistrictId is null || string.IsNullOrWhiteSpace(shipping.ToAddress.WardCode))
        {
            throw new InvalidOperationException("Missing destination ward code or district id");
        }

        var requiredNote = string.IsNullOrWhiteSpace(shipping.RequiredNote)
            ? "KHONGCHOXEMHANG"
            : shipping.RequiredNote;

        var fromName = _configuration["GHN:FromName"];
        var fromPhone = _configuration["GHN:FromPhone"];
        var fromAddress = _configuration["GHN:FromAddress"];
        var fromWardName = _configuration["GHN:FromWardName"];
        var fromDistrictName = _configuration["GHN:FromDistrictName"];
        var fromProvinceName = _configuration["GHN:FromProvinceName"];

        if (string.IsNullOrWhiteSpace(fromName) || string.IsNullOrWhiteSpace(fromPhone) || string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new InvalidOperationException("Missing GHN sender configuration (GHN:FromName, FromPhone, FromAddress)");
        }

        shipping.FromAddress = new ShippingAddressSnapshot(
            fromName,
            fromPhone,
            fromAddress,
            null,
            null,
            fromWardName,
            fromDistrictName,
            fromProvinceName);

        var items = shipping.Items.Select(i => new GhnCreateOrderItem(
            i.Name,
            i.Code,
            i.Quantity,
            i.Price,
            i.Length,
            i.Width,
            i.Height,
            i.Weight)).ToList();

        var ghnRequest = new GhnCreateOrderRequest(
            PaymentTypeId: shipping.PaymentTypeId ?? 1,
            Note: shipping.Note,
            RequiredNote: requiredNote,
            FromName: fromName,
            FromPhone: fromPhone,
            FromAddress: fromAddress,
            FromWardName: fromWardName,
            FromDistrictName: fromDistrictName,
            FromProvinceName: fromProvinceName,
            ReturnPhone: _configuration["GHN:ReturnPhone"],
            ReturnAddress: _configuration["GHN:ReturnAddress"],
            ReturnDistrictId: TryParseInt(_configuration["GHN:ReturnDistrictId"]),
            ReturnWardCode: _configuration["GHN:ReturnWardCode"],
            ClientOrderCode: shipping.ClientOrderCode,
            ToName: shipping.ToAddress.Name,
            ToPhone: shipping.ToAddress.Phone,
            ToAddress: shipping.ToAddress.Address,
            ToWardCode: shipping.ToAddress.WardCode!,
            ToDistrictId: shipping.ToAddress.DistrictId.Value,
            CodAmount: shipping.CodAmount,
            Content: $"E-Verland order {shipping.OrderId}",
            Weight: shipping.Weight,
            Length: shipping.Length,
            Width: shipping.Width,
            Height: shipping.Height,
            PickStationId: null,
            DeliverStationId: null,
            InsuranceValue: shipping.InsuranceValue,
            ServiceId: shipping.ServiceId,
            ServiceTypeId: shipping.ServiceTypeId,
            Coupon: null,
            PickShift: null,
            Items: items
        );

        var response = await _ghnClient.CreateOrderAsync(ghnRequest, ct);
        if (response.Data is null)
        {
            throw new InvalidOperationException("GHN create order failed with empty response");
        }

        shipping.ProviderOrderCode = response.Data.OrderCode;
        shipping.ExpectedDeliveryTime = response.Data.ExpectedDeliveryTime;
        shipping.TotalFee = response.Data.TotalFee;
        shipping.ProviderStatus = "created";
        shipping.Status = ShippingStatus.Created;
        shipping.LastSyncedAt = DateTime.UtcNow;

        if (response.Data.Fee is not null)
        {
            shipping.FeeSnapshot = new ShippingFeeSnapshot(
                response.Data.TotalFee,
                response.Data.Fee.MainService,
                response.Data.Fee.Insurance,
                response.Data.Fee.StationDo + response.Data.Fee.StationPu,
                response.Data.Fee.Coupon,
                response.Data.Fee.R2S,
                response.Data.Fee.Return,
                0,
                0,
                0,
                0,
                0);
        }

        await _repo.UpdateAsync(shipping, ct);
        await _db.SaveChangesAsync(ct);

        await PublishShippingStatusAsync(shipping, ct);

        _logger.LogInformation("Activated shipping order {OrderId} with provider code {ProviderOrderCode}",
            shipping.OrderId, shipping.ProviderOrderCode);

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

    private static int? TryParseInt(string? value)
    {
        return int.TryParse(value, out var parsed) ? parsed : null;
    }
}
