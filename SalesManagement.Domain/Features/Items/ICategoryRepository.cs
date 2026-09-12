
namespace SalesManagement.Domain.Features.Items
{
    public interface ICategoryRepository
    {
        Task<IReadOnlyList<Category>> GetSaleGroupsAsync(
            string entityType, CancellationToken cancellationToken = default);
    }
}
