using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SalesManagement.Application.Common.Interfaces;

namespace SalesManagement.Persistence.ConnectionFactories
{
    public class SepidyarhesabConnectionFactory : ISepidyarhesabConnectionFactory
    {
        private readonly string _connectionString;

        public SepidyarhesabConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Sepidyarhesab")
                                ?? throw new InvalidOperationException("Connection String 'Sepidyarhesab' پیدا نشد");
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}