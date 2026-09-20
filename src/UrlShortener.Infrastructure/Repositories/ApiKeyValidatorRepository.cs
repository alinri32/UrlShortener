
using Dapper;
using global::UrlShortener.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace UrlShortener.Infrastructure.Repositories;

public sealed class ApiKeyValidator : IApiKeyValidator
{
    private readonly string _connectionString;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ApiKeyValidator(IConfiguration configuration, IMemoryCache cache)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        _cache = cache;
    }

    public async Task<long?> ValidateApiKeyAsync(string rawApiKey, CancellationToken cancellationToken = default)
    {
        string keyHash = ComputeSha256Hex(rawApiKey);
        string cacheKey = $"auth:apikey:{keyHash}";

        // Fast In-Memory Cache Lookup
        if (_cache.TryGetValue(cacheKey, out long cachedUserId))
        {
            return cachedUserId;
        }

        string keyPrefix = rawApiKey.Length >= 8 ? rawApiKey[..8] : string.Empty;

        const string sql = """
            SELECT UserId 
            FROM dbo.ApiKeys WITH (NOLOCK)
            WHERE KeyPrefix = @KeyPrefix 
              AND KeyHash = @KeyHash 
              AND IsRevoked = 0 
              AND (ExpiresAt IS NULL OR ExpiresAt > SYSUTCDATETIME());
        """;

        await using var conn = new SqlConnection(_connectionString);
        long? userId = await conn.QueryFirstOrDefaultAsync<long?>(
            new CommandDefinition(sql, new { KeyPrefix = keyPrefix, KeyHash = keyHash }, cancellationToken: cancellationToken));

        if (userId.HasValue)
        {
            _cache.Set(cacheKey, userId.Value, CacheDuration);
        }

        return userId;
    }

    private static string ComputeSha256Hex(string input)
    {
        Span<byte> hashBytes = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(input), hashBytes);
        return Convert.ToHexString(hashBytes);
    }
}