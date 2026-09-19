namespace UrlShortener.Infrastructure.Persistence;

using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using UrlShortener.Application.Common.Interfaces;
using UrlShortener.Domain.Entities;

public sealed class UrlRepository : IUrlRepository
{
    private readonly string _connectionString;

    public UrlRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    // High-Performance Point Lookup Query
    public async Task<ShortenedUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                Id, 
                ShortCode, 
                OriginalUrl, 
                CreatedByUserId, 
                CreatedAt, 
                ExpiresAt, 
                IsActive
            FROM dbo.ShortenedUrls WITH (NOLOCK)
            WHERE ShortCode = @ShortCode;
        """;

        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new { ShortCode = shortCode },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken
        );

        return await connection.QuerySingleOrDefaultAsync<ShortenedUrl>(command);
    }

    // Command: Append-Only Create
    public async Task CreateAsync(ShortenedUrl shortenedUrl, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.ShortenedUrls 
                (Id, ShortCode, OriginalUrl, CreatedByUserId, CreatedAt, ExpiresAt, IsActive)
            VALUES 
                (@Id, @ShortCode, @OriginalUrl, @CreatedByUserId, @CreatedAt, @ExpiresAt, @IsActive);
        """;

        await using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            commandText: sql,
            parameters: shortenedUrl,
            commandType: CommandType.Text,
            cancellationToken: cancellationToken
        );

        await connection.ExecuteAsync(command);
    }
}