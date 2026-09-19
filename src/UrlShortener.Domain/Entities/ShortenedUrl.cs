namespace UrlShortener.Domain.Entities;

public sealed class ShortenedUrl
{
    public long Id { get; init; }
    public string ShortCode { get; init; } = string.Empty;
    public string OriginalUrl { get; init; } = string.Empty;
    public long? CreatedByUserId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; init; }
    public bool IsActive { get; set; } = true;

    // Validation
    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    public bool CanRedirect() => IsActive && !IsExpired();
}