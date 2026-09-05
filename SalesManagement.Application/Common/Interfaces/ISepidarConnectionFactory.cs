using System.Data;

namespace SalesManagement.Application.Common.Interfaces
{
    public interface ISepidarConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}