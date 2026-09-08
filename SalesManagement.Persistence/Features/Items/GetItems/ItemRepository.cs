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


        private static string GroupingJoin { get; } =
            "INNER JOIN GNR.Grouping AS g ON g.GroupingID = i.SaleGroupRef";

        private static string PropertyApply { get; } =
            "OUTER APPLY (SELECT TOP (1) p.ItemPropertyAmountID, p.PropertyAmount1, p.PropertyAmount2, p.PropertyAmount3, p.PropertyAmount4, p.PropertyAmount5, p.PropertyAmount6, p.PropertyAmount10 FROM INV.ItemPropertyAmount AS p WHERE p.ItemRef = i.ItemID ORDER BY p.ItemPropertyAmountID) AS ipa";

        private static string PropertyColumns { get; } =
            "ipa.ItemPropertyAmountID AS ItemPropertyAmountId, ipa.PropertyAmount1 AS BodyMaterial, ipa.PropertyAmount2 AS BodyColor, ipa.PropertyAmount3 AS OpticalSpectrum, ipa.PropertyAmount4 AS ProductModel, ipa.PropertyAmount5 AS Power, ipa.PropertyAmount6 AS Coverage, ipa.PropertyAmount10 AS QuantityCarton";


        private static string CatalogColumns { get; } =
            $"i.ItemID AS Id, i.Code, i.Title, i.UnitRef, i.Sellable, i.IsActive, g.GroupingID AS GroupingId, g.Code AS GroupingCode, g.Title AS GroupingTitle, g.ParentGroupRef, {PropertyColumns}, ISNULL(stk.TotalQuantity, 0) AS TotalQuantity, prc.MainPrice";

        private static string TotalQuantityApply { get; } =
            "OUTER APPLY (SELECT SUM(ISNULL(iss.Quantity, 0)) AS TotalQuantity FROM INV.ItemStockSummary AS iss INNER JOIN INV.Stock AS st ON st.StockID = iss.StockRef AND st.IsActive = 1 WHERE iss.ItemRef = i.ItemID AND iss.UnitRef = i.UnitRef AND iss.FiscalYearRef = @Fy) AS stk";

        private static string MainPriceApply { get; } =
            "OUTER APPLY (SELECT TOP (1) pni.Fee AS MainPrice FROM SLS.PriceNoteItem AS pni WHERE pni.ItemRef = i.ItemID AND pni.SaleTypeRef = @SaleTypeRef AND pni.UnitRef = i.UnitRef ORDER BY pni.PriceNoteItemID) AS prc";

        private static readonly string SqlCatalogAll = $"""
            DECLARE @Fy INT = (SELECT MAX(fy.FiscalYearId) FROM FMK.FiscalYear AS fy);
            SELECT {CatalogColumns}
            FROM INV.Item AS i
            {GroupingJoin}
            {PropertyApply}
            {TotalQuantityApply}
            {MainPriceApply}
            WHERE g.ParentGroupRef > 0 AND EXISTS (SELECT 1 FROM INV.ItemStockSummary AS s WHERE s.ItemRef = i.ItemID)
            ORDER BY i.Title DESC;
            """;

        private static readonly string SqlCatalogByCategory = $"""
            DECLARE @Fy INT = (SELECT MAX(fy.FiscalYearId) FROM FMK.FiscalYear AS fy);
            SELECT {CatalogColumns}
            FROM INV.Item AS i
            {GroupingJoin}
            {PropertyApply}
            {TotalQuantityApply}
            {MainPriceApply}
            WHERE g.Code = @CategoryCode AND i.IsActive = 1
            ORDER BY i.Title DESC;
            """;


        private static string DetailColumns { get; } =
            $"i.ItemID AS ItemId, i.Type, i.Code, i.Title, i.UnitRef, i.SecondaryUnitRef, i.UnitsRatio, i.Sellable, i.IsActive, g.GroupingID AS GroupingId, g.Code AS GroupingCode, g.Title AS GroupingTitle, g.ParentGroupRef, {PropertyColumns}";

        private static string SummaryColumns { get; } =
            "iss.ItemStockSummaryID AS ItemStockSummaryId, iss.ItemRef, iss.StockRef, st.Title AS StockTitle, iss.UnitRef, ISNULL(iss.InputQuantity, 0) AS InputQuantity, ISNULL(iss.OutputQuantity, 0) AS OutputQuantity, ISNULL(iss.Quantity, 0) AS Quantity, ISNULL(iss.SaleQuantity, 0) AS SaleQuantity, iss.FiscalYearRef";

        private static string PriceColumns { get; } =
            "pni.PriceNoteItemID AS PriceNoteItemId, pni.ItemRef, pni.SaleTypeRef, pni.UnitRef, pni.Fee, pni.Discount";

        private static readonly string SqlDetail = $"""
            SELECT {DetailColumns}
            FROM INV.Item AS i
            {GroupingJoin}
            {PropertyApply}
            WHERE i.ItemID = @ItemId;

            SELECT {SummaryColumns}
            FROM INV.ItemStockSummary AS iss
            INNER JOIN INV.Item AS i ON i.ItemID = iss.ItemRef AND i.UnitRef = iss.UnitRef
            INNER JOIN INV.Stock AS st ON st.StockID = iss.StockRef AND st.IsActive = 1
            WHERE iss.FiscalYearRef = (SELECT MAX(fy.FiscalYearId) FROM FMK.FiscalYear AS fy) AND iss.ItemRef = @ItemId;

            SELECT {PriceColumns}
            FROM SLS.PriceNoteItem AS pni
            WHERE pni.SaleTypeRef = @SaleTypeRef AND pni.ItemRef = @ItemId;
            """;


        public async Task<IReadOnlyList<CatalogItem>> GetCatalogAsync(
            int saleTypeRef, CancellationToken cancellationToken = default) =>
            await ReadCatalogAsync(new CommandDefinition(
                SqlCatalogAll,
                new { SaleTypeRef = saleTypeRef },
                cancellationToken: cancellationToken));

        public async Task<IReadOnlyList<CatalogItem>> GetCatalogByCategoryAsync(
            string categoryCode, int saleTypeRef, CancellationToken cancellationToken = default) =>
            await ReadCatalogAsync(new CommandDefinition(
                SqlCatalogByCategory,
                new { SaleTypeRef = saleTypeRef, CategoryCode = categoryCode },
                cancellationToken: cancellationToken));

        public async Task<Item?> GetByIdAsync(
            int itemId, int saleTypeRef, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                SqlDetail,
                new { ItemId = itemId, SaleTypeRef = saleTypeRef },
                cancellationToken: cancellationToken);

            using var multi = await connection.QueryMultipleAsync(command);

            var itemDtos = (await multi.ReadAsync<ItemDto>()).AsList();
            var summaryDtos = (await multi.ReadAsync<ItemStockSummaryDto>()).AsList();
            var priceDtos = (await multi.ReadAsync<ItemPriceDto>()).AsList();

            if (itemDtos.Count == 0)
                return null;

            return ToEntity(
                itemDtos[0],
                summaryDtos.Select(ToStockSummary).ToList(),
                priceDtos.Select(ToPrice).ToList());
        }

        private async Task<IReadOnlyList<CatalogItem>> ReadCatalogAsync(CommandDefinition command)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.QueryAsync<CatalogItemDto>(command);
            return rows.Select(ToCatalogItem).ToList();
        }

        private static CatalogItem ToCatalogItem(CatalogItemDto d) =>
            CatalogItem.Reconstitute(
                d.Id, d.Code, d.Title, d.UnitRef, d.Sellable, d.IsActive,
                new ItemGrouping(d.GroupingId, d.GroupingCode, d.GroupingTitle, d.ParentGroupRef),
                ToProperties(d.ItemPropertyAmountId, d.BodyMaterial, d.BodyColor, d.OpticalSpectrum,
                    d.ProductModel, d.Power, d.Coverage, d.QuantityCarton),
                d.TotalQuantity, d.MainPrice);

        private static Item ToEntity(
            ItemDto d, IEnumerable<ItemStockSummary> summaries, IEnumerable<ItemPrice> prices) =>
            Item.Reconstitute(
                d.ItemId, d.Type, d.Code, d.Title, d.UnitRef, d.SecondaryUnitRef, d.UnitsRatio,
                d.Sellable, d.IsActive,
                new ItemGrouping(d.GroupingId, d.GroupingCode, d.GroupingTitle, d.ParentGroupRef),
                ToProperties(d.ItemPropertyAmountId, d.BodyMaterial, d.BodyColor, d.OpticalSpectrum,
                    d.ProductModel, d.Power, d.Coverage, d.QuantityCarton),
                summaries, prices);

        private static ItemProperties? ToProperties(
            int? id, string? bodyMaterial, string? bodyColor, string? opticalSpectrum,
            string? productModel, string? power, string? coverage, string? quantityCarton) =>
            id is null
                ? null
                : new ItemProperties(id.Value, bodyMaterial, bodyColor, opticalSpectrum,
                    productModel, power, coverage, quantityCarton);

        private static ItemStockSummary ToStockSummary(ItemStockSummaryDto d) =>
            ItemStockSummary.Reconstitute(
                d.ItemStockSummaryId, d.StockRef, d.StockTitle, d.UnitRef,
                d.InputQuantity, d.OutputQuantity, d.Quantity, d.SaleQuantity, d.FiscalYearRef);

        private static ItemPrice ToPrice(ItemPriceDto d) =>
            new(d.PriceNoteItemId, d.SaleTypeRef, d.UnitRef, d.Fee, d.Discount);
    }
}
