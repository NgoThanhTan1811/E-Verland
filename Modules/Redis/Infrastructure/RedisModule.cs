using System.Security.Authentication;
using Modules.Redis.Services;
using StackExchange.Redis;

namespace Modules.Redis.Infrastructure;

public static class RedisModuleExtensions
{
    public static IServiceCollection AddRedisModule(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection =
            configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Missing Redis connection string.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redisConnection);

            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 5000;
            options.KeepAlive = 30;
            options.Ssl = true;
            options.SslProtocols = SslProtocols.Tls12;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, CacheService>();

        services.AddScoped<IJwtCacheService, JwtCacheService>();
        services.AddScoped<IProductCacheService, ProductCacheService>();
        services.AddScoped<ICartCacheService, CartCacheService>();

        return services;
    }
}
