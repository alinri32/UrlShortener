namespace UrlShortener.Tests.Application;

using FluentAssertions;
using FluentValidation;
using NSubstitute;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Application.DTOs;
using UrlShortener.Application.Services;
using UrlShortener.Domain.Entities;
using Xunit;

public class UrlServiceTests
{
    private readonly IUrlRepository _urlRepository = Substitute.For<IUrlRepository>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly IIdRangeAllocatorRepository _rangeAllocator = Substitute.For<IIdRangeAllocatorRepository>();
    private readonly IValidator<ShortenUrlRequest> _validator = Substitute.For<IValidator<ShortenUrlRequest>>();
    private readonly UrlService _sut;

    public UrlServiceTests()
    {
        _rangeAllocator.AllocateRangeAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((10000000L, 10100000L)));

        var idGenerator = new UniqueIdGenerator(_rangeAllocator);

        _sut = new UrlService(_urlRepository, _cacheService, idGenerator, _validator);
    }

    [Fact]
    public async Task ShortenUrlAsync_WhenValid_ShouldPersistAndSetCache()
    {
        // Arrange
        var request = new ShortenUrlRequest("https://example.com");
        _validator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await _sut.SetAsync(request, userId: 1, baseUrl: "https://sho.rt");

        // Assert
        result.Should().NotBeNull();
        result.ShortCode.Should().NotBeNullOrEmpty();
        result.ShortUrl.Should().StartWith("https://sho.rt/");

        await _urlRepository.Received(1).CreateAsync(Arg.Is<ShortenedUrl>(u => u.OriginalUrl == request.OriginalUrl));
        await _cacheService.Received(1).SetUrlAsync(result.ShortCode, request.OriginalUrl, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOriginalUrlAsync_WhenCacheHit_ShouldReturnDirectlyWithoutDbCall()
    {
        // Arrange
        const string shortCode = "FXsk";
        const string targetUrl = "https://example.com";

        _cacheService.GetOrSetUrlAsync(
            shortCode,
            Arg.Any<Func<Task<string?>>>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>())
            .Returns(targetUrl);

        // Act
        string? result = await _sut.GetAsync(shortCode);

        // Assert
        result.Should().Be(targetUrl);
        await _urlRepository.DidNotReceive().GetByShortCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}