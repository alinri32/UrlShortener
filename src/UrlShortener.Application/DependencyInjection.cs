namespace UrlShortener.Application;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Application.Common.Generators;
using UrlShortener.Application.Common.Validators;
using UrlShortener.Application.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Singletons
        services.AddSingleton<UniqueIdGenerator>();

        // Scoped Services
        services.AddScoped<IUrlService, UrlService>();

        // Fluent Validators
        services.AddValidatorsFromAssemblyContaining<ShortenUrlRequestValidator>();

        return services;
    }
}