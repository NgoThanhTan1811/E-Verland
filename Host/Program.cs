using EVerland.Extentions;
using Modules.User;
using Modules.Product;
using Modules.Cart;
using Modules.Order;
using Modules.Payment;
using Modules.Media;
using Modules.Redis.Infrastructure;
using Modules.Auth.Infrastructure.Persistence;
using Modules.Chat.Api.Hubs;
using Modules.Chat.Infrastructure.Persistence;
using Modules.Notification.Infrastructure;
using Modules.Dashboard;
using DotNetEnv;
using Infra.AWS;
using Infra.AWS.XRay;



var builder = WebApplication.CreateBuilder(args);

var envPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));
Env.Load(envPath);
builder.AddLocalFileLogging();

var xrayOptions = builder.Configuration.GetSection(XRayOptions.SectionName).Get<XRayOptions>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();



builder.Services.AddTransient<ApiExceptionExtension>();

// Rate Limiting
builder.Services.AddCustomRateLimiting();
// Authentication
builder.Services.AddCustomJwtAuthentication(builder.Configuration);
// Authorization 
builder.Services.AddCustomAuthorization();
// CORS 
builder.Services.AddCustomCors(builder.Configuration);
// OAuth Google
builder.AddGoogleOAuth();
builder.AddSwagger();



// AWS Infrastructure (must be registered before modules)
builder.Services.AddAWSInfrastructure(builder.Configuration);

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


app.UseCors("AllowCredentials");

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.Run();
