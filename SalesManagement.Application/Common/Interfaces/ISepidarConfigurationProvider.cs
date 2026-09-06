using SalesManagement.Application.Common.Configuration;

namespace SalesManagement.Application.Common.Interfaces
{
    /// <summary>مقادیر پیش‌فرض سپیدار را از فایل پیکربندی می‌خواند (یک‌بار خوانده و کش می‌شود).</summary>
    public interface ISepidarConfigurationProvider
    {
        SepidarConfiguration Get();
    }
}
