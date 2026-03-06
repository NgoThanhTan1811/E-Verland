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
using Modules.Chat.Infrastructure.Persistence;
using Modules.Notification.Infrastructure;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

var envPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));
Env.Load(envPath);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger Configuration
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type =>
    {
        // Helper: prefix theo module để tránh trùng enum/class cùng tên
        static string PrefixByModule(Type t, string baseName)
        {
            var ns = t.Namespace ?? "";
            if (ns.StartsWith("Modules.Payment")) return "Payment_" + baseName;
            if (ns.StartsWith("Modules.Order")) return "Order_" + baseName;
            if (ns.StartsWith("Modules.User")) return "User_" + baseName;
            if (ns.StartsWith("SharedKernel")) return "Shared_" + baseName;
            return baseName;
        }

        if (!type.IsGenericType)
        {
            // Non-generic: dùng Name + prefix module
            return PrefixByModule(type, type.Name);
        }

        // Generic: PageResult_OrderOverviewResponseDto (có prefix module của generic type)
        var genericName = type.GetGenericTypeDefinition().Name.Split('`')[0];
        genericName = PrefixByModule(type, genericName);

        var args = string.Join("_",
            type.GetGenericArguments().Select(a =>
            {
                // mỗi generic arg cũng nên unique (lỡ arg trùng tên giữa module)
                var argBase = a.IsGenericType
                    ? a.GetGenericTypeDefinition().Name.Split('`')[0]
                    : a.Name;

                return PrefixByModule(a, argBase);
            }));

        return $"{genericName}_{args}";
    });

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
var jwtKey = builder.Configuration["JWT_KEY"] ?? builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? builder.Configuration["Jwt:Audience"];

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
builder.Services.AddChatModule(builder.Configuration);
builder.Services.AddNotificationModule(builder.Configuration);


var app = builder.Build();


if (app.Environment.IsDevelopment())
{

}
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ApiExceptionMiddleware>();

// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();


app.MapControllers();
app.Run();
