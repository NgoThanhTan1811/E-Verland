using System.Diagnostics;
using Amazon.XRay.Recorder.Core;
using AutoMapper;
using Infra.AWS.CloudWatch;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Order.Application.Contracts;
using Modules.Order.Application.DTOs.Request;
using Modules.Order.Application.DTOs.Response;
using Modules.Order.Domain;

namespace Modules.Order.Application.Commands;

public sealed record CreateOrderCommand(
    Guid UserId,
    ReceiverRequestDto Receiver,
    PaymentMethod PaymentMethod,
    decimal? VoucherCode,
    List<CreateOrderItemRequestDto> Items
) : IRequest<CreateOrderResponseDto>;

public sealed class CreateOrderHandler(
    IOrderRepository repo,
    IOrderDbContext db,
    IProductService productService,
    IMapper mapper,
    ICloudWatchService cloudWatch,
    ILogger<CreateOrderHandler> logger)
    : IRequestHandler<CreateOrderCommand, CreateOrderResponseDto>
{
    private readonly IOrderRepository _repo = repo;
    private readonly IOrderDbContext _db = db;
    private readonly IProductService _productService = productService;
    private readonly IMapper _mapper = mapper;

    public async Task<CreateOrderResponseDto> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("Order must have at least one item");

        var sw = Stopwatch.StartNew();
        try
        {
            var receiverSnapshot = ReceiverSnapshot.Create(
                request.Receiver.Name,
                request.Receiver.Phone,
                request.Receiver.Address
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
            logger.LogInformation("Order created. {OrderId} {UserId} {LatencyMs}", order.Id, request.UserId, sw.ElapsedMilliseconds);
            await cloudWatch.PutMetricAsync("order.created", 1, "Count", ct: ct);
            await cloudWatch.PutMetricAsync("order.latency_ms", sw.ElapsedMilliseconds, "Milliseconds", ct: ct);

            return new CreateOrderResponseDto(order.Id, order.Code);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Order creation failed. {UserId} {LatencyMs}", request.UserId, sw.ElapsedMilliseconds);
            await cloudWatch.PutMetricAsync("order.failed", 1, "Count", ct: ct);
            throw;
        }
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
}
