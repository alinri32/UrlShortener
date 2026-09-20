using System.Security.Claims;
using UrlShortener.Application.DTOs;
using UrlShortener.Application.Services;
using UrlShortener.WebApi.Authentication;
using UrlShortener.WebApi.Extensions;

namespace UrlShortener.WebApi.Endpoints;

public static class UrlEndpoints
{
    public static IEndpointRouteBuilder MapUrlEndpoints(this IEndpointRouteBuilder app)
    {
        // Command Route: Create Short URL
        app.MapPost("/api/v1/urls", HandleCreateShortUrlAsync)
            .WithName("CreateShortUrl")
            .WithSummary("Creates a new shortened URL")
            .Produces<ShortenUrlResponseDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimiterExtensions.CreationPolicyName)
            .AddEndpointFilter<ApiKeyEndpointFilter>();

        // Query Route: Redirect
        app.MapGet("/{shortCode:regex(^[a-zA-Z0-9]{{1,11}}$)}", HandleRedirectAsync)
            .WithName("RedirectUrl")
            .WithSummary("Redirects to the original URL via HTTP 302")
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> HandleCreateShortUrlAsync(
        ShortenUrlRequestDto request,
        IUrlService urlService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        long? userId = ResolveUserId(httpContext);
        string baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        var response = await urlService.SetAsync(request, userId, baseUrl, ct);

        return Results.Created($"/{response.ShortCode}", response);
    }

    private static async Task<IResult> HandleRedirectAsync(
        string shortCode,
        IUrlService urlService,
        CancellationToken ct)
    {
        string? originalUrl = await urlService.GetAsync(shortCode, ct);

        return originalUrl is not null
            ? Results.Redirect(originalUrl, permanent: false)
            : Results.NotFound();
    }

    private static long? ResolveUserId(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var claimValue = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(claimValue, out long parsedId))
            {
                return parsedId;
            }
        }
        else if (httpContext.Items.TryGetValue("UserId", out var idObj) && idObj is long id)
        {
            return id;
        }

        return null;
    }
}