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
using Infra.AWS;
using Infra.AWS.CloudWatch;
using Infra.AWS.XRay;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.Runtime;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.AwsCloudWatch;

var builder = WebApplication.CreateBuilder(args);

var envPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));
Env.Load(envPath);

var cloudWatchOptions = builder.Configuration.GetSection(CloudWatchOptions.SectionName).Get<CloudWatchOptions>() ?? new CloudWatchOptions();
cloudWatchOptions.Enabled = bool.TryParse(Environment.GetEnvironmentVariable("AWS_CLOUDWATCH_ENABLED"), out var sinkEnabledOverride)
    ? sinkEnabledOverride
    : cloudWatchOptions.Enabled;

var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(new RenderedCompactJsonFormatter());

if (cloudWatchOptions.Enabled)
{
    var awsRegion = builder.Configuration["AWS:Region"]
        ?? Environment.GetEnvironmentVariable("AWS_REGION")
        ?? cloudWatchOptions.Region;
    var awsAccessKey = builder.Configuration["AWS:AccessKey"] ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
    var awsSecretKey = builder.Configuration["AWS:SecretKey"] ?? Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

    var awsClientConfig = new AmazonCloudWatchLogsConfig
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(awsRegion)
    };

    var cloudWatchClient = !string.IsNullOrWhiteSpace(awsAccessKey) && !string.IsNullOrWhiteSpace(awsSecretKey)
        ? new AmazonCloudWatchLogsClient(new BasicAWSCredentials(awsAccessKey, awsSecretKey), awsClientConfig)
        : new AmazonCloudWatchLogsClient(awsClientConfig);

    var sinkOptions = new CloudWatchSinkOptions
    {
        LogGroupName = string.IsNullOrWhiteSpace(cloudWatchOptions.LogGroupName)
            ? cloudWatchOptions.ApplicationLogGroup
            : cloudWatchOptions.LogGroupName,
        LogStreamNameProvider = new PrefixedLogStreamNameProvider(cloudWatchOptions.LogStreamPrefix),
        TextFormatter = new RenderedCompactJsonFormatter(),
        MinimumLogEventLevel = Serilog.Events.LogEventLevel.Information,
        BatchSizeLimit = cloudWatchOptions.BatchSizeLimit,
        QueueSizeLimit = cloudWatchOptions.QueueSizeLimit,
        Period = TimeSpan.FromSeconds(Math.Max(1, cloudWatchOptions.PeriodSeconds)),
        CreateLogGroup = cloudWatchOptions.CreateLogGroup,
        RetryAttempts = (byte)Math.Clamp(cloudWatchOptions.RetryAttempts, 0, byte.MaxValue)
    };

    loggerConfiguration.WriteTo.AmazonCloudWatch(sinkOptions, cloudWatchClient);
}

Log.Logger = loggerConfiguration.CreateLogger();
builder.Host.UseSerilog();

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
