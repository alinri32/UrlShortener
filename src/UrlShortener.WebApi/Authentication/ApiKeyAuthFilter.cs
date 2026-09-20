namespace UrlShortener.WebApi.Authentication;

using UrlShortener.Application.Common.Interfaces;

public sealed class ApiKeyEndpointFilter : IEndpointFilter
{
    private readonly IApiKeyValidator _apiKeyValidator;
    private const string HeaderName = "X-Api-Key";

    public ApiKeyEndpointFilter(IApiKeyValidator apiKeyValidator)
    {
        _apiKeyValidator = apiKeyValidator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        // Skip If Authenticated Via JWT Bearer
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            return await next(context);
        }

        // Header Check
        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var extractedApiKey) ||
            string.IsNullOrWhiteSpace(extractedApiKey))
        {
            return Results.Json(new { error = "Unauthorized: Missing API Key." }, statusCode: StatusCodes.Status401Unauthorized);
        }

        // Validation Delegated to Application/Infrastructure
        long? userId = await _apiKeyValidator.ValidateApiKeyAsync(extractedApiKey.ToString(), httpContext.RequestAborted);

        if (userId is null)
        {
            return Results.Json(new { error = "Unauthorized: Invalid or expired API Key." }, statusCode: StatusCodes.Status401Unauthorized);
        }

        // Store UserId Context
        httpContext.Items["UserId"] = userId.Value;

        return await next(context);
    }
}