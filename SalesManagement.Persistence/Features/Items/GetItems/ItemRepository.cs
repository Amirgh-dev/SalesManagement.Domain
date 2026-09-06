using Dapper;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Features.Items;
using SalesManagement.Domain.ValueObjects;
using SalesManagement.Persistence.Features.Items.GetItems.Dtos;

namespace SalesManagement.Persistence.Features.Items.GetItems
{
    public class ItemRepository : IItemRepository
    {
        private readonly ISepidarConnectionFactory _connectionFactory;

        public ItemRepository(ISepidarConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // @ItemId = NULL  → همه‌ی کالاها  |  @ItemId = <id>  → فقط همان کالا
        private const string SqlByItem = """
            SELECT i.ItemID AS ItemId, i.Type, i.Code, i.Title, i.UnitRef, i.SecondaryUnitRef, i.UnitsRatio, i.Sellable, i.IsActive, g.GroupingID AS GroupingId, g.Code AS GroupingCode, g.Title AS GroupingTitle, g.ParentGroupRef, ipa.ItemPropertyAmountID AS ItemPropertyAmountId, ipa.PropertyAmount1 AS BodyMaterial, ipa.PropertyAmount2 AS BodyColor, ipa.PropertyAmount3 AS OpticalSpectrum, ipa.PropertyAmount4 AS ProductModel, ipa.PropertyAmount5 AS Power, ipa.PropertyAmount6 AS Coverage, ipa.PropertyAmount10 AS QuantityCarton FROM INV.Item AS i INNER JOIN GNR.Grouping AS g ON g.GroupingID = i.SaleGroupRef OUTER APPLY (SELECT TOP (1) p.ItemPropertyAmountID, p.PropertyAmount1, p.PropertyAmount2, p.PropertyAmount3, p.PropertyAmount4, p.PropertyAmount5, p.PropertyAmount6, p.PropertyAmount10 FROM INV.ItemPropertyAmount AS p WHERE p.ItemRef = i.ItemID ORDER BY p.ItemPropertyAmountID) AS ipa WHERE g.ParentGroupRef > 0 AND EXISTS (SELECT 1 FROM INV.ItemStockSummary AS s WHERE s.ItemRef = i.ItemID) AND (@ItemId IS NULL OR i.ItemID = @ItemId) ORDER BY i.Title DESC;

            SELECT iss.ItemStockSummaryID AS ItemStockSummaryId, iss.ItemRef, iss.StockRef, st.Title AS StockTitle, iss.UnitRef, ISNULL(iss.InputQuantity, 0) AS InputQuantity, ISNULL(iss.OutputQuantity, 0) AS OutputQuantity, ISNULL(iss.Quantity, 0) AS Quantity, ISNULL(iss.SaleQuantity, 0) AS SaleQuantity, iss.FiscalYearRef FROM INV.ItemStockSummary AS iss INNER JOIN INV.Item AS i ON i.ItemID = iss.ItemRef AND i.UnitRef = iss.UnitRef INNER JOIN INV.Stock AS st ON st.StockID = iss.StockRef AND st.IsActive = 1 WHERE iss.FiscalYearRef = (SELECT MAX(fy.FiscalYearId) FROM FMK.FiscalYear AS fy) AND (@ItemId IS NULL OR iss.ItemRef = @ItemId);

            SELECT pni.PriceNoteItemID AS PriceNoteItemId, pni.ItemRef, pni.SaleTypeRef, pni.UnitRef, pni.Fee, pni.Discount FROM SLS.PriceNoteItem AS pni WHERE pni.SaleTypeRef = @SaleTypeRef AND (@ItemId IS NULL OR pni.ItemRef = @ItemId);
            """;

        // کالاهای فعالِ یک دسته بر اساس کدِ گروه فروش
        private const string SqlByCategory = """
            SELECT i.ItemID AS ItemId, i.Type, i.Code, i.Title, i.UnitRef, i.SecondaryUnitRef, i.UnitsRatio, i.Sellable, i.IsActive, g.GroupingID AS GroupingId, g.Code AS GroupingCode, g.Title AS GroupingTitle, g.ParentGroupRef, ipa.ItemPropertyAmountID AS ItemPropertyAmountId, ipa.PropertyAmount1 AS BodyMaterial, ipa.PropertyAmount2 AS BodyColor, ipa.PropertyAmount3 AS OpticalSpectrum, ipa.PropertyAmount4 AS ProductModel, ipa.PropertyAmount5 AS Power, ipa.PropertyAmount6 AS Coverage, ipa.PropertyAmount10 AS QuantityCarton FROM INV.Item AS i INNER JOIN GNR.Grouping AS g ON g.GroupingID = i.SaleGroupRef OUTER APPLY (SELECT TOP (1) p.ItemPropertyAmountID, p.PropertyAmount1, p.PropertyAmount2, p.PropertyAmount3, p.PropertyAmount4, p.PropertyAmount5, p.PropertyAmount6, p.PropertyAmount10 FROM INV.ItemPropertyAmount AS p WHERE p.ItemRef = i.ItemID ORDER BY p.ItemPropertyAmountID) AS ipa WHERE g.Code = @CategoryCode AND i.IsActive = 1 ORDER BY i.Title DESC;

            SELECT iss.ItemStockSummaryID AS ItemStockSummaryId, iss.ItemRef, iss.StockRef, st.Title AS StockTitle, iss.UnitRef, ISNULL(iss.InputQuantity, 0) AS InputQuantity, ISNULL(iss.OutputQuantity, 0) AS OutputQuantity, ISNULL(iss.Quantity, 0) AS Quantity, ISNULL(iss.SaleQuantity, 0) AS SaleQuantity, iss.FiscalYearRef FROM INV.ItemStockSummary AS iss INNER JOIN INV.Item AS i ON i.ItemID = iss.ItemRef AND i.UnitRef = iss.UnitRef INNER JOIN INV.Stock AS st ON st.StockID = iss.StockRef AND st.IsActive = 1 INNER JOIN GNR.Grouping AS g ON g.GroupingID = i.SaleGroupRef WHERE iss.FiscalYearRef = (SELECT MAX(fy.FiscalYearId) FROM FMK.FiscalYear AS fy) AND g.Code = @CategoryCode AND i.IsActive = 1;

            SELECT pni.PriceNoteItemID AS PriceNoteItemId, pni.ItemRef, pni.SaleTypeRef, pni.UnitRef, pni.Fee, pni.Discount FROM SLS.PriceNoteItem AS pni INNER JOIN INV.Item AS i ON i.ItemID = pni.ItemRef INNER JOIN GNR.Grouping AS g ON g.GroupingID = i.SaleGroupRef WHERE pni.SaleTypeRef = @SaleTypeRef AND g.Code = @CategoryCode AND i.IsActive = 1;
            """;

        public async Task<IReadOnlyList<Item>> GetAllAsync(
            int saleTypeRef, CancellationToken cancellationToken = default) =>
            await ReadItemsAsync(new CommandDefinition(
                SqlByItem,
                new { SaleTypeRef = saleTypeRef, ItemId = (int?)null },
                cancellationToken: cancellationToken));

        public async Task<Item?> GetByIdAsync(
            int itemId, int saleTypeRef, CancellationToken cancellationToken = default)
        {
            var items = await ReadItemsAsync(new CommandDefinition(
                SqlByItem,
                new { SaleTypeRef = saleTypeRef, ItemId = (int?)itemId },
                cancellationToken: cancellationToken));

            return items.Count > 0 ? items[0] : null;
        }

        public async Task<IReadOnlyList<Item>> GetByCategoryCodeAsync(
            string categoryCode, int saleTypeRef, CancellationToken cancellationToken = default) =>
            await ReadItemsAsync(new CommandDefinition(
                SqlByCategory,
                new { SaleTypeRef = saleTypeRef, CategoryCode = categoryCode },
                cancellationToken: cancellationToken));

        private async Task<IReadOnlyList<Item>> ReadItemsAsync(CommandDefinition command)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var multi = await connection.QueryMultipleAsync(command);

            var itemDtos = (await multi.ReadAsync<ItemDto>()).AsList();
            var summaryDtos = (await multi.ReadAsync<ItemStockSummaryDto>()).AsList();
            var priceDtos = (await multi.ReadAsync<ItemPriceDto>()).AsList();

            var summariesByItem = summaryDtos
                .GroupBy(d => d.ItemRef)
                .ToDictionary(g => g.Key, g => g.Select(ToStockSummary).ToList());

            var pricesByItem = priceDtos
                .GroupBy(d => d.ItemRef)
                .ToDictionary(g => g.Key, g => g.Select(ToPrice).ToList());

            var noStock = new List<ItemStockSummary>();
            var noPrice = new List<ItemPrice>();

            return itemDtos
                .Select(d => ToEntity(
                    d,
                    summariesByItem.GetValueOrDefault(d.ItemId, noStock),
                    pricesByItem.GetValueOrDefault(d.ItemId, noPrice)))
                .ToList();
        }

