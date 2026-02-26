using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using EVerland.Middleware;
using Modules.User;
using Modules.Product;
using Modules.Cart;
using Modules.Order;
using Modules.Payment;
using Modules.Redis.Infrastructure;
using Modules.Auth.Infrastructure.Persistence;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger Configuration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddTransient<ApiExceptionMiddleware>();

// JWT Configuration
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")?.Split('\n', '\r')[0].Trim()
    ?? builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("JWT Key is not configured. Set JWT_KEY env var or Jwt:Key in appsettings.json");

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"];
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"];

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

// Rate Limiting & Authorization Configuration
builder.Services.AddCustomRateLimiting();
builder.Services.AddCustomAuthorization();


// Add Modules
builder.Services.AddRedisModule(builder.Configuration);
builder.Services.AddUserModule(builder.Configuration);
builder.Services.AddProductModule(builder.Configuration);
builder.Services.AddCartModule(builder.Configuration);
builder.Services.AddOrderModule(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddPaymentModule(builder.Configuration);


var app = builder.Build();


if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();


app.MapControllers();
app.Run();
