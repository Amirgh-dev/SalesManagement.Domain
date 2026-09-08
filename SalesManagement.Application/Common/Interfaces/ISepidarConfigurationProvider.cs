using SalesManagement.Application.Common.Configuration;

namespace SalesManagement.Application.Common.Interfaces
{
    public interface ISepidarConfigurationProvider
    {
        SepidarConfiguration Get();
    }
}
