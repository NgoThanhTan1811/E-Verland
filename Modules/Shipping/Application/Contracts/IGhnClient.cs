using Modules.Shipping.Application.DTOs.External;

namespace Modules.Shipping.Application.Contracts;

public interface IGhnClient
{
    Task<GhnApiResponse<GhnCreateOrderResponse>> CreateOrderAsync(GhnCreateOrderRequest request, CancellationToken ct = default);

    Task<GhnApiResponse<GhnFeeResponse>> CalculateFeeAsync(GhnFeeRequest request, CancellationToken ct = default);

    Task<GhnApiResponse<List<GhnServiceResponse>>> GetAvailableServicesAsync(GhnServiceRequest request, CancellationToken ct = default);

    Task<GhnApiResponse<List<GhnCancelResult>>> CancelOrderAsync(GhnCancelRequest request, CancellationToken ct = default);

    Task<GhnApiResponse<GhnOrderInfoResponse>> GetOrderInfoAsync(string orderCode, CancellationToken ct = default);
}
