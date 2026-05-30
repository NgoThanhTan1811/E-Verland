using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Application.DTOs.Request;
using Modules.Shipping.Application.DTOs.Response;
using Modules.Shipping.Domain;

namespace Modules.Shipping.Application.Commands;

public sealed record CreateShippingDraftCommand(CreateShippingDraftRequestDto Payload) : IRequest<ShippingOrderResponseDto>;

public sealed class CreateShippingDraftHandler(
    IShippingRepository repo,
    IShippingDbContext db,
    IMapper mapper,
    ILogger<CreateShippingDraftHandler> logger)
    : IRequestHandler<CreateShippingDraftCommand, ShippingOrderResponseDto>
{
    private readonly IShippingRepository _repo = repo;
    private readonly IShippingDbContext _db = db;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<CreateShippingDraftHandler> _logger = logger;

    public async Task<ShippingOrderResponseDto> Handle(CreateShippingDraftCommand request, CancellationToken ct)
    {
        var payload = request.Payload;
        var existing = await _repo.GetByOrderIdAsync(payload.OrderId, ct);
        if (existing is not null)
        {
            return _mapper.Map<ShippingOrderResponseDto>(existing);
        }

        if (payload.Items == null || payload.Items.Count == 0)
            throw new ArgumentException("Shipping draft requires at least one item");

        var totalUnits = Math.Max(1, payload.Items.Sum(i => i.Quantity));
        var fallbackWeight = Math.Max(1, payload.Dimensions.Weight / totalUnits);

        var items = payload.Items.Select(i => new ShippingItemSnapshot(
            i.Name,
            i.Code,
            i.Quantity,
            i.Price ?? 0,
            i.Weight > 0 ? i.Weight : fallbackWeight,
            i.Length ?? payload.Dimensions.Length,
            i.Width ?? payload.Dimensions.Width,
            i.Height ?? payload.Dimensions.Height)).ToList();

        var paymentTypeId = payload.PaymentTypeId ?? 1;
        var codAmount = payload.CodAmount ?? 0m;

        var shipping = new ShippingOrder
        {
            OrderId = payload.OrderId,
            UserId = payload.UserId,
            ClientOrderCode = payload.ClientOrderCode,
            Status = ShippingStatus.Draft,
            ServiceId = payload.ServiceId,
            ServiceTypeId = payload.ServiceTypeId,
            PaymentTypeId = paymentTypeId,
            CodAmount = codAmount,
            InsuranceValue = payload.InsuranceValue ?? 0m,
            Weight = payload.Dimensions.Weight,
            Length = payload.Dimensions.Length,
            Width = payload.Dimensions.Width,
            Height = payload.Dimensions.Height,
            Note = payload.Note,
            RequiredNote = payload.RequiredNote,
            ToAddress = new ShippingAddressSnapshot(
                payload.ToAddress.Name,
                payload.ToAddress.Phone,
                payload.ToAddress.Address,
                payload.ToAddress.DistrictId,
                payload.ToAddress.WardCode,
                payload.ToAddress.WardName,
                payload.ToAddress.DistrictName,
                payload.ToAddress.ProvinceName),
            Items = items
        };

        await _repo.CreateAsync(shipping, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created shipping draft for order {OrderId}", shipping.OrderId);

        return _mapper.Map<ShippingOrderResponseDto>(shipping);
    }
}
