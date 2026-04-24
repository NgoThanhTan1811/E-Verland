using System.Security.Authentication;
using Modules.Redis.Services;
using StackExchange.Redis;

namespace Modules.Redis.Infrastructure;

public static class RedisModuleExtension
{
    public static IServiceCollection AddRedisModule(this IServiceCollection services, ConfigurationManager configuration)
    {
        var host = Environment.GetEnvironmentVariable("Redis_URL");
        var portValue = Environment.GetEnvironmentVariable("Redis_Port");
        var user = Environment.GetEnvironmentVariable("Redis_User");
        var password = Environment.GetEnvironmentVariable("Redis_Password");
        var ssl = bool.TryParse(Environment.GetEnvironmentVariable("Redis_Ssl"), out var parsedSsl) && parsedSsl;
        var abortConnectValue = Environment.GetEnvironmentVariable("Redis_AbortConnect");

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("Missing Redis_URL.");

        if (!int.TryParse(portValue, out var port))
            port = 6379;



        if (!bool.TryParse(abortConnectValue, out var abortConnect))
            abortConnect = false;

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = new ConfigurationOptions
            {
                User = user,
                Password = password,
                Ssl = ssl,
                SslHost = host,
                AbortOnConnectFail = abortConnect,
                ConnectTimeout = 5000,
                SyncTimeout = 5000,
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