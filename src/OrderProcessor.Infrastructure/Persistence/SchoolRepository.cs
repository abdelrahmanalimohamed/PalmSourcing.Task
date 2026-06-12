using Dapper;
using Microsoft.Extensions.Configuration;
using OrderProcessor.Application.Contracts.Repositories;
using OrderProcessor.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace OrderProcessor.Infrastructure.Persistence
{
    public sealed class SchoolRepository : ISchoolRepository
    {
        private readonly string _connectionString;

        private static readonly Dictionary<string, SchoolTier>
            TierMap = new(StringComparer.OrdinalIgnoreCase)
            {
                ["GOLD"] = SchoolTier.Gold,
                ["SILVER"] = SchoolTier.Silver
            };

        public SchoolRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("OrderHub")
                ?? throw new InvalidOperationException(
                    "Connection string missing.");
        }

        public async Task<SchoolTier?> GetTierAsync(
            int schoolId,
            CancellationToken ct)
        {
            const string sql = """
            SELECT TierCode
            FROM Schools
            WHERE Id = @SchoolId
            """;

            await using var connection =
                new SqlConnection(_connectionString);

            var tierCode = await connection.QuerySingleOrDefaultAsync<string>(
                    new CommandDefinition(
                        sql,
                        new { SchoolId = schoolId },
                        cancellationToken: ct));

            if (tierCode is null)
                return null;

            return TierMap.GetValueOrDefault(
                tierCode,
                SchoolTier.Standard);
        }
    }
}