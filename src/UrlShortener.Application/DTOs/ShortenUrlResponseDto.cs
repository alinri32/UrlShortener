namespace UrlShortener.Application.DTOs;

public sealed record ShortenUrlResponseDto
(
    string ShortCode,
    string ShortUrl,
    DateTime? ExpiresAt
);