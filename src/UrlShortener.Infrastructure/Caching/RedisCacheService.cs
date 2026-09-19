namespace UrlShortener.Infrastructure.Caching;

using StackExchange.Redis;
using System.Collections.Concurrent;
using UrlShortener.Application.Common.Interfaces;

public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    // Single-Flight In-Flight Registry
    private readonly ConcurrentDictionary<string, Task<string?>> _inFlightRequests = new();

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = _redis.GetDatabase();
    }

    // Direct Lookup
    public async Task<string?> GetUrlAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        string cacheKey = FormatKey(shortCode);
        RedisValue value = await _database.StringGetAsync(cacheKey);

        return value.HasValue ? value.ToString() : null;
    }

    // Write-Through / Cache-Aside Set
    public async Task SetUrlAsync(string shortCode, string originalUrl, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        string cacheKey = FormatKey(shortCode);
        await _database.StringSetAsync(cacheKey, originalUrl, ttl);
    }

    // Single-Flight Cache Lookup & Fallback
    public async Task<string?> GetOrSetUrlAsync(
        string shortCode,
        Func<Task<string?>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        // Cache Lookup
        string? cachedValue = await GetUrlAsync(shortCode, cancellationToken);
        if (cachedValue is not null)
        {
            return cachedValue;
        }

        // Single-Flight Deduplication Engine
        return await _inFlightRequests.GetOrAdd(shortCode, _ => ExecuteFlightAsync(shortCode, factory, ttl));
    }

    // Flight Worker
    private async Task<string?> ExecuteFlightAsync(string shortCode, Func<Task<string?>> factory, TimeSpan ttl)
    {
        try
        {
            // Second Cache Check Inside Flight
            string? value = await GetUrlAsync(shortCode);
            if (value is not null)
            {
                return value;
            }

            // DB Fallback Execution
            value = await factory();

            if (value is not null)
            {
                // Populate Cache
                await SetUrlAsync(shortCode, value, ttl);
            }

            return value;
        }
        finally
        {
            // Flight Completed - Remove Lock Registration
            _inFlightRequests.TryRemove(shortCode, out _);
        }
    }

    // Key Prefix Normalizer
    private static string FormatKey(string shortCode) => $"url:{shortCode}";
}