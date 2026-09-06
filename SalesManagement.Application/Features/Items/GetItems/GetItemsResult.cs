namespace SalesManagement.Application.Features.Items.GetItems
{
    public record GetItemsResult(IReadOnlyList<ItemListItem> Items);

    public record ItemListItem(
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

    public record ItemGroupingInfo(int GroupingId, string? Code, string? Title, int? ParentGroupRef);

    /// <summary>ویژگی‌های کالا از جدول INV.ItemPropertyAmount (هر کالا یک ردیف).</summary>
    public record ItemPropertiesInfo(
        int ItemPropertyAmountId,
        string? BodyMaterial,     // PropertyAmount1  - جنس بدنه
        string? BodyColor,        // PropertyAmount2  - رنگ بدنه
        string? OpticalSpectrum,  // PropertyAmount3  - طیف نوری
        string? ProductModel,     // PropertyAmount4  - مدل محصول
        string? Power,            // PropertyAmount5  - توان
        string? Coverage,         // PropertyAmount6  - پوشش
        string? QuantityCarton);  // PropertyAmount10 - تعداد در کارتن

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
