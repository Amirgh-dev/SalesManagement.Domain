namespace SalesManagement.Application.Features.Items.GetItems
{
    public record GetItemsResult(IReadOnlyList<ItemListItem> Items);

    public record ItemListItem(
        int ItemId,
        string Code,
        string Title,
        int UnitRef,
        bool Sellable,
        bool IsActive,
        ItemGroupingInfo Grouping,
        ItemPropertiesInfo? Properties,
        decimal TotalQuantity,
        decimal? MainPrice);
}
