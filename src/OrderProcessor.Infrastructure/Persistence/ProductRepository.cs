using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OrderProcessor.Application.Contracts.Repositories;

namespace OrderProcessor.Infrastructure.Persistence
{
    public sealed class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;
        public ProductRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("OrderHub")!;
        }

        public async Task<decimal> GetBasePriceAsync(
            string sku,
            CancellationToken ct)
        {
            const string sql = """
            SELECT BasePrice
            FROM Products
            WHERE Sku = @Sku
            """;

            await using var connection =
                new SqlConnection(_connectionString);

            return await connection.QuerySingleAsync<decimal>(
                new CommandDefinition(
                    sql,
                    new { Sku = sku },
                    cancellationToken: ct));
        }
    }
}