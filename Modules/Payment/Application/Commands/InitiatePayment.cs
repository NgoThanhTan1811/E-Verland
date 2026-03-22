using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.Helpers;
using Modules.Payment.Domain;
using Modules.Product.Application.Contracts;

namespace Modules.Payment.Application.Commands;

public sealed record OrderItemDto(Guid SkuId, int Quantity);

public sealed record InitiatePaymentCommand(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    PaymentMethod Method,
    List<OrderItemDto> Items
) : IRequest<InitiatePaymentResponseDto>;

public sealed record InitiatePaymentResponseDto(
    Guid Id,
    string Code,
    PaymentStatus Status,
    string? PaymentUrl
);

public sealed class InitiatePaymentHandler(
    IPaymentRepository repo,
    IPaymentDbContext db,
    IProductReservationService reservationService,
    ISePayClient sePayClient
) : IRequestHandler<InitiatePaymentCommand, InitiatePaymentResponseDto>
{
    public async Task<InitiatePaymentResponseDto> Handle(InitiatePaymentCommand request, CancellationToken ct)
    {
        // Idempotency check
        var existing = await repo.GetByOrderIdAsync(request.OrderId, ct);
        if (existing != null)
            throw new InvalidOperationException("Payment already exists for this order.");

        var payment = new Domain.Payment
        {
            Code = PaymentCodeHelper.Generate(),
            OrderId = request.OrderId,
            UserId = request.UserId,
            Amount = request.Amount,
            Method = request.Method,
            Status = PaymentStatus.Pending
        };

        // Reserve stock before persisting
        await reservationService.ReserveStockAsync(
            payment.Id,
            request.Items.Select(i => (i.SkuId, i.Quantity)),
            ct);

        await repo.CreateAsync(payment, ct);

        // If OnlineBanking, create SePay payment link
        if (request.Method == PaymentMethod.OnlineBanking)
        {
            var paymentUrl = await sePayClient.CreatePaymentLinkAsync(
                payment.Code,
                payment.Amount,
                $"Thanh toan don hang {payment.OrderId}",
                ct);
            payment.PaymentUrl = paymentUrl;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException($"Payment creation failed due to database error: {ex.Message}");
        }

        return new InitiatePaymentResponseDto(payment.Id, payment.Code, payment.Status, payment.PaymentUrl);
    }
}
