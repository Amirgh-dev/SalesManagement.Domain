namespace SalesManagement.Domain.Features.Items
{
    public interface IItemQueryRepository
    {
        Task<(int TotalItems,
            int TotalCategories,
            IReadOnlyList<IDictionary<string, object?>>? Items,
            IReadOnlyList<IDictionary<string, object?>>? Categories)> QueryAsync(
            ItemQuery query, int saleTypeRef, string categoryEntityType, CancellationToken cancellationToken = default);
    }
}
