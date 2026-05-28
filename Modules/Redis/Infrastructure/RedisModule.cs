using System.Security.Authentication;
using Modules.Redis.Services;
using StackExchange.Redis;

namespace Modules.Redis.Infrastructure;

public static class RedisModuleExtension
{
    public static IServiceCollection AddRedisModule(this IServiceCollection services, ConfigurationManager configuration)
    {
        var connectionString = configuration["Redis:ConnectionString"]
            ?? configuration["Redis:URL"]
            ?? configuration["Redis:Url"];

        var host = configuration["Redis:Host"] ?? configuration["Redis:URL"] ?? "localhost";
        var portValue = configuration["Redis:Port"] ?? "6379";
        var user = configuration["Redis:User"] ?? "default";
        var password = configuration["Redis:Password"] ?? string.Empty;
        var ssl = bool.TryParse(configuration["Redis:Ssl"], out var sslValue) && sslValue;
        var abortConnectValue = configuration["Redis:AbortConnect"] ?? "false";

        if (string.IsNullOrWhiteSpace(connectionString) && string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("Missing Redis configuration. Set Redis:ConnectionString in user secrets or environment variables.");

        if (!string.IsNullOrWhiteSpace(connectionString) && Uri.TryCreate(connectionString, UriKind.Absolute, out var redisUri))
        {
            host = redisUri.Host;
            portValue = redisUri.Port > 0 ? redisUri.Port.ToString() : portValue;

            if (!string.IsNullOrWhiteSpace(redisUri.UserInfo))
            {
                var userInfo = redisUri.UserInfo.Split(':', 2);
                if (userInfo.Length > 0 && !string.IsNullOrWhiteSpace(userInfo[0]))
                    user = Uri.UnescapeDataString(userInfo[0]);

                if (userInfo.Length > 1)
                    password = Uri.UnescapeDataString(userInfo[1]);
            }

            ssl = string.Equals(redisUri.Scheme, "rediss", StringComparison.OrdinalIgnoreCase) || ssl;
        }

        var port = int.TryParse(portValue, out var parsedPort) ? parsedPort : 6379;

        var sslHost = host;
        if (host.Contains(':') && !host.StartsWith('['))
        {
            sslHost = host.Split(':', 2)[0];
        }

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = new ConfigurationOptions
            {
                User = user,
                Password = password,
                Ssl = ssl,
                SslHost = sslHost,
                AbortOnConnectFail = abortConnectValue == "true",
                ConnectTimeout = 10000,
                SyncTimeout = 10000,
                KeepAlive = 30,
                SslProtocols = SslProtocols.Tls12
            };

            options.EndPoints.Add(host, port);

            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ICacheService, CacheService>();
        services.AddScoped<IJwtCacheService, JwtCacheService>();
        services.AddScoped<IProductCacheService, ProductCacheService>();
        services.AddScoped<ICartCacheService, CartCacheService>();

        return services;
    }
}