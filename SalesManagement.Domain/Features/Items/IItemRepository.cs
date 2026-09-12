
namespace SalesManagement.Domain.Features.Items
{
    public interface IItemRepository
    {
        Task<IReadOnlyList<CatalogItem>> GetCatalogAsync(
            int saleTypeRef, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CatalogItem>> GetCatalogByCategoryAsync(
            string categoryCode, int saleTypeRef, CancellationToken cancellationToken = default);

        Task<Item?> GetByIdAsync(
            int itemId, int saleTypeRef, CancellationToken cancellationToken = default);
    }
}