        private static Item ToEntity(
            ItemDto d, IEnumerable<ItemStockSummary> summaries, IEnumerable<ItemPrice> prices) =>
            Item.Reconstitute(
                d.ItemId, d.Type, d.Code, d.Title, d.UnitRef, d.SecondaryUnitRef, d.UnitsRatio,
                d.Sellable, d.IsActive,
                new ItemGrouping(d.GroupingId, d.GroupingCode, d.GroupingTitle, d.ParentGroupRef),
                d.ItemPropertyAmountId is null
                    ? null
                    : new ItemProperties(
                        d.ItemPropertyAmountId.Value, d.BodyMaterial, d.BodyColor, d.OpticalSpectrum,
                        d.ProductModel, d.Power, d.Coverage, d.QuantityCarton),
                summaries, prices);

        private static ItemStockSummary ToStockSummary(ItemStockSummaryDto d) =>
            ItemStockSummary.Reconstitute(
                d.ItemStockSummaryId, d.StockRef, d.StockTitle, d.UnitRef,
                d.InputQuantity, d.OutputQuantity, d.Quantity, d.SaleQuantity, d.FiscalYearRef);

        private static ItemPrice ToPrice(ItemPriceDto d) =>
            new(d.PriceNoteItemId, d.SaleTypeRef, d.UnitRef, d.Fee, d.Discount);
    }
}
