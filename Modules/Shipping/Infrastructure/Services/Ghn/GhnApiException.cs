namespace Modules.Shipping.Infrastructure.Services.Ghn;

public sealed class GhnApiException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
