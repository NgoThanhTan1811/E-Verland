using AutoMapper;
using MediatR;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Application.DTOs.Response;

namespace Modules.Payment.Application.Queries;

public sealed record GetPaymentByOrderIdQuery(Guid OrderId) : IRequest<PaymentResponseDto?>;

public sealed class GetPaymentByOrderIdHandler(IPaymentRepository repo, IMapper mapper) : IRequestHandler<GetPaymentByOrderIdQuery, PaymentResponseDto?>
{
    private readonly IPaymentRepository _repo = repo;
    private readonly IMapper _mapper = mapper;

    public async Task<PaymentResponseDto?> Handle(GetPaymentByOrderIdQuery request, CancellationToken ct)
    {
        var payment = await _repo.GetByOrderIdAsync(request.OrderId, ct);

        return payment == null ? null : _mapper.Map<PaymentResponseDto>(payment);
    }
}
