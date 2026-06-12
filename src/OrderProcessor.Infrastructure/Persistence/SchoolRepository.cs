using Dapper;
using Microsoft.Data.SqlClient;
using OrderProcessor.Application.Contracts.Repositories;
using OrderProcessor.Domain.Enums;

namespace OrderProcessor.Infrastructure.Persistence
{
    public sealed class SchoolRepository : ISchoolRepository
    {
        private static readonly Dictionary<string, SchoolTier>
            TierMap = new(StringComparer.OrdinalIgnoreCase)
            {
                ["GOLD"] = SchoolTier.Gold,
                ["SILVER"] = SchoolTier.Silver
            };

        private readonly IDbConnectionFactory _connectionFactory;
        public SchoolRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
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
                (SqlConnection)_connectionFactory.CreateConnection();

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