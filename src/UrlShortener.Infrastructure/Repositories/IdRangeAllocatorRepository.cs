namespace UrlShortener.Infrastructure.Repositories;

using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using UrlShortener.Application.Common.Interfaces;

public sealed class IdRangeAllocatorRepository : IIdRangeAllocatorRepository
{
    private readonly string _connectionString;

    public IdRangeAllocatorRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    // Atomic Range Fetch
    public async Task<(long Start, long End)> AllocateRangeAsync(int batchSize = 100000, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@BatchSize", batchSize, DbType.Int32, ParameterDirection.Input);
        parameters.Add("@AllocatedStart", dbType: DbType.Int64, direction: ParameterDirection.Output);
        parameters.Add("@AllocatedEnd", dbType: DbType.Int64, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            commandText: "dbo.usp_AllocateIdRange",
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken
        );

        // Execute Procedure
        await connection.ExecuteAsync(command);

        long start = parameters.Get<long>("@AllocatedStart");
        long end = parameters.Get<long>("@AllocatedEnd");

        return (start, end);
    }
}