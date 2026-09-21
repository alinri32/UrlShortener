namespace UrlShortener.Application.Services;

using FluentValidation;
using UrlShortener.Application.Common.Generators;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.DTOs;
using UrlShortener.Domain.Common;
using UrlShortener.Domain.Entities;

public sealed class UrlService : IUrlService
{
    // Services
    private readonly IUrlRepository _urlRepository;
    private readonly ICacheService _cacheService;
    private readonly UniqueIdGenerator _idGenerator;
    private readonly IValidator<ShortenUrlRequestDto> _validator;
    
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    // Ctor
    public UrlService(
        IUrlRepository urlRepository,
        ICacheService cacheService,
        UniqueIdGenerator idGenerator,
        IValidator<ShortenUrlRequestDto> validator)
    {
        _urlRepository = urlRepository;
        _cacheService = cacheService;
        _idGenerator = idGenerator;
        _validator = validator;
    }

    // Command: Create Short URL
    public async Task<ShortenUrlResponseDto> SetAsync(ShortenUrlRequestDto request, long? userId, string baseUrl, CancellationToken cancellationToken = default)
    {
        // Input Validation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Generate Unique Numeric ID
        long numericId = await _idGenerator.NextIdAsync(cancellationToken);
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

        await _urlRepository.CreateAsync(shortenedUrl, cancellationToken);

        // Cache 
        TimeSpan ttl = request.ExpiresAt.HasValue
            ? (request.ExpiresAt.Value - DateTime.UtcNow)
            : DefaultTtl;

        await _cacheService.SetUrlAsync(shortCode, request.OriginalUrl, ttl, cancellationToken);

        // Result 
        string fullShortUrl = $"{baseUrl.TrimEnd('/')}/{shortCode}";
        return new ShortenUrlResponseDto(shortCode, fullShortUrl, request.ExpiresAt);
    }

    // Query: Resolve Short URL
    public async Task<string?> GetAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        // Cache 
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