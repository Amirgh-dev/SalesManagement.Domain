namespace SalesManagement.Application.Features.Items.QueryItems
{
    public record QueryItemsResult(
        int TotalItems,
        int TotalCategories,
        IReadOnlyList<IDictionary<string, object?>>? Items,
        IReadOnlyList<IDictionary<string, object?>>? Categories);
}
