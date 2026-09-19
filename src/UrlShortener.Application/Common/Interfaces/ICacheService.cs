namespace UrlShortener.Application.Common.Interfaces;

public interface ICacheService
{
    // Direct Lookup
    Task<string?> GetUrlAsync(string shortCode, CancellationToken cancellationToken = default);

    // Write-Through / Cache-Aside
    Task SetUrlAsync(string shortCode, string originalUrl, TimeSpan ttl, CancellationToken cancellationToken = default);

    // Single-Flight Pattern Protected Get
    Task<string?> GetOrSetUrlAsync(
        string shortCode,
        Func<Task<string?>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);
}