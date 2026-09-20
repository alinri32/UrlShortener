namespace UrlShortener.Application.Common.Interfaces;

public interface IApiKeyValidator
{
    Task<long?> ValidateApiKeyAsync(string rawApiKey, CancellationToken cancellationToken = default);
}