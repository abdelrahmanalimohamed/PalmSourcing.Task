using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OrderProcessor.Application.Contracts.Repositories;

namespace OrderProcessor.Infrastructure.Persistence
{
    public sealed class InventoryRepository : IInventoryRepository
    {
        private readonly string _connectionString;
        public InventoryRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("OrderHub")!;
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
                new SqlConnection(_connectionString);

            return await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    sql,
                    new { Sku = sku },
                    cancellationToken: ct));
        }
    }
}