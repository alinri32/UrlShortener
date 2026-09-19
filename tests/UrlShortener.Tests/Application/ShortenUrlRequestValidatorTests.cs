namespace UrlShortener.Tests.Application;

using FluentAssertions;
using UrlShortener.Application.DTOs;
using UrlShortener.Application.Validators;
using Xunit;

public class ShortenUrlRequestValidatorTests
{
    private readonly ShortenUrlRequestValidator _validator = new();

    [Theory]
    [InlineData("https://google.com")]
    [InlineData("http://subdomain.example.org/path/to/resource?arg=val")]
    public async Task Validate_ValidUrl_ShouldPassValidation(string validUrl)
    {
        // Arrange
        var request = new ShortenUrlRequest(validUrl);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-url")]
    [InlineData("ftp://files.example.com")] // Invalid scheme
    public async Task Validate_InvalidUrl_ShouldFailValidation(string invalidUrl)
    {
        // Arrange
        var request = new ShortenUrlRequest(invalidUrl);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ShortenUrlRequest.OriginalUrl));
    }

    [Fact]
    public async Task Validate_PastExpirationDate_ShouldFailValidation()
    {
        // Arrange
        var request = new ShortenUrlRequest("https://example.com", DateTime.UtcNow.AddMinutes(-10));

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ShortenUrlRequest.ExpiresAt));
    }
}