namespace SalesManagement.Application.Features.Items.GetItems
{
    public record GetItemsResult(IReadOnlyList<ItemListItem> Items);

    /// <summary>
    /// ردیفِ سبکِ فهرست کاتالوگ — بدون آرایه‌ی موجودی/قیمت؛ به‌جایشان
    /// <see cref="TotalQuantity"/> و <see cref="MainPrice"/> (دو عدد).
    /// </summary>
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
