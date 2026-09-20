namespace UrlShortener.Application.Services;

using UrlShortener.Application.DTOs;

public interface IUrlService
{
    // Command
    Task<ShortenUrlResponseDto> SetAsync(ShortenUrlRequestDto request, long? userId, string baseUrl, CancellationToken cancellationToken = default);

    // Query
    Task<string?> GetAsync(string shortCode, CancellationToken cancellationToken = default);
}