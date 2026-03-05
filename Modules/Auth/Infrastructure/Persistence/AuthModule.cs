using Microsoft.EntityFrameworkCore;
using Modules.Auth.Application.Services;
using Modules.Auth.Infrastructure.Services;

namespace Modules.Auth.Infrastructure.Persistence
{
    public static class AuthModule
    {
        public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
        {
            var conn = configuration.GetConnectionString("AuthDb")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__AuthDb");
            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("Missing ConnectionStrings__AuthDb");

            // Register DbContext
            services.AddDbContext<AuthDbContext>(options =>
            {
                options.UseNpgsql(conn, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AuthDbContext).Assembly.GetName().Name);
                });
            });

            // Register Services
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();

            // Register Logger
            services.AddLogging();

            return services;
        }
    }
}
