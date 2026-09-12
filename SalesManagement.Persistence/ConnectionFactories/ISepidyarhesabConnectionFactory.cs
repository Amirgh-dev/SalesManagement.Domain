using System.Data;

namespace SalesManagement.Persistence.ConnectionFactories
{
    public interface ISepidyarhesabConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}