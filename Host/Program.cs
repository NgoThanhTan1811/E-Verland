using EVerland.Extentions;
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
using Modules.Dashboard;
using Modules.Shipping;
using DotNetEnv;
using Infra.AWS;
using Infra.AWS.XRay;
using Prometheus;
using Modules.Auth.Infrastructure.Services;
using Modules.Media.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);

// var envPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));
// Env.Load(envPath);
builder.AddLocalFileLogging();
builder.Configuration.AddEnvironmentVariables();

var xrayOptions = builder.Configuration.GetSection(XRayOptions.SectionName).Get<XRayOptions>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Thêm dòng này để công cụ tuần tự hóa JSON tự cắt đứt khi phát hiện vòng lặp vô hạn
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

        // Giữ nguyên các cấu hình đặt tên camelCase nếu có của bạn
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Health Checks
builder.Services.AddHealthChecks();
// TraceId / Request context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SharedKernel.Context.IRequestContext, SharedKernel.Context.HttpRequestContext>();


// Rate Limiting
builder.Services.AddCustomRateLimiting();
// Authentication
builder.Services.AddCustomJwtAuthentication(builder.Configuration);
// Authorization 
builder.Services.AddCustomAuthorization();
// CORS 
builder.Services.AddCustomCors(builder.Configuration);
// OAuth Google
// builder.AddGoogleOAuth();
builder.AddSwagger();



// AWS Infrastructure 
builder.Services.AddAWSInfrastructure(builder.Configuration);
// SmtpOptions configuration for EmailService
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Email:Smtp"));

// Add Modules
builder.Services.AddRedisModule(builder.Configuration);
builder.Services.AddUserModule(builder.Configuration);
builder.Services.AddMediaModule(builder.Configuration);
builder.Services.AddProductModule(builder.Configuration);
builder.Services.AddCartModule(builder.Configuration);
builder.Services.AddOrderModule(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddPaymentModule(builder.Configuration);
builder.Services.AddChatModule(builder.Configuration);
builder.Services.AddNotificationModule(builder.Configuration);
builder.Services.AddDashboardModule(builder.Configuration);
builder.Services.AddShippingModule(builder.Configuration);

builder.Services.Configure<RouteOptions>(o =>
{
    o.LowercaseUrls = true;
    o.LowercaseQueryStrings = true;
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var dbContextTypes = new[]
    {
        typeof(Modules.User.Infrastructure.Persistence.UserDbContext),
        typeof(Modules.Auth.Infrastructure.Persistence.AuthDbContext),
        typeof(Modules.Product.Infrastructure.Persistence.ProductDbContext),
        typeof(Modules.Order.Infrastructure.Persistence.OrderDbContext),
        typeof(Modules.Payment.Infrastructure.Persistence.PaymentDbContext),
        typeof(Modules.Cart.Infrastructure.Persistence.CartDbContext),
        typeof(Modules.Media.Infrastructure.Persistence.MediaDbContext),
        typeof(Modules.Shipping.Infrastructure.Persistence.ShippingDbContext),
    };

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    foreach (var dbContextType in dbContextTypes)
    {
        try
        {
            var db = (Microsoft.EntityFrameworkCore.DbContext)scope.ServiceProvider.GetRequiredService(dbContextType);
            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                logger.LogInformation("Applying {Count} pending migration(s) for {DbContext}: {Migrations}",
                    pending.Count, dbContextType.Name, string.Join(", ", pending));
                await db.Database.MigrateAsync();
                logger.LogInformation("Migrations applied for {DbContext}", dbContextType.Name);
            }
        }
        catch (Exception ex)
        {
            var logger2 = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger2.LogError(ex, "Migration failed for {DbContext} — app will continue but may be unstable", dbContextType.Name);
        }
    }
}

app.UseCustomSwagger();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                     | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
    KnownNetworks = { },
    KnownProxies  = { },
});
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/payment/webhook", StringComparison.OrdinalIgnoreCase))
        context.Request.EnableBuffering();
    await next();
});

// Global Exception Handling
app.UseMiddleware<ApiExceptionMiddleware>();
if (xrayOptions?.Enabled == true)
{
    app.UseXRay("E-Verland");
}
// TraceId Middleware for logging and correlation
app.UseMiddleware<TraceIdMiddleware>();

app.UseCors("AllowCredentials");
app.UseMiddleware<AutoRefreshTokenMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();



app.MapHealthChecks("/health");
// Prometheus scrape endpoint
app.MapMetrics();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.Run();
