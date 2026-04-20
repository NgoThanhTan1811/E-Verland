using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using EVerland.Extentions;
using BFF;
using Modules.User;
using Modules.Product;
using Modules.Cart;
using Modules.Order;
using Modules.Payment;
using Modules.Redis.Infrastructure;
using Modules.Auth.Infrastructure.Persistence;
using Modules.Chat.Api.Hubs;
using Modules.Chat.Infrastructure.Persistence;
using Modules.Notification.Infrastructure;
using DotNetEnv;
using Infra.AWS;
using Infra.AWS.XRay;

var builder = WebApplication.CreateBuilder(args);

var envPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));
Env.Load(envPath);

var xrayOptions = builder.Configuration.GetSection(XRayOptions.SectionName).Get<XRayOptions>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();



builder.Services.AddTransient<ApiExceptionExtension>();

// JWT Configuration with validation
var jwtKey = builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "e-verland-platform";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "e-verland-platform";

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("JWT Key is not configured. Set 'Jwt:Key' in appsettings.json or JWT_KEY environment variable. Must be at least 32 characters.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(jwtKey);

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

        // Read JWT from HttpOnly cookie instead of Authorization header
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

// Rate Limiting
builder.Services.AddCustomRateLimiting();
// Authorization (Requirement 6: Role-based policies)
builder.Services.AddCustomAuthorization();
// CORS (Requirement 8: Configure with credentials support)
builder.Services.AddCustomCors(builder.Configuration);
// OAuth Google
builder.AddGoogleOAuth();
builder.AddSwagger();



// AWS Infrastructure (must be registered before modules)
builder.Services.AddAWSInfrastructure(builder.Configuration);

// Add Modules
builder.Services.AddRedisModule(builder.Configuration);
builder.Services.AddUserModule(builder.Configuration);
builder.Services.AddProductModule(builder.Configuration);
builder.Services.AddCartModule(builder.Configuration);
builder.Services.AddOrderModule(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddPaymentModule(builder.Configuration);
builder.Services.AddChatModule(builder.Configuration);
builder.Services.AddNotificationModule(builder.Configuration);

// Add BFF Module (Requirement 4: BFF Gateway with role-based facades)
builder.Services.AddBffModule(builder.Configuration);

builder.Services.Configure<RouteOptions>(o =>
{
    o.LowercaseUrls = true;
    o.LowercaseQueryStrings = true;
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{

}
app.UseCustomSwagger();

app.UseMiddleware<ApiExceptionExtension>();

// app.UseHttpsRedirection();
app.UseAuthentication();
if (xrayOptions?.Enabled == true)
{
    app.UseXRay("E-Verland");
}
app.UseRateLimiter();
app.UseAuthorization();

// Requirement 8.9: Apply CORS policy with credentials support
app.UseCors("AllowCredentials");

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.Run();
