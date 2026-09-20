namespace UrlShortener.Infrastructure.Caching;

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Application.Common.Interfaces;

public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    // حفظ الگوی Single-Flight جهت جلوگیری از Cache Stampede در ترافیک بالا
    private readonly ConcurrentDictionary<string, Task<string?>> _inFlightRequests = new();

    public MemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    // Direct Lookup
    public Task<string?> GetUrlAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        string cacheKey = FormatKey(shortCode);
        _memoryCache.TryGetValue(cacheKey, out string? value);
        return Task.FromResult(value);
    }

    // Direct Set
    public Task SetUrlAsync(string shortCode, string originalUrl, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        string cacheKey = FormatKey(shortCode);

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            // تعیین سایز منطقی برای هر آیتم در صورت فعال‌سازی SizeLimit
            Size = 1
        };

        _memoryCache.Set(cacheKey, originalUrl, options);
        return Task.CompletedTask;
    }

    // Single-Flight Pattern Protected Get
    public async Task<string?> GetOrSetUrlAsync(
        string shortCode,
        Func<Task<string?>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        // بررسی مستقیم کش
        string? cachedValue = await GetUrlAsync(shortCode, cancellationToken);
        if (cachedValue is not null)
        {
            return cachedValue;
        }

        // ایجاد پرواز واحد در صورت عدم وجود کلید
        return await _inFlightRequests.GetOrAdd(shortCode, _ => ExecuteFlightAsync(shortCode, factory, ttl));
    }

    private async Task<string?> ExecuteFlightAsync(string shortCode, Func<Task<string?>> factory, TimeSpan ttl)
    {
        try
        {
            // بررسی مجدد پس از ورود به Flight
            string? value = await GetUrlAsync(shortCode);
            if (value is not null)
            {
                return value;
            }

            // واکشی از دیتابیس
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