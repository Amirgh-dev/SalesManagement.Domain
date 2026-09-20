using System.Data;

namespace SalesManagement.Persistence.ConnectionFactories
{
    public interface IZanisServicesConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
