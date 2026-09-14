using SalesManagement.Persistence.ConnectionFactories;
using static SalesManagement.Persistence.Common.PersianText;
using static SalesManagement.Persistence.Common.DapperRows;
using System.Data;
using Dapper;
using SalesManagement.Domain.Features.Items;

namespace SalesManagement.Persistence.Features.Items.QueryItems
{
    public class ItemQueryRepository(ISepidarConnectionFactory connectionFactory) : IItemQueryRepository
    {
        private const string GroupingJoin =
            "INNER JOIN GNR.Grouping AS g ON g.GroupingID = i.SaleGroupRef";

        private const string PropertyApply = """
            OUTER APPLY (
                SELECT TOP (1) p.ItemPropertyAmountID, p.PropertyAmount1, p.PropertyAmount2, p.PropertyAmount3,
                    p.PropertyAmount4, p.PropertyAmount5, p.PropertyAmount6, p.PropertyAmount10
                FROM INV.ItemPropertyAmount AS p
                WHERE p.ItemRef = i.ItemID
                ORDER BY p.ItemPropertyAmountID
            ) AS ipa
            """;

        private const string TotalQuantityApply = """
            OUTER APPLY (
                SELECT SUM(ISNULL(iss.Quantity, 0)) AS TotalQuantity
                FROM INV.ItemStockSummary AS iss
                    INNER JOIN INV.Stock AS st ON st.StockID = iss.StockRef AND st.IsActive = 1
                WHERE iss.ItemRef = i.ItemID AND iss.UnitRef = i.UnitRef AND iss.FiscalYearRef = @Fy
            ) AS stk
            """;

        private const string MainPriceApply = """
            OUTER APPLY (
                SELECT TOP (1) pni.Fee AS MainPrice
                FROM SLS.PriceNoteItem AS pni
                WHERE pni.ItemRef = i.ItemID AND pni.SaleTypeRef = @SaleTypeRef AND pni.UnitRef = i.UnitRef
                ORDER BY pni.PriceNoteItemID
            ) AS prc
            """;

        private static readonly IReadOnlyDictionary<string, string> ItemFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = "i.ItemID",
                ["code"] = "i.Code",
                ["title"] = "i.Title",
                ["unitRef"] = "i.UnitRef",
                ["sellable"] = "i.Sellable",
                ["isActive"] = "i.IsActive",
                ["type"] = "i.Type",
                ["secondaryUnitRef"] = "i.SecondaryUnitRef",
                ["unitsRatio"] = "i.UnitsRatio",
                ["groupingId"] = "g.GroupingID",
                ["groupingCode"] = "g.Code",
                ["groupingTitle"] = "g.Title",
                ["parentGroupRef"] = "g.ParentGroupRef",
                ["itemPropertyAmountId"] = "ipa.ItemPropertyAmountID",
                ["bodyMaterial"] = "ipa.PropertyAmount1",
                ["bodyColor"] = "ipa.PropertyAmount2",
                ["opticalSpectrum"] = "ipa.PropertyAmount3",
                ["productModel"] = "ipa.PropertyAmount4",
                ["power"] = "ipa.PropertyAmount5",
                ["coverage"] = "ipa.PropertyAmount6",
                ["quantityCarton"] = "ipa.PropertyAmount10",
                ["totalQuantity"] = "ISNULL(stk.TotalQuantity, 0)",
                ["mainPrice"] = "prc.MainPrice",
            };

        private const string ItemCountExpr = """
            (SELECT COUNT(*)
             FROM INV.Item AS ci
             WHERE ci.SaleGroupRef = g.GroupingID
                AND EXISTS (SELECT 1 FROM INV.ItemStockSummary AS cex WHERE cex.ItemRef = ci.ItemID))
            """;

