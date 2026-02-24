using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.DTOs.Response;
using Modules.Payment.Domain;

namespace Modules.Payment.Application.Commands;

public sealed record CreatePaymentCommand(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    PaymentMethod Method
) : IRequest<CreatePaymentResponseDto>;

public sealed class CreatePaymentHandler(IPaymentRepository repo, IPaymentDbContext db) : IRequestHandler<CreatePaymentCommand, CreatePaymentResponseDto>
{
    private readonly IPaymentRepository _repo = repo;
    private readonly IPaymentDbContext _db = db;

    public async Task<CreatePaymentResponseDto> Handle(CreatePaymentCommand request, CancellationToken ct)
    {
        var existingPayment = await _repo.GetByOrderIdAsync(request.OrderId, ct);
        if (existingPayment != null)
            throw new InvalidOperationException("Payment already exists for this order");

        var payment = new Domain.Payment
        {
            Code = await GeneratePaymentCodeAsync(ct),
            OrderId = request.OrderId,
            UserId = request.UserId,
            Amount = request.Amount,
            Method = PaymentMethod.COD,
            Status = PaymentStatus.Pending
        };

        await _repo.CreateAsync(payment, ct);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Payment creation failed due to database error.");
        }

        return new CreatePaymentResponseDto(payment.Id, payment.Code, payment.Status);
    }

    private async Task<string> GeneratePaymentCodeAsync(CancellationToken ct)
    {
        string code;
        int attempt = 0;
        const int maxAttempts = 5;

        do
        {
            code = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
            attempt++;

            if (attempt >= maxAttempts)
                throw new InvalidOperationException("Failed to generate unique payment code after multiple attempts.");

        } while (await _repo.IsPaymentCodeExistsAsync(code, ct));

        return code;
    }
}
