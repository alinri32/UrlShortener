namespace UrlShortener.WebApi.Authentication;

using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public sealed class ApiKeyEndpointFilter : IEndpointFilter
{
    private readonly string _connectionString;
    private const string HeaderName = "X-Api-Key";

    public ApiKeyEndpointFilter(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
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
        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var extractedApiKey) || string.IsNullOrWhiteSpace(extractedApiKey))
        {
            return Results.Json(new { error = "Unauthorized: Missing API Key or Bearer Token." }, statusCode: StatusCodes.Status401Unauthorized);
        }

        string rawKey = extractedApiKey.ToString();
        string keyPrefix = rawKey.Length >= 8 ? rawKey[..8] : string.Empty;
        string keyHash = ComputeSha256Hex(rawKey);

        // Fast Lookup with Prefix Filter
        const string sql = """
            SELECT UserId 
            FROM dbo.ApiKeys WITH (NOLOCK)
            WHERE KeyPrefix = @KeyPrefix 
              AND KeyHash = @KeyHash 
              AND IsRevoked = 0 
              AND (ExpiresAt IS NULL OR ExpiresAt > SYSUTCDATETIME());
        """;

        await using var conn = new SqlConnection(_connectionString);
        long? userId = await conn.QueryFirstOrDefaultAsync<long?>(sql, new { KeyPrefix = keyPrefix, KeyHash = keyHash });

        if (userId is null)
        {
            return Results.Json(new { error = "Unauthorized: Invalid or expired API Key." }, statusCode: StatusCodes.Status401Unauthorized);
        }

        // Store UserId Context
        httpContext.Items["UserId"] = userId.Value;

        return await next(context);
    }

    // SHA-256 Utility (Zero-Allocation Spans)
    private static string ComputeSha256Hex(string input)
    {
        Span<byte> hashBytes = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(input), hashBytes);
        return Convert.ToHexString(hashBytes);
    }
}