namespace SalesManagement.Application.Features.Items.GetItemById
{
    public record GetItemByIdResult(
        int ItemId,
        int Type,
        string Code,
        string Title,
        int UnitRef,
        int? SecondaryUnitRef,
        decimal UnitsRatio,
        bool Sellable,
        bool IsActive,
        ItemGroupingInfo Grouping,
        ItemPropertiesInfo? Properties,
        IReadOnlyList<ItemStockInfo> StockSummaries,
        IReadOnlyList<ItemPriceInfo> Prices);

    public record ItemStockInfo(
        int ItemStockSummaryId,
        int StockRef,
        string? StockTitle,
        int UnitRef,
        decimal InputQuantity,
        decimal OutputQuantity,
        decimal Quantity,
        decimal SaleQuantity,
        int FiscalYearRef);

    public record ItemPriceInfo(
        int PriceNoteItemId,
        int SaleTypeRef,
        int UnitRef,
        decimal Fee,
        decimal Discount);
}
