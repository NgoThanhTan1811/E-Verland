using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

var envPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));
Env.Load(envPath);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();



builder.Services.AddTransient<ApiExceptionExtension>();

// JWT Configuration
var jwtKey = builder.Configuration["JWT_KEY"];
var jwtIssuer = builder.Configuration["JWT_ISSUER"];
var jwtAudience = builder.Configuration["JWT_AUDIENCE"];

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("JWT Key is not configured.");

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
    });

// Rate Limiting 
builder.Services.AddCustomRateLimiting();
// Authorization 
builder.Services.AddCustomAuthorization();
// OAuth Google
builder.AddGoogleOAuth();
builder.AddSwagger();



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

builder.Services.Configure<RouteOptions>(o =>
{
    o.LowercaseUrls = true;
    o.LowercaseQueryStrings = true;
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{

}
app.MapGet("/swagger-download/v1", async (HttpContext context) =>
{
    var url = $"{context.Request.Scheme}://{context.Request.Host}/swagger/v1/swagger.json";

    using var httpClient = new HttpClient();
    var json = await httpClient.GetStringAsync(url);

    return Results.File(
        System.Text.Encoding.UTF8.GetBytes(json),
        "application/json",
        "swagger-v1.json");
});
app.UseSwagger(options =>
{
    options.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI();

app.UseMiddleware<ApiExceptionExtension>();

// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();


app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.Run();
