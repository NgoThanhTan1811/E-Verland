using Modules.Payment.Application;
using Modules.Payment.Infrastructure.Repositories;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Modules.Payment.Infrastructure.Persistence;
using SharedKernel.Persistence;
using System.Net.Http.Headers;

namespace Modules.Payment;

public static class PaymentModuleExtension
{
    public static IServiceCollection AddPaymentModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var conn = configuration["ConnectionStrings:PaymentDb"]
                ?? throw new InvalidOperationException("Missing ConnectionStrings:PaymentDb");

        services.AddDbContext<PaymentDbContext>(options =>
            options.ConfigureNpgsql(conn, typeof(PaymentDbContext).Assembly.GetName().Name!));

        // Add Repositories
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // Add HTTP Clients
        services.AddHttpClient<ISePayClient, SePayClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var apiKey = config[$"{SePayOptions.SectionName}:ApiKey"]
                    ?? config["SePay:APIKey"]
                    ?? Environment.GetEnvironmentVariable("SEPAY_API_KEY")
                    ?? Environment.GetEnvironmentVariable("SEPAY_API")
                    ?? throw new InvalidOperationException("Missing Payment:SePay:ApiKey (or SEPAY_API_KEY environment variable).");

                if (apiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    apiKey = apiKey[7..].Trim();
                }

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
            });

        // Add Application Services
        services.AddScoped<IPaymentDbContext>(provider => provider.GetRequiredService<PaymentDbContext>());
        services.AddScoped<IWebhookIdempotencyService, WebhookIdempotencyService>();
        services.AddScoped<ILedgerService, LedgerService>();
        services.AddScoped<ISellerBalanceService, SellerBalanceService>();
        services.AddHostedService<SellerPayoutBackgroundService>();

        // Add MediatR
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(PaymentApplicationMarker).Assembly));

        // Add AutoMapper
        services.AddAutoMapper(typeof(PaymentApplicationMarker).Assembly);

        return services;
    }
}
