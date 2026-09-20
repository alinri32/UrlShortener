namespace UrlShortener.Application.Services;

using FluentValidation;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.DTOs;
using UrlShortener.Domain.Common;
using UrlShortener.Domain.Entities;

public sealed class UrlService : IUrlService
{
    private readonly IUrlRepository _urlRepository;
    private readonly ICacheService _cacheService;
    private readonly UniqueIdGenerator _idGenerator;
    private readonly IValidator<ShortenUrlRequest> _validator;

    // Cache TTL Settings
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    public UrlService(
        IUrlRepository urlRepository,
        ICacheService cacheService,
        UniqueIdGenerator idGenerator,
        IValidator<ShortenUrlRequest> validator)
    {
        _urlRepository = urlRepository;
        _cacheService = cacheService;
        _idGenerator = idGenerator;
        _validator = validator;
    }

    // Command: Create Short URL
    public async Task<ShortenUrlResponse> SetAsync(ShortenUrlRequest request, long? userId, string baseUrl, CancellationToken cancellationToken = default)
    {
        // Input Validation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Generate Unique Numeric ID
        long numericId = await _idGenerator.NextIdAsync(cancellationToken);

        // Convert to Base62 Code
        string shortCode = Base62Converter.Encode(numericId);

        // Entity Mapping
        var shortenedUrl = new ShortenedUrl
        {
            Id = numericId,
            ShortCode = shortCode,
            OriginalUrl = request.OriginalUrl,
            CreatedByUserId = userId,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // DB Persist
        await _urlRepository.CreateAsync(shortenedUrl, cancellationToken);

        // Cache Population
        TimeSpan ttl = request.ExpiresAt.HasValue
            ? (request.ExpiresAt.Value - DateTime.UtcNow)
            : DefaultTtl;

        await _cacheService.SetUrlAsync(shortCode, request.OriginalUrl, ttl, cancellationToken);

        // Result Response
        string fullShortUrl = $"{baseUrl.TrimEnd('/')}/{shortCode}";
        return new ShortenUrlResponse(shortCode, fullShortUrl, request.ExpiresAt);
    }

    // Query: Resolve Short URL (Protected with Single-Flight)
    public async Task<string?> GetAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        // Cache Lookup with Single-Flight Fallback to DB
        return await _cacheService.GetOrSetUrlAsync(
            shortCode,
            async () =>
            {
                // DB Fallback
                var entity = await _urlRepository.GetByShortCodeAsync(shortCode, cancellationToken);

                // Status Validation
                if (entity is null || !entity.CanRedirect())
                {
                    return null;
                }

                return entity.OriginalUrl;
            },
            DefaultTtl,
            cancellationToken
        );
    }
}