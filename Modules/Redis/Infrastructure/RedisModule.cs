using System.Security.Authentication;
using Modules.Redis.Services;
using StackExchange.Redis;

namespace Modules.Redis.Infrastructure;

public static class RedisModuleExtension
{
    public static IServiceCollection AddRedisModule(this IServiceCollection services, ConfigurationManager configuration)
    {
        var host = configuration["Redis:URL"] ?? "localhost";
        var portValue = configuration["Redis:Port"] ?? "6379";
        var user = configuration["Redis:User"] ?? "default";
        var password = configuration["Redis:Password"] ?? string.Empty;
        var ssl = bool.TryParse(configuration["Redis:Ssl"], out var sslValue) && sslValue;
        var abortConnectValue = configuration["Redis:AbortConnect"] ?? "false";

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("Missing Redis_URL.");

        var port = int.TryParse(portValue, out var parsedPort) ? parsedPort : 6379;

        var sslHost = host;
        if (host.Contains(':') && !host.StartsWith('['))
        {
            sslHost = host.Split(':', 2)[0];
        }

        // 4. Đăng ký ConnectionMultiplexer
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = new ConfigurationOptions
            {
                User = user,
                Password = password,
                Ssl = ssl,
                SslHost = sslHost, // Dùng sslHost thuần túy cho chứng chỉ TLS
                AbortOnConnectFail = abortConnectValue == "false",
                ConnectTimeout = 5000,
                SyncTimeout = 5000,
                KeepAlive = 30,
                SslProtocols = SslProtocols.Tls12
            };

            options.EndPoints.Add(sslHost, port);

            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ICacheService, CacheService>();
        services.AddScoped<IJwtCacheService, JwtCacheService>();
        services.AddScoped<IProductCacheService, ProductCacheService>();
        services.AddScoped<ICartCacheService, CartCacheService>();

        return services;
    }
}