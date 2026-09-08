using SalesManagement.Application.Features.Items.GetItemById;
using SalesManagement.Application.Features.Items.GetItems;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.ValueObjects;

namespace SalesManagement.Application.Features.Items
{
    internal static class ItemResultMapper
    {
        public static ItemListItem ToListItem(CatalogItem item) => new(
            item.Id,
            item.Code,
            item.Title,
            item.UnitRef,
            item.Sellable,
            item.IsActive,
            ToGroupingInfo(item.Grouping),
            ToPropertiesInfo(item.Properties),
            item.TotalQuantity,
            item.MainPrice);

        public static ItemDetail ToDetail(Item item) => new(
            item.Id,
            item.Type,
            item.Code,
            item.Title,
            item.UnitRef,
            item.SecondaryUnitRef,
            item.UnitsRatio,
            item.Sellable,
            item.IsActive,
            ToGroupingInfo(item.Grouping),
            ToPropertiesInfo(item.Properties),
            item.StockSummaries
                .Select(s => new ItemStockInfo(
                    s.ItemStockSummaryId, s.StockRef, s.StockTitle, s.UnitRef,
                    s.InputQuantity, s.OutputQuantity, s.Quantity, s.SaleQuantity, s.FiscalYearRef))
                .ToList(),
            item.Prices
                .Select(p => new ItemPriceInfo(
                    p.PriceNoteItemId, p.SaleTypeRef, p.UnitRef, p.Fee, p.Discount))
                .ToList());

        private static ItemGroupingInfo ToGroupingInfo(ItemGrouping g) =>
            new(g.GroupingId, g.Code, g.Title, g.ParentGroupRef);

        private static ItemPropertiesInfo? ToPropertiesInfo(ItemProperties? p) =>
            p is null
                ? null
                : new ItemPropertiesInfo(
                    p.ItemPropertyAmountId, p.BodyMaterial, p.BodyColor, p.OpticalSpectrum,
                    p.ProductModel, p.Power, p.Coverage, p.QuantityCarton);
    }
}
