namespace UrlShortener.Application.Common.Interfaces;

using UrlShortener.Domain.Entities;

public interface IUrlRepository
{
    // High-Performance Query Route
    Task<ShortenedUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);

    // Command Route
    Task CreateAsync(ShortenedUrl shortenedUrl, CancellationToken cancellationToken = default);
}