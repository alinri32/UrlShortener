namespace UrlShortener.Application.Services;

using UrlShortener.Application.DTOs;

public interface IUrlService
{
    // Command
    Task<ShortenUrlResponse> ShortenUrlAsync(ShortenUrlRequest request, long? userId, string baseUrl, CancellationToken cancellationToken = default);

    // Query
    Task<string?> GetOriginalUrlAsync(string shortCode, CancellationToken cancellationToken = default);
}