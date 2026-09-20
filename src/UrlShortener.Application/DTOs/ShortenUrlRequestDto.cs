namespace UrlShortener.Application.DTOs;

public sealed record ShortenUrlRequestDto(string OriginalUrl, DateTime? ExpiresAt = null);