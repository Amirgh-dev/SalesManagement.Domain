using System.Data;

namespace SalesManagement.Persistence.ConnectionFactories
{
    public interface ISepidarConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}