using Microsoft.EntityFrameworkCore;
using Modules.User.Application;
using Modules.User.Application.Interfaces.Repositories;
using Modules.User.Infrastructure.Persistence;
using Modules.User.Infrastructure.Repositories;

namespace Modules.User;

public static class UserModule
{
    public static IServiceCollection AddUserModule(this IServiceCollection services, IConfiguration configuration)
    {
        var conn =
            Environment.GetEnvironmentVariable("USER_DB_CONNECTION")
            ?? configuration.GetConnectionString("UserDb");

        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException("User DB connection is not configured. Set USER_DB_CONNECTION or ConnectionStrings:UserDb.");

        services.AddDbContext<UserDbContext>(options =>
        {
            options.UseNpgsql(conn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(UserDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IUserDbContext>(sp => sp.GetRequiredService<UserDbContext>());

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(UserApplicationMarker).Assembly));

        services.AddAutoMapper(typeof(UserApplicationMarker).Assembly);

        return services;
    }
}
