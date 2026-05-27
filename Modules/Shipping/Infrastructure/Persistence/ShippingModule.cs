using Microsoft.EntityFrameworkCore;
using Modules.Shipping.Application;
using Modules.Shipping.Application.Contracts;
using Modules.Shipping.Infrastructure.Consumers;
using Modules.Shipping.Infrastructure.Persistence;
using Modules.Shipping.Infrastructure.Repositories;
using Modules.Shipping.Infrastructure.Services.Ghn;
using SharedKernel.Persistence;

namespace Modules.Shipping;

public static class ShippingModuleExtension
{
    public static IServiceCollection AddShippingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = configuration["ConnectionStrings:ShippingDb"]
            ?? throw new InvalidOperationException("Missing ConnectionStrings:ShippingDb");

        services.AddDbContext<ShippingDbContext>(options =>
            options.ConfigureNpgsql(conn, typeof(ShippingDbContext).Assembly.GetName().Name!));

        services.AddScoped<IShippingRepository, ShippingRepository>();
        services.AddScoped<IShippingDbContext>(provider => provider.GetRequiredService<ShippingDbContext>());

        services.Configure<GhnOptions>(configuration.GetSection(GhnOptions.SectionName));
        services.AddHttpClient<IGhnClient, GhnClient>();

        services.AddHostedService<ShippingRequestConsumer>();

        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(ShippingApplicationMarker).Assembly));

        services.AddAutoMapper(typeof(ShippingApplicationMarker).Assembly);

        return services;
    }
}
