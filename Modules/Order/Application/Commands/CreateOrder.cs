using System.Diagnostics;
using System.Linq;
using Amazon.XRay.Recorder.Core;
using Infra.AWS.CloudWatch;
using Infra.AWS.EventBridge;
using Infra.AWS.SNS;
using Infra.AWS.SQS;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Order.Application.Contracts;
using Modules.Order.Application.DTOs.Request;
using Modules.Order.Application.DTOs.Response;
using Modules.Order.Domain;
using Modules.User.Application.Interfaces.Repositories;
using SharedKernel.Events;

namespace Modules.Order.Application.Commands;

public sealed record CreateOrderCommand(
    Guid UserId,
    Guid? ShippingAddressId,
    ShippingAddressRequestDto? ShippingAddress,
    ReceiverRequestDto Receiver,
    int Weight,
    int Length,
    int Width,
    int Height,
    int? ServiceId,
    int? ServiceTypeId,
    decimal? InsuranceValue,
    string? Note,
    string? RequiredNote,
    PaymentMethod PaymentMethod,
    decimal? VoucherCode,
    List<CreateOrderItemRequestDto> Items
) : IRequest<CreateOrderResponseDto>;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponseDto>
{
    private readonly IOrderRepository _repo;
    private readonly IOrderDbContext _db;
    private readonly IProductService _productService;
    private readonly IProfileRepository _profileRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly ICloudWatchService _cloudWatch;
    private readonly ILogger<CreateOrderHandler> _logger;
    private readonly ISQSService? _sqsService;
    private readonly ISNSService? _snsService;
    private readonly IEventBridgeService? _eventBridgeService;
    private readonly IConfiguration? _configuration;

    public CreateOrderHandler(
        IOrderRepository repo,
        IOrderDbContext db,
        IProductService productService,
        IProfileRepository profileRepository,
        IAddressRepository addressRepository,
        ICloudWatchService cloudWatch,
        ILogger<CreateOrderHandler> logger,
        ISQSService? sqsService = null,
        ISNSService? snsService = null,
        IEventBridgeService? eventBridgeService = null,
        IConfiguration? configuration = null)
    {
        _repo = repo;
        _db = db;
        _productService = productService;
        _profileRepository = profileRepository;
        _addressRepository = addressRepository;
        _cloudWatch = cloudWatch;
        _logger = logger;
        _sqsService = sqsService;
        _snsService = snsService;
        _eventBridgeService = eventBridgeService;
        _configuration = configuration;
    }

    public async Task<CreateOrderResponseDto> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("Order must have at least one item");

        if (request.Weight <= 0 || request.Length <= 0 || request.Width <= 0 || request.Height <= 0)
            throw new ArgumentException("Shipping dimensions must be greater than 0");

        var shippingAddress = request.ShippingAddress;
        if (request.ShippingAddressId.HasValue)
        {
            shippingAddress = await ResolveShippingAddressAsync(request.UserId, request.ShippingAddressId.Value, ct);
        }

        if (shippingAddress is null)
            throw new ArgumentException("Shipping address is required");

        if (shippingAddress.DistrictId <= 0 || string.IsNullOrWhiteSpace(shippingAddress.WardCode))
            throw new ArgumentException("Shipping address is missing district/ward codes");

        if (string.IsNullOrWhiteSpace(shippingAddress.Address))
            throw new ArgumentException("Shipping address detail is required");

        var sw = Stopwatch.StartNew();
        try
        {
            var receiverSnapshot = ReceiverSnapshot.Create(
                request.Receiver.Name,
                request.Receiver.Phone,
                shippingAddress.Address
            );

            var order = new Domain.Order
            {
                UserId = request.UserId,
                Code = await GenerateOrderCodeAsync(ct),
                Receiver = receiverSnapshot,
                PaymentMethod = request.PaymentMethod,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                Items = [],
                Discount = request.VoucherCode
            };

            decimal totalPrice = 0;
            foreach (var itemDto in request.Items)
            {
                var product = await _productService.GetProductAsync(itemDto.ProductId, ct);
                if (product == null)
                    throw new ArgumentException($"Product with ID {itemDto.ProductId} not found");

                var orderItem = new OrderItem(
                    itemDto.ProductId,
                    itemDto.SkuId,
                    product.Name,
                    (int)product.Price,
                    itemDto.Quantity
                );

                order.Items.Add(orderItem);
                totalPrice += orderItem.TotalPrice;
            }

            order.TotalPrice = totalPrice;

            AWSXRayRecorder.Instance.BeginSubsegment("Order.DB");
            try
            {
                await _repo.CreateAsync(order, ct);
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                AWSXRayRecorder.Instance.AddException(ex);
                if (ex is DbUpdateException)
                    throw new InvalidOperationException("Order creation failed due to database error.", ex);
                throw;
            }
            finally
            {
                AWSXRayRecorder.Instance.EndSubsegment();
            }

            sw.Stop();
            _logger.LogInformation("Order created. {OrderId} {UserId} {LatencyMs}", order.Id, request.UserId, sw.ElapsedMilliseconds);
            await _cloudWatch.PutMetricAsync("order.created", 1, "Count", ct: ct);
            await _cloudWatch.PutMetricAsync("order.latency_ms", sw.ElapsedMilliseconds, "Milliseconds", ct: ct);

            await PublishShippingDraftRequestedAsync(order, request, shippingAddress, ct);
            await PublishOrderEventAsync(order, "OrderCreated", ct);

            return new CreateOrderResponseDto(order.Id, order.Code);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Order creation failed. {UserId} {LatencyMs}", request.UserId, sw.ElapsedMilliseconds);
            await _cloudWatch.PutMetricAsync("order.failed", 1, "Count", ct: ct);
            throw;
        }
    }

    private async Task<ShippingAddressRequestDto> ResolveShippingAddressAsync(Guid userId, Guid addressId, CancellationToken ct)
    {
        var profile = await _profileRepository.GetByAccountIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Profile not found.");

        var address = await _addressRepository.GetByIdForProfileAsync(addressId, profile.Id, ct)
            ?? throw new KeyNotFoundException("Shipping address not found.");

        var detail = string.Join(", ", new[]
        {
            address.Detail,
            address.Street,
            address.Ward,
            address.District,
            address.Province
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return new ShippingAddressRequestDto(
            detail,
            address.DistrictId ?? 0,
            address.WardCode ?? string.Empty,
            address.Ward,
            address.District,
            address.Province
        );
    }

    private async Task<string> GenerateOrderCodeAsync(CancellationToken ct)
    {
        string code;
        int attempt = 0;
        const int maxAttempts = 5;

        do
        {
            code = $"ORD-{DateTime.UtcNow:ddMMyyyy}-{Random.Shared.Next(1000, 9999)}";
            attempt++;

            if (attempt >= maxAttempts)
                throw new InvalidOperationException("Failed to generate unique order code");

        } while (await _repo.CodeExistsAsync(code, ct));

        return code;
    }

    private async Task PublishShippingDraftRequestedAsync(
        Domain.Order order,
        CreateOrderCommand request,
        ShippingAddressRequestDto shippingAddress,
        CancellationToken ct)
    {
        if (_configuration == null || _sqsService == null)
        {
            return;
        }

        var queueUrl = _configuration["AWS:SQS:ShippingDraftQueueUrl"]
            ?? _configuration["SQS:ShippingDraftQueueUrl"]
            ?? Environment.GetEnvironmentVariable("AWS_SQS_SHIPPING_DRAFT_QUEUE_URL");

        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            return;
        }

        var totalUnits = Math.Max(1, order.Items.Sum(i => i.Quantity));
        var itemWeight = Math.Max(1, request.Weight / totalUnits);

        var items = order.Items.Select(i => new ShippingDraftItem(
            i.ProductName,
            i.SkuId.ToString(),
            i.Quantity,
            (int)i.UnitPrice,
            itemWeight,
            request.Length,
            request.Width,
            request.Height)).ToList();

        var paymentTypeId = order.PaymentMethod == PaymentMethod.COD ? 2 : 1;
        var codAmount = order.PaymentMethod == PaymentMethod.COD ? order.GrandTotal : 0m;

        var evt = new ShippingDraftRequested(
            order.Id,
            order.Code,
            order.UserId,
            request.Receiver.Name,
            request.Receiver.Phone,
            shippingAddress.Address,
            shippingAddress.DistrictId,
            shippingAddress.WardCode,
            shippingAddress.WardName,
            shippingAddress.DistrictName,
            shippingAddress.ProvinceName,
            request.Weight,
            request.Length,
            request.Width,
            request.Height,
            request.ServiceId,
            request.ServiceTypeId,
            paymentTypeId,
            codAmount,
            request.InsuranceValue ?? order.GrandTotal,
            request.Note,
            request.RequiredNote,
            items
        );

        try
        {
            await _sqsService.SendMessageAsync(queueUrl, evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish shipping draft request for order {OrderId}", order.Id);
        }
    }

    private async Task PublishOrderEventAsync(Domain.Order order, string eventType, CancellationToken ct)
    {
        if (_configuration == null)
        {
            return;
        }

        var payload = new
        {
            orderId = order.Id,
            orderCode = order.Code,
            userId = order.UserId,
            status = order.Status.ToString(),
            paymentStatus = order.PaymentStatus.ToString(),
            totalPrice = order.TotalPrice,
            createdAtUtc = order.CreatedAt,
            eventType
        };

        try
        {
            var queueUrl = _configuration["AWS:SQS:OrderEventsQueueUrl"]
                ?? _configuration["SQS:OrderEventsQueueUrl"]
                ?? Environment.GetEnvironmentVariable("AWS_SQS_ORDER_EVENTS_QUEUE_URL");
            if (_sqsService != null && !string.IsNullOrWhiteSpace(queueUrl))
            {
                await _sqsService.SendMessageAsync(queueUrl, payload, ct);
            }

            var topicArn = _configuration["AWS:SNS:OrderEventsTopicArn"]
                ?? _configuration["SNS:OrderEventsTopicArn"]
                ?? Environment.GetEnvironmentVariable("AWS_SNS_ORDER_EVENTS_TOPIC_ARN");
            if (_snsService != null && !string.IsNullOrWhiteSpace(topicArn))
            {
                await _snsService.PublishAsync(topicArn, payload, eventType, ct);
            }

            if (_eventBridgeService != null)
            {
                var source = _configuration["AWS:EventBridge:OrderEventSource"] ?? "e-verland.orders";
                await _eventBridgeService.PutEventAsync(source, eventType, payload, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish order event {EventType} for order {OrderId}", eventType, order.Id);
        }
    }
}
