using Modules.Payment.Application;
using Modules.Payment.Infrastructure.Repositories;
using Modules.Payment.Application.Contracts;
using Modules.Payment.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Modules.Payment.Infrastructure.Persistence;
using SharedKernel.Persistence;

namespace Modules.Payment;

public static class PaymentModuleExtension
{
    public static IServiceCollection AddPaymentModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var conn = configuration.GetConnectionString("PaymentDb")
                ?? Environment.GetEnvironmentVariable("PaymentDb")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:PaymentDb");

        services.AddDbContext<PaymentDbContext>(options =>
            options.ConfigureNpgsql(conn, typeof(PaymentDbContext).Assembly.GetName().Name!));

        // Add Repositories
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // Add HTTP Clients
        services.AddHttpClient<ISePayClient, SePayClient>();

        // Add Application Services
        services.AddScoped<IPaymentDbContext>(provider => provider.GetRequiredService<PaymentDbContext>());
        services.AddScoped<IWebhookIdempotencyService, WebhookIdempotencyService>();
        services.AddScoped<ILedgerService, LedgerService>();
        services.AddScoped<ISellerBalanceService, SellerBalanceService>();
        services.AddHostedService<SellerPayoutBackgroundService>();

        // Add MediatR
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(PaymentApplicationMarker).Assembly));

        return services;
    }
}
