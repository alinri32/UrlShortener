namespace UrlShortener.Domain.Entities;

public sealed class ApiKey
{
    public long Id { get; init; }
    public long UserId { get; init; }
    public string KeyHash { get; init; } = string.Empty;
    public string KeyPrefix { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; init; }
    public bool IsRevoked { get; set; }

    // Validation
    public bool IsValid() => !IsRevoked && (ExpiresAt is null || ExpiresAt > DateTime.UtcNow);
}