using Microsoft.EntityFrameworkCore;
using Modules.User.Application;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Infrastructure.Persistence;
using Modules.User.Infrastructure.Repositories;
using SharedKernel.Persistence;

namespace Modules.User;

public static class UserModule
{
    public static IServiceCollection AddUserModule(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = configuration["ConnectionStrings:UserDb"]
             ?? throw new InvalidOperationException("Missing ConnectionStrings:UserDb");

        services.AddDbContext<UserDbContext>(options =>
        {
            options.ConfigureNpgsql(conn, typeof(UserDbContext).Assembly.GetName().Name!);
        });

        services.AddScoped<IUserDbContext>(sp => sp.GetRequiredService<UserDbContext>());

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(UserApplicationMarker).Assembly));

        return services;
    }
}
