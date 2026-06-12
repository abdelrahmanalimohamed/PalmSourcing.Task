using Dapper;
using Microsoft.Data.SqlClient;
using OrderProcessor.Application.Contracts.Repositories;

namespace OrderProcessor.Infrastructure.Persistence
{
    public sealed class ProductRepository : IProductRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        public ProductRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
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
                (SqlConnection)_connectionFactory.CreateConnection();

            return await connection.QuerySingleAsync<decimal>(
                new CommandDefinition(
                    sql,
                    new { Sku = sku },
                    cancellationToken: ct));
        }
    }
}