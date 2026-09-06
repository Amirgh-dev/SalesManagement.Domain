using SalesManagement.Domain.Entities;

namespace SalesManagement.Domain.Features.Items
{
    public interface ICategoryRepository
    {
        /// <summary>دسته‌های فروش کالا (سطح ۱ و ۲) برای EntityType داده‌شده.</summary>
        Task<IReadOnlyList<Category>> GetSaleGroupsAsync(
            string entityType, CancellationToken cancellationToken = default);
    }
}
