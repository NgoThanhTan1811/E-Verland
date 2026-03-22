namespace Modules.Payment.Application.Helpers;

public static class PaymentCodeHelper
{
    public static string Generate()
        => $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
}
