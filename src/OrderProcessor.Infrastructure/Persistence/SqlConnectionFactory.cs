using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace OrderProcessor.Infrastructure.Persistence
{
    public sealed class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("OrderHub")
                ?? throw new InvalidOperationException(
                    "Connection string missing.");
        }
        public IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);
    }
}