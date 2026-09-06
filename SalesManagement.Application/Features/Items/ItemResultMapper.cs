using SalesManagement.Application.Features.Items.GetItems;
using SalesManagement.Domain.Entities;

namespace SalesManagement.Application.Features.Items
{
    /// <summary>نگاشت موجودیت دامنه‌ی <see cref="Item"/> به مدل خروجی API.</summary>
    internal static class ItemResultMapper
    {
        public static ItemListItem ToListItem(Item item) => new(
            item.Id,
            item.Type,
            item.Code,
            item.Title,
            item.UnitRef,
            item.SecondaryUnitRef,
            item.UnitsRatio,
            item.Sellable,
            item.IsActive,
            new ItemGroupingInfo(
                item.Grouping.GroupingId, item.Grouping.Code,
                item.Grouping.Title, item.Grouping.ParentGroupRef),
            item.Properties is null
                ? null
                : new ItemPropertiesInfo(
                    item.Properties.ItemPropertyAmountId,
                    item.Properties.BodyMaterial,
                    item.Properties.BodyColor,
                    item.Properties.OpticalSpectrum,
                    item.Properties.ProductModel,
                    item.Properties.Power,
                    item.Properties.Coverage,
                    item.Properties.QuantityCarton),
            item.StockSummaries
                .Select(s => new ItemStockInfo(
                    s.ItemStockSummaryId, s.StockRef, s.StockTitle, s.UnitRef,
                    s.InputQuantity, s.OutputQuantity, s.Quantity, s.SaleQuantity, s.FiscalYearRef))
                .ToList(),
            item.Prices
                .Select(p => new ItemPriceInfo(
                    p.PriceNoteItemId, p.SaleTypeRef, p.UnitRef, p.Fee, p.Discount))
                .ToList());
    }
}
