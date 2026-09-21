namespace UrlShortener.Application.Common.Interfaces;

public interface ICacheService
{
    Task<string?> GetUrlAsync(string shortCode, CancellationToken cancellationToken = default);
    Task SetUrlAsync(string shortCode, string originalUrl, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<string?> GetOrSetUrlAsync(string shortCode, Func<Task<string?>> factory, TimeSpan ttl, CancellationToken cancellationToken = default);
}