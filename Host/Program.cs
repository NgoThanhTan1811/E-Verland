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



var builder = WebApplication.CreateBuilder(args);

// var envPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));
// Env.Load(envPath);
builder.AddLocalFileLogging();

var xrayOptions = builder.Configuration.GetSection(XRayOptions.SectionName).Get<XRayOptions>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

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

app.UseCustomSwagger();

// Global Exception Handling
app.UseMiddleware<ApiExceptionMiddleware>();
// TraceId Middleware for logging and correlation
app.UseMiddleware<TraceIdMiddleware>();

// app.UseHttpsRedirection();
app.UseAuthentication();
if (xrayOptions?.Enabled == true)
{
    app.UseXRay("E-Verland");
}
app.UseRateLimiter();
app.UseAuthorization();


app.UseCors("AllowCredentials");

// Prometheus scrape endpoint
app.MapMetrics();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.Run();
