using Dapper;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Items.GetItems;

namespace SalesManagement.Persistence.Features.Items.GetItems
{
    public class ItemReadRepository : IItemReadRepository
    {
        private readonly ISepidarConnectionFactory _connectionFactory;

        public ItemReadRepository(ISepidarConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const string Sql = """
            SELECT i.ItemID AS ItemId, i.Type, i.Code, i.Title, i.UnitRef, i.SecondaryUnitRef, i.UnitsRatio, i.Sellable, i.IsActive, g.GroupingID AS GroupingId, g.Code AS GroupingCode, g.Title AS GroupingTitle, g.ParentGroupRef, ipa.ItemPropertyAmountID AS ItemPropertyAmountId, ipa.PropertyAmount1 AS BodyMaterial, ipa.PropertyAmount2 AS BodyColor, ipa.PropertyAmount3 AS OpticalSpectrum, ipa.PropertyAmount4 AS ProductModel, ipa.PropertyAmount5 AS Power, ipa.PropertyAmount6 AS Coverage, ipa.PropertyAmount10 AS QuantityCarton FROM INV.Item AS i INNER JOIN GNR.Grouping AS g ON g.GroupingID = i.SaleGroupRef OUTER APPLY (SELECT TOP (1) p.ItemPropertyAmountID, p.PropertyAmount1, p.PropertyAmount2, p.PropertyAmount3, p.PropertyAmount4, p.PropertyAmount5, p.PropertyAmount6, p.PropertyAmount10 FROM INV.ItemPropertyAmount AS p WHERE p.ItemRef = i.ItemID ORDER BY p.ItemPropertyAmountID) AS ipa WHERE g.ParentGroupRef > 0 AND EXISTS (SELECT 1 FROM INV.ItemStockSummary AS s WHERE s.ItemRef = i.ItemID) ORDER BY i.Title DESC;
            
            SELECT iss.ItemStockSummaryID AS ItemStockSummaryId, iss.ItemRef, iss.StockRef, st.Title AS StockTitle, iss.UnitRef, ISNULL(iss.InputQuantity, 0) AS InputQuantity, ISNULL(iss.OutputQuantity, 0) AS OutputQuantity, ISNULL(iss.Quantity, 0) AS Quantity, ISNULL(iss.SaleQuantity, 0) AS SaleQuantity, iss.FiscalYearRef FROM INV.ItemStockSummary AS iss INNER JOIN INV.Item AS i ON i.ItemID = iss.ItemRef AND i.UnitRef = iss.UnitRef INNER JOIN INV.Stock AS st ON st.StockID = iss.StockRef AND st.IsActive = 1 WHERE iss.FiscalYearRef = (SELECT MAX(fy.FiscalYearId) FROM FMK.FiscalYear AS fy);
            
            SELECT pni.PriceNoteItemID AS PriceNoteItemId, pni.ItemRef, pni.SaleTypeRef, pni.UnitRef, pni.Fee, pni.Discount FROM SLS.PriceNoteItem AS pni WHERE pni.SaleTypeRef = @SaleTypeRef;
            """;

        public async Task<IReadOnlyList<ItemListItem>> GetItemsAsync(
            int saleTypeRef, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                Sql, new { SaleTypeRef = saleTypeRef }, cancellationToken: cancellationToken);

            using var multi = await connection.QueryMultipleAsync(command);

            var itemRows = (await multi.ReadAsync<ItemRow>()).AsList();
            var summaryRows = (await multi.ReadAsync<StockSummaryRow>()).AsList();
            var priceRows = (await multi.ReadAsync<PriceRow>()).AsList();

            var summariesByItem = summaryRows
                .GroupBy(r => r.ItemRef)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<ItemStockInfo>)g.Select(r => new ItemStockInfo(
                        r.ItemStockSummaryId, r.StockRef, r.StockTitle, r.UnitRef,
                        r.InputQuantity, r.OutputQuantity, r.Quantity, r.SaleQuantity,
                        r.FiscalYearRef)).ToList());

            var pricesByItem = priceRows
                .GroupBy(r => r.ItemRef)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<ItemPriceInfo>)g.Select(r => new ItemPriceInfo(
                        r.PriceNoteItemId, r.SaleTypeRef, r.UnitRef, r.Fee, r.Discount)).ToList());

            IReadOnlyList<ItemStockInfo> noStock = Array.Empty<ItemStockInfo>();
            IReadOnlyList<ItemPriceInfo> noPrice = Array.Empty<ItemPriceInfo>();

            return itemRows.Select(r => new ItemListItem(
                    r.ItemId, r.Type, r.Code, r.Title, r.UnitRef, r.SecondaryUnitRef, r.UnitsRatio,
                    r.Sellable, r.IsActive,
                    new ItemGroupingInfo(r.GroupingId, r.GroupingCode, r.GroupingTitle, r.ParentGroupRef),
                    r.ItemPropertyAmountId is null
                        ? null
                        : new ItemPropertiesInfo(
                            r.ItemPropertyAmountId.Value, r.BodyMaterial, r.BodyColor, r.OpticalSpectrum,
                            r.ProductModel, r.Power, r.Coverage, r.QuantityCarton),
                    summariesByItem.GetValueOrDefault(r.ItemId, noStock),
                    pricesByItem.GetValueOrDefault(r.ItemId, noPrice)))
                .ToList();
        }

        private sealed class ItemRow
        {
            public int ItemId { get; set; }
            public int Type { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public int UnitRef { get; set; }
            public int? SecondaryUnitRef { get; set; }
            public decimal UnitsRatio { get; set; }
            public bool Sellable { get; set; }
            public bool IsActive { get; set; }
            public int GroupingId { get; set; }
            public string? GroupingCode { get; set; }
            public string? GroupingTitle { get; set; }
            public int? ParentGroupRef { get; set; }
            public int? ItemPropertyAmountId { get; set; }
            public string? BodyMaterial { get; set; }
            public string? BodyColor { get; set; }
            public string? OpticalSpectrum { get; set; }
            public string? ProductModel { get; set; }
            public string? Power { get; set; }
            public string? Coverage { get; set; }
            public string? QuantityCarton { get; set; }
        }

        private sealed class StockSummaryRow
        {
            public int ItemStockSummaryId { get; set; }
            public int ItemRef { get; set; }
            public int StockRef { get; set; }
            public string? StockTitle { get; set; }
            public int UnitRef { get; set; }
            public decimal InputQuantity { get; set; }
            public decimal OutputQuantity { get; set; }
            public decimal Quantity { get; set; }
            public decimal SaleQuantity { get; set; }
            public int FiscalYearRef { get; set; }
        }

        private sealed class PriceRow
        {
            public int PriceNoteItemId { get; set; }
            public int ItemRef { get; set; }
            public int SaleTypeRef { get; set; }
            public int UnitRef { get; set; }
            public decimal Fee { get; set; }
            public decimal Discount { get; set; }
        }
    }
}
