using Microsoft.EntityFrameworkCore;
using Modules.Auth.Application.Services;
using Modules.Auth.Infrastructure.Services;
using SharedKernel.Persistence;

namespace Modules.Auth.Infrastructure.Persistence
{
    public static class AuthModule
    {
        public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
        {
            var conn = configuration["ConnectionStrings:AuthDb"] 
                    ?? throw new InvalidOperationException("Missing connection string for AuthDb.");

            // Register DbContext
            services.AddDbContext<AuthDbContext>(options =>
            {
                options.ConfigureNpgsql(conn, typeof(AuthDbContext).Assembly.GetName().Name!);
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
