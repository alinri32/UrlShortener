namespace UrlShortener.Infrastructure.Caching;

using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using UrlShortener.Application.Common.Interfaces;

public sealed class MemoryCacheService : ICacheService
{
    // Services
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, Task<string?>> _inFlightRequests = new();
    //Ctor
    public MemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    // Public Methods
    public Task<string?> GetUrlAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        string cacheKey = FormatKey(shortCode);
        _memoryCache.TryGetValue(cacheKey, out string? value);
        return Task.FromResult(value);
    }
    public Task SetUrlAsync(string shortCode, string originalUrl, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        string cacheKey = FormatKey(shortCode);

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Size = 1
        };

        _memoryCache.Set(cacheKey, originalUrl, options);
        return Task.CompletedTask;
    }
    public async Task<string?> GetOrSetUrlAsync(string shortCode, Func<Task<string?>> factory, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        string? cachedValue = await GetUrlAsync(shortCode, cancellationToken);
        if (cachedValue is not null)
        {
            return cachedValue;
        }

        return await _inFlightRequests.GetOrAdd(shortCode, _ => ExecuteFlightAsync(shortCode, factory, ttl));
    }

    // Private Methods
    private async Task<string?> ExecuteFlightAsync(string shortCode, Func<Task<string?>> factory, TimeSpan ttl)
    {
        try
        {
            string? value = await GetUrlAsync(shortCode);
            if (value is not null)
            {
                return value;
            }

            value = await factory();

            if (value is not null)
            {
                await SetUrlAsync(shortCode, value, ttl);
            }

            return value;
        }
        finally
        {
            _inFlightRequests.TryRemove(shortCode, out _);
        }
    }
    private static string FormatKey(string shortCode) => $"url:{shortCode}";
}