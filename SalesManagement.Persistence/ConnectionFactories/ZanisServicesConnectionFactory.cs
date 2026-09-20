using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SalesManagement.Persistence.ConnectionFactories
{
    public class ZanisServicesConnectionFactory : IZanisServicesConnectionFactory
    {
        private readonly string _connectionString;

        public ZanisServicesConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ZanisServices")
                                ?? throw new InvalidOperationException("Connection String 'ZanisServices' پیدا نشد");
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
