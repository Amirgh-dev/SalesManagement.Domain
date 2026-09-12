using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SalesManagement.Persistence.ConnectionFactories
{
    public class SepidarConnectionFactory : ISepidarConnectionFactory
    {
        private readonly string _connectionString;

        public SepidarConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Sepidar")
                                ?? throw new InvalidOperationException("Connection String 'Sepidar' پیدا نشد");
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}