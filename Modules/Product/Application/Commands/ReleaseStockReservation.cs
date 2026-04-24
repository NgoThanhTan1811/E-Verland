using MediatR;
using Microsoft.Extensions.Logging;
using Modules.Product.Application.Contracts;
using Modules.Product.Infrastructure.Persistence;

namespace Modules.Product.Application.Commands;

/// <summary>
/// Internal command to handle OrderCanceled events from Order Module
/// This command is triggered by the ProductOrderCanceledConsumer
/// </summary>
public sealed record ReleaseStockReservationCommand(
    Guid PaymentId,
    string OrderCode,
    string Source = "order-canceled-event"
) : IRequest<bool>;

/// <summary>
/// Handles releasing stock reservations when an order is canceled
/// </summary>
public sealed class ReleaseStockReservationHandler(
    IProductReservationService reservationService,
    ILogger<ReleaseStockReservationHandler> logger)
    : IRequestHandler<ReleaseStockReservationCommand, bool>
{
    private readonly IProductReservationService _reservationService = reservationService;
    private readonly ILogger<ReleaseStockReservationHandler> _logger = logger;

    public async Task<bool> Handle(ReleaseStockReservationCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Processing stock release for payment {PaymentId} (order {OrderCode}) from {Source}",
                request.PaymentId, request.OrderCode, request.Source);

            await _reservationService.ReleaseReservationAsync(request.PaymentId, ct);

            _logger.LogInformation(
                "Successfully released stock reservations for payment {PaymentId}",
                request.PaymentId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to release stock reservations for payment {PaymentId}",
                request.PaymentId);
            return false;
        }
    }
}
