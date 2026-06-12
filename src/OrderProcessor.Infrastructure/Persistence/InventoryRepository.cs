using Dapper;
using Microsoft.Data.SqlClient;
using OrderProcessor.Application.Contracts.Repositories;

namespace OrderProcessor.Infrastructure.Persistence
{
    public sealed class InventoryRepository : IInventoryRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        public InventoryRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<int> GetStockAsync(
            string sku,
            CancellationToken ct)
        {
            const string sql = """
            SELECT Qty
            FROM Stock
            WHERE Sku = @Sku
            """;

            await using var connection =
                (SqlConnection)_connectionFactory.CreateConnection();

            return await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    sql,
                    new { Sku = sku },
                    cancellationToken: ct));
        }
    }
}