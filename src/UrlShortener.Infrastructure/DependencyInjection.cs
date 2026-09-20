namespace UrlShortener.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // افزودن MemoryCache دات‌نت به همراه تنظیم محدودیت سقف تعداد کلیدها (جلوگیری از Out Of Memory)
        services.AddMemoryCache(options =>
        {
            // حداکثر نگه‌داری ۵۰۰ هزار لینک کوتاه در حافظه رم نود جاری
            options.SizeLimit = 500_000;
        });

        // ثبت پیاده‌سازی مموری‌کش به جای RedisCacheService
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // Range Allocator
        services.AddSingleton<IIdRangeAllocatorRepository, IdRangeAllocatorRepository>();

        // Repositories
        services.AddScoped<IUrlRepository, UrlRepository>();

        return services;
    }
}