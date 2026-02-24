using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.DTOs.Response;
using Modules.Payment.Domain;

namespace Modules.Payment.Application.Commands;

public sealed record UpdatePaymentStatusCommand(
    Guid PaymentId,
    PaymentStatus Status
) : IRequest<PaymentResponseDto>;

public sealed class UpdatePaymentStatusHandler(IPaymentRepository repo, IPaymentDbContext db, IMapper mapper) : IRequestHandler<UpdatePaymentStatusCommand, PaymentResponseDto>
{
    private readonly IPaymentRepository _repo = repo;
    private readonly IPaymentDbContext _db = db;
    private readonly IMapper _mapper = mapper;

    public async Task<PaymentResponseDto> Handle(UpdatePaymentStatusCommand request, CancellationToken ct)
    {
        var payment = await _repo.GetByIdAsync(request.PaymentId, ct)
            ?? throw new KeyNotFoundException("Payment not found");

        if (payment.Status == PaymentStatus.Success)
            throw new InvalidOperationException("Cannot update status of a successful payment");

        if (payment.Status == PaymentStatus.Refunded)
            throw new InvalidOperationException("Cannot update status of a refunded payment");

        payment.Status = request.Status;

        await _repo.UpdateAsync(payment, ct);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Payment status update failed due to database error.");
        }

        return _mapper.Map<PaymentResponseDto>(payment);
    }
}
