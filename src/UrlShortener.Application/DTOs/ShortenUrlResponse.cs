namespace UrlShortener.Application.DTOs;

public sealed record ShortenUrlResponse(string ShortCode, string ShortUrl, DateTime? ExpiresAt);