namespace UrlShortener.Application.Validators;

using FluentValidation;
using UrlShortener.Application.DTOs;

public sealed class ShortenUrlRequestValidator : AbstractValidator<ShortenUrlRequest>
{
    public ShortenUrlRequestValidator()
    {
        // Validation Rules
        RuleFor(x => x.OriginalUrl)
            .NotEmpty().WithMessage("Original URL is required.")
            .MaximumLength(2048).WithMessage("URL length cannot exceed 2048 characters.")
            .Must(BeAValidUrl).WithMessage("Original URL must be a valid HTTP or HTTPS address.");

        RuleFor(x => x.ExpiresAt)
            .Must(x => !x.HasValue || x.Value > DateTime.UtcNow)
            .WithMessage("Expiration date must be in the future.");
    }

    // URI Structural Checker
    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}