        private static readonly IReadOnlyDictionary<string, string> CategoryFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["groupingId"] = "g.GroupingID",
                ["code"] = "g.Code",
                ["title"] = "g.Title",
                ["parentGroupRef"] = "g.ParentGroupRef",
                ["isTopLevel"] = "CAST(CASE WHEN LEN(g.Code) = 1 THEN 1 ELSE 0 END AS BIT)",
                ["itemCount"] = ItemCountExpr,
            };

        private static readonly IReadOnlyDictionary<string, string> ItemSearchableFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = "i.Title",
                ["code"] = "i.Code",
            };

        private static readonly string[] DefaultItemSearchFields = ["title", "code"];

        private static readonly IReadOnlyDictionary<string, string> CategorySearchableFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = "g.Title",
                ["code"] = "g.Code",
            };

        private static readonly string[] DefaultCategorySearchFields = ["title", "code"];

        private static readonly IReadOnlyDictionary<string, string> ItemOrderFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["title"] = "i.Title",
                ["code"] = "i.Code",
                ["totalQuantity"] = "ISNULL(stk.TotalQuantity, 0)",
                ["mainPrice"] = "prc.MainPrice",
            };

        private static readonly IReadOnlyDictionary<string, string> CategoryOrderFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["code"] = "g.Code",
                ["title"] = "g.Title",
            };

        public async Task<(int TotalItems, int TotalCategories,
            IReadOnlyList<IDictionary<string, object?>>? Items,
            IReadOnlyList<IDictionary<string, object?>>? Categories)> QueryAsync(
            ItemQuery query, int saleTypeRef, string categoryEntityType, CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();

            if (query.Id is not null)
            {
                var (total, items, _) = await QueryItemsRootAsync(connection, query, saleTypeRef, cancellationToken);
                return (total, 0, items, null);
            }

            if (query.Scope.Items && query.Scope.Categories)
                return await QueryCategoriesWithItemCountsAsync(
                    connection, query, saleTypeRef, categoryEntityType, cancellationToken);

            if (query.Scope.Categories)
            {
                var (total, categories) = await QueryCategoryRowsAsync(connection, query, categoryEntityType, cancellationToken);
                return (0, total, null, categories);
            }

            var (itemTotal, itemRows, _) = await QueryItemsRootAsync(connection, query, saleTypeRef, cancellationToken);
            return (itemTotal, 0, itemRows, null);
        }

        private async Task<(int, IReadOnlyList<IDictionary<string, object?>>?, IReadOnlyList<IDictionary<string, object?>>?)>
            QueryItemsRootAsync(
                IDbConnection connection, ItemQuery query, int saleTypeRef, CancellationToken ct)
        {
            var p = new DynamicParameters();
            p.Add("@SaleTypeRef", saleTypeRef);
            p.Add("@Skip", query.Skip);
            p.Add("@Take", query.Take);

            var whereClause = string.Empty;
            if (query.Id is { } id)
            {
                p.Add("@Id", id);
                whereClause = "AND i.ItemID = @Id";
            }
            else
            {
                whereClause = "AND g.ParentGroupRef > 0 AND EXISTS (SELECT 1 FROM INV.ItemStockSummary AS ex WHERE ex.ItemRef = i.ItemID)";
                if (query.CategoryCode is { } categoryCode)
                {
                    p.Add("@CategoryCode", categoryCode);
                    whereClause += " AND g.Code = @CategoryCode";
                }
            }

            whereClause += BuildSearch(query.Search, ItemSearchableFields, DefaultItemSearchFields, p);

            var from = $"""
                FROM INV.Item AS i
                    {GroupingJoin}
                    {PropertyApply}
                    {TotalQuantityApply}
                    {MainPriceApply}
                WHERE 1 = 1
                    {whereClause}
                """;

            var sql = $"""
                DECLARE @Fy INT = (SELECT MAX(fy.FiscalYearId) FROM FMK.FiscalYear AS fy);

                SELECT COUNT(*) {from};

                SELECT {BuildSelect(query.Fields, ItemFields, "id")}
                {from}
                ORDER BY {ResolveOrderBy(query, ItemOrderFields, "i.Title", "i.ItemID")}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            var (total, items) = await ReadCountAndRowsAsync(connection, sql, p, ct);

            if (query.Id is not null && items.Count > 0)
                await AttachDetailAsync(connection, items[0], query.Id.Value, saleTypeRef, ct);

            return (total, items, null);
        }

        private async Task<(int TotalItems, int TotalCategories,
            IReadOnlyList<IDictionary<string, object?>>?, IReadOnlyList<IDictionary<string, object?>>?)>
            QueryCategoriesWithItemCountsAsync(
                IDbConnection connection, ItemQuery query, int saleTypeRef, string categoryEntityType,
                CancellationToken ct)
        {
            var (totalCategories, categories) = await QueryCategoryRowsAsync(connection, query, categoryEntityType, ct);
            var totalItems = await CountItemsAsync(connection, query, ct);

            if (categories.Count == 0)
                return (totalItems, totalCategories, null, categories);

            var codes = categories
                .Select(c => c.TryGetValue("code", out var v) ? v as string : null)
                .Where(c => c is not null)
                .Cast<string>()
                .Distinct()
                .ToArray();

            if (codes.Length == 0)
                return (totalItems, totalCategories, null, categories);

            var p = new DynamicParameters();
            p.Add("@SaleTypeRef", saleTypeRef);
            p.Add("@CategoryCodes", codes);

            var whereClause = "AND g.ParentGroupRef > 0 AND EXISTS (SELECT 1 FROM INV.ItemStockSummary AS ex WHERE ex.ItemRef = i.ItemID)";
            whereClause += BuildSearch(query.Search, ItemSearchableFields, DefaultItemSearchFields, p);

            var sql = $"""
                DECLARE @Fy INT = (SELECT MAX(fy.FiscalYearId) FROM FMK.FiscalYear AS fy);

                SELECT {BuildSelect(query.Fields, ItemFields, "id", "groupingCode")}
                FROM INV.Item AS i
                    {GroupingJoin}
                    {PropertyApply}
                    {TotalQuantityApply}
                    {MainPriceApply}
                WHERE g.Code IN @CategoryCodes
                    {whereClause}
                ORDER BY g.Code, i.Title DESC;
                """;

            var items = await ReadRowsAsync(connection, sql, p, ct);

            var itemsByCategory = items
                .GroupBy(r => r.TryGetValue("groupingCode", out var v) ? v as string : null)
                .Where(g => g.Key is not null)
                .ToDictionary(g => g.Key!, g => g.ToList());

            foreach (var category in categories)
            {
                var code = category.TryGetValue("code", out var v) ? v as string : null;
                category["items"] = code is not null && itemsByCategory.TryGetValue(code, out var its)
                    ? its
                    : new List<IDictionary<string, object?>>();
            }

            return (totalItems, totalCategories, null, categories);
        }

        private static async Task<int> CountItemsAsync(IDbConnection connection, ItemQuery query, CancellationToken ct)
        {
            var p = new DynamicParameters();

            var whereClause = "AND g.ParentGroupRef > 0 AND EXISTS (SELECT 1 FROM INV.ItemStockSummary AS ex WHERE ex.ItemRef = i.ItemID)";
            if (query.CategoryCode is { } categoryCode)
            {
                p.Add("@CategoryCode", categoryCode);
                whereClause += " AND g.Code = @CategoryCode";
            }
            whereClause += BuildSearch(query.Search, ItemSearchableFields, DefaultItemSearchFields, p);

            var sql = $"""
                SELECT COUNT(*)
                FROM INV.Item AS i
                    {GroupingJoin}
                WHERE 1 = 1
                    {whereClause};
                """;

            return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, p, cancellationToken: ct));
        }

        private async Task<(int Total, List<IDictionary<string, object?>> Rows)> QueryCategoryRowsAsync(
            IDbConnection connection, ItemQuery query, string categoryEntityType, CancellationToken ct)
        {
            var p = new DynamicParameters();
            p.Add("@EntityType", categoryEntityType);
            p.Add("@Skip", query.Skip);
            p.Add("@Take", query.Take);

            var whereClause = "AND LEN(g.Code) IN (1, 2)";
            if (query.CategoryCode is { } categoryCode)
            {
                p.Add("@CategoryCode", categoryCode);
                whereClause += " AND g.Code = @CategoryCode";
            }
            whereClause += BuildSearch(query.Search, CategorySearchableFields, DefaultCategorySearchFields, p);

            var from = $"""
                FROM GNR.Grouping AS g
                WHERE g.EntityType = @EntityType
                    {whereClause}
                """;

            var sql = $"""
                SELECT COUNT(*) {from};

                SELECT {BuildSelect(query.Fields, CategoryFields, "groupingId", "code")}
                {from}
                ORDER BY {ResolveOrderBy(query, CategoryOrderFields, "g.Code", "g.GroupingID")}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            return await ReadCountAndRowsAsync(connection, sql, p, ct);
        }

        private static async Task AttachDetailAsync(
            IDbConnection connection, IDictionary<string, object?> item, int itemId, int saleTypeRef,
            CancellationToken ct)
        {
            const string stockSql = """
                SELECT iss.ItemStockSummaryID AS [itemStockSummaryId], iss.StockRef AS [stockRef],
                    st.Title AS [stockTitle], iss.UnitRef AS [unitRef],
                    ISNULL(iss.InputQuantity, 0) AS [inputQuantity], ISNULL(iss.OutputQuantity, 0) AS [outputQuantity],
                    ISNULL(iss.Quantity, 0) AS [quantity], ISNULL(iss.SaleQuantity, 0) AS [saleQuantity],
                    iss.FiscalYearRef AS [fiscalYearRef]
                FROM INV.ItemStockSummary AS iss
                    INNER JOIN INV.Item AS i ON i.ItemID = iss.ItemRef AND i.UnitRef = iss.UnitRef
                    INNER JOIN INV.Stock AS st ON st.StockID = iss.StockRef AND st.IsActive = 1
                WHERE iss.FiscalYearRef = (SELECT MAX(fy.FiscalYearId) FROM FMK.FiscalYear AS fy)
                    AND iss.ItemRef = @ItemId;
                """;

            const string priceSql = """
                SELECT pni.PriceNoteItemID AS [priceNoteItemId], pni.SaleTypeRef AS [saleTypeRef],
                    pni.UnitRef AS [unitRef], pni.Fee AS [fee], pni.Discount AS [discount]
                FROM SLS.PriceNoteItem AS pni
                WHERE pni.SaleTypeRef = @SaleTypeRef AND pni.ItemRef = @ItemId;
                """;

            item["stockSummaries"] = await ReadRowsAsync(
                connection, stockSql, new { ItemId = itemId }, ct);
            item["prices"] = await ReadRowsAsync(
                connection, priceSql, new { ItemId = itemId, SaleTypeRef = saleTypeRef }, ct);
        }

        private static string BuildSelect(
            IReadOnlyList<string> requested, IReadOnlyDictionary<string, string> whitelist, params string[] forced)
        {
            var chosen = requested.Where(whitelist.ContainsKey).ToList();

            List<string> names;
            if (chosen.Count == 0)
            {
                names = whitelist.Keys.ToList();
            }
            else
            {
                names = new List<string>();
                foreach (var f in forced)
                    if (whitelist.ContainsKey(f))
                        names.Add(f);
                foreach (var c in chosen)
                    if (!names.Contains(c, StringComparer.OrdinalIgnoreCase))
                        names.Add(c);
            }

            return string.Join(", ", names.Select(n => $"{whitelist[n]} AS [{n}]"));
        }

        private static string BuildSearch(
            ItemSearch? search, IReadOnlyDictionary<string, string> searchable, string[] defaultFields,
            DynamicParameters p)
        {
            if (search is null || string.IsNullOrWhiteSpace(search.Term))
                return string.Empty;

            var known = search.Fields.Where(searchable.ContainsKey).ToList();
            var names = known.Count > 0 ? known : defaultFields.ToList();

            p.Add("@s", "%" + NormalizeFa(search.Term) + "%");

            var ors = names.Select(n =>
                $"{FaNormExpr($"CAST({searchable[n]} AS nvarchar(400))")} LIKE @s");

            return " AND (" + string.Join(" OR ", ors) + ")";
        }

        private static string ResolveOrderBy(
            ItemQuery query, IReadOnlyDictionary<string, string> fields, string fallbackExpr, string tieBreaker)
        {
            var dir = query.Descending ? "DESC" : "ASC";

            if (query.OrderBy is not null && fields.TryGetValue(query.OrderBy, out var expr))
                return $"{expr} {dir}, {tieBreaker} ASC";

            return $"{fallbackExpr} {dir}, {tieBreaker} ASC";
        }
    }
}
