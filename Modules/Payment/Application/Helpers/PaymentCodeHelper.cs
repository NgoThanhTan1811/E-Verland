namespace Modules.Payment.Application.Helpers;

public static class PaymentCodeHelper
{
    public static string Generate()
        => $"PAY{Random.Shared.Next(10000000, 999999999)}";
}
