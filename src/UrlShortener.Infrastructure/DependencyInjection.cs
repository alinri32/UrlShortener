namespace UrlShortener.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Redis Connection Multiplexer Setup
        string redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));

        // Cache Service Registration
        services.AddSingleton<ICacheService, RedisCacheService>();

        // High-Performance ID Range Allocator
        services.AddSingleton<IIdRangeAllocator, IdRangeAllocator>();

        // Repositories
        services.AddScoped<IUrlRepository, UrlRepository>();

        return services;
    }
}