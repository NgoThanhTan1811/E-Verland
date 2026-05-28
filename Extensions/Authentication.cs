using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

public static class AuthenticationExtension
{
    public static IServiceCollection AddCustomJwtAuthentication( this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"]
                        ?? throw new InvalidOperationException("Missing Jwt:Key.");

        var jwtIssuer = configuration["Jwt:Issuer"]
                        ?? throw new InvalidOperationException("Missing Jwt:Issuer.");

        var jwtAudience = configuration["Jwt:Audience"]
                          ?? throw new InvalidOperationException("Missing Jwt:Audience.");

        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            throw new InvalidOperationException("JWT Key must be at least 32 characters.");

        var key = Encoding.UTF8.GetBytes(jwtKey);
        Console.WriteLine($"DEBUG_LOG: Giá trị Jwt:Issuer hiện tại là: '{jwtIssuer}'");
        Console.WriteLine($"DEBUG_LOG: Giá trị Jwt:Audience hiện tại là: '{jwtAudience}'");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // dev only

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ClockSkew = TimeSpan.Zero
                };

                // Allow JWT in cookies for WebSocket connections
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.TryGetValue("access_token", out var token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}