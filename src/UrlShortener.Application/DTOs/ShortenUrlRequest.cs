namespace UrlShortener.Application.DTOs;

public sealed record ShortenUrlRequest(string OriginalUrl, DateTime? ExpiresAt = null);