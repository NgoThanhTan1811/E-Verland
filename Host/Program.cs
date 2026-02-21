using DotNetEnv;
using Modules.User;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using System.Threading.RateLimiting;
using Modules.Product;
using Modules.Cart;
using Microsoft.OpenApi;





var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<ApiExceptionMiddleware>();

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
    // options.AddSecurityRequirement(new OpenApiSecurityRequirement
    // {
    //     {
    //         new OpenApiSecurityScheme
    //         {
    //             Reference = new OpenApiReference
    //             {
    //                 Type = ReferenceType.SecurityScheme,
    //                 Id = "bearer"
    //             }
    //         },
    //         Array.Empty<string>()
    //     }
    // });
});

// JWT Configuration
// var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")?.Split('\n', '\r')[0].Trim()
//     ?? builder.Configuration["Jwt:Key"];
// if (string.IsNullOrEmpty(jwtKey)) throw new InvalidOperationException("JWT Key is not configured. Set JWT_KEY environment variable or Jwt:Key in appsettings.json");
// var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"];
// var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"];

// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         var key = Encoding.UTF8.GetBytes(jwtKey);

//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = true,
//             ValidateAudience = true,
//             ValidateLifetime = true,
//             ValidateIssuerSigningKey = true,
//             ValidIssuer = jwtIssuer,
//             ValidAudience = jwtAudience,
//             IssuerSigningKey = new SymmetricSecurityKey(key),
//             ClockSkew = TimeSpan.Zero
//         };
//     });

// // Rate Limiting Configuration
// builder.Services.AddRateLimiter(options =>
// {
//     options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

//     options.AddPolicy("per-user", context =>
//     {
//         var userId = context.User?.FindFirst("sub")?.Value;
//         var key = userId ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

//         return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
//         {
//             TokenLimit = 30,
//             TokensPerPeriod = 30,
//             ReplenishmentPeriod = TimeSpan.FromMinutes(1),
//             AutoReplenishment = true,
//             QueueLimit = 0
//         });
//     });
// });



// Add Modules
builder.Services.AddUserModule(builder.Configuration);
builder.Services.AddProductModule(builder.Configuration);
builder.Services.AddCartModule(builder.Configuration);


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ApiExceptionMiddleware>();

// app.UseHttpsRedirection();
// app.UseAuthentication();
// app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

// app.UseRateLimiter();

app.MapControllers();
app.Run();
