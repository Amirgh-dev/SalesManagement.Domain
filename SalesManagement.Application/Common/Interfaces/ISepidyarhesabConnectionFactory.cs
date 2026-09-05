using System.Data;

namespace SalesManagement.Application.Common.Interfaces
{
    public interface ISepidyarhesabConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}