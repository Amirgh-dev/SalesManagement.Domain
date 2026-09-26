using SalesManagement.Persistence.ConnectionFactories;
using SalesManagement.Persistence.Common;
using static SalesManagement.Persistence.Common.PersianText;
using static SalesManagement.Persistence.Common.DapperRows;
using static SalesManagement.Persistence.Common.SqlFilter;
using System.Data;
using Dapper;
using SalesManagement.Domain.Features.Quotations;

namespace SalesManagement.Persistence.Features.Quotations.QueryQuotations
{
    public class QuotationQueryRepository(ISepidarConnectionFactory connectionFactory) : IQuotationQueryRepository
    {
        private static readonly IReadOnlyDictionary<string, (string Expr, Kind Kind)> QuotationFields =
            new Dictionary<string, (string, Kind)>(StringComparer.OrdinalIgnoreCase)
            {
                ["quotationId"] = ("q.QuotationId", Kind.Int),
                ["number"] = ("q.Number", Kind.Int),
                ["date"] = ("q.Date", Kind.DateTime),
                ["creationDate"] = ("q.CreationDate", Kind.DateTime),
                ["expirationDate"] = ("q.ExpirationDate", Kind.DateTime),
                ["customerPartyRef"] = ("q.CustomerPartyRef", Kind.Int),
                ["customerName"] = ("q.CustomerRealName", Kind.String),
                ["saleTypeRef"] = ("q.SaleTypeRef", Kind.Int),
                ["saleTypeTitle"] = ("(SELECT st.Title FROM SLS.SaleType AS st WHERE st.SaleTypeId = q.SaleTypeRef)", Kind.String),
                ["price"] = ("q.Price", Kind.Decimal),
                ["discount"] = ("q.Discount", Kind.Decimal),
                ["tax"] = ("q.Tax", Kind.Decimal),
                ["netPrice"] = ("q.NetPrice", Kind.Decimal),
                ["closed"] = ("q.Closed", Kind.Bool),
            };

        private static readonly IReadOnlyDictionary<string, (string Table, string Column, Kind Kind)> ItemFields =
            new Dictionary<string, (string, string, Kind)>(StringComparer.OrdinalIgnoreCase)
            {
                ["itemId"] = ("QI", "QuotationItemID", Kind.Int),
                ["itemRowId"] = ("QI", "RowID", Kind.Int),
                ["itemRef"] = ("QI", "ItemRef", Kind.Int),
                ["itemCode"] = ("IT", "Code", Kind.String),
                ["itemTitle"] = ("IT", "Title", Kind.String),
                ["itemQuantity"] = ("QI", "Quantity", Kind.Decimal),
                ["itemFee"] = ("QI", "Fee", Kind.Decimal),
                ["itemPrice"] = ("QI", "Price", Kind.Decimal),
                ["itemDiscount"] = ("QI", "Discount", Kind.Decimal),
                ["itemNetPrice"] = ("QI", "NetPrice", Kind.Decimal),
                ["itemDescription"] = ("QI", "Description", Kind.String),
            };

        private const string VendorDeclare = VendorScopeSql.Declare;

        public async Task<(int Total, IReadOnlyList<IDictionary<string, object?>>? Quotations, IReadOnlyList<IDictionary<string, object?>>? Items)> QueryAsync(
            int partyId, QuotationQuery query, CancellationToken cancellationToken = default)
        {
            var p = new DynamicParameters();
            p.Add("@PartyId", partyId);
            p.Add("@Skip", query.Skip);
            p.Add("@Take", query.Take);

            var (quotationWhere, itemWhereMain, itemWhereExists) = BuildFilters(query.Filters, p);
            var (searchQuotationCtx, searchItemsJoined, searchItemsNested) = BuildSearch(query.Search, p);

            using var connection = connectionFactory.CreateConnection();

            return query.Scope switch
            {
                QuotationScope.Items => await QueryItemsOnlyAsync(
                    connection, query, p, quotationWhere, itemWhereMain, searchItemsJoined, cancellationToken),
                QuotationScope.Quotations => await QueryQuotationsOnlyAsync(
                    connection, query, p, quotationWhere, itemWhereExists, searchQuotationCtx, cancellationToken),
                _ => await QueryBothAsync(
                    connection, query, p, quotationWhere, itemWhereExists, searchQuotationCtx, searchItemsNested,
                    query.Filters, cancellationToken),
            };
        }

        private async Task<(int Total, IReadOnlyList<IDictionary<string, object?>>? Quotations, IReadOnlyList<IDictionary<string, object?>>? Items)> QueryQuotationsOnlyAsync(
            IDbConnection connection, QuotationQuery query, DynamicParameters p,
            string quotationWhere, string itemWhereExists, string searchClause, CancellationToken ct)
        {
            var from = $"""
                FROM GNR.Grouping AS g
                    INNER JOIN GNR.Party AS p ON p.CustomerGroupingRef = g.GroupingID
                    INNER JOIN SLS.Quotation AS q ON q.CustomerPartyRef = p.PartyId
                WHERE g.Code = @VendorCode
                    {quotationWhere}
                    {ItemExistsClause(itemWhereExists)}
                    {searchClause}
                """;

            var sql = $"""
                {VendorDeclare}

                SELECT COUNT(*) {from};

                SELECT {BuildQuotationSelect(query.Fields)} {from}
                ORDER BY {ResolveOrderBy(query, quotation: true)}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            var (total, quotations) = await ReadCountAndRowsAsync(connection, sql, p, ct);
            return (total, quotations, null);
        }

        private async Task<(int Total, IReadOnlyList<IDictionary<string, object?>>? Quotations, IReadOnlyList<IDictionary<string, object?>>? Items)> QueryItemsOnlyAsync(
            IDbConnection connection, QuotationQuery query, DynamicParameters p,
            string quotationWhere, string itemWhereMain, string searchClause, CancellationToken ct)
        {
            var from = $"""
                FROM GNR.Grouping AS g
                    INNER JOIN GNR.Party AS p ON p.CustomerGroupingRef = g.GroupingID
                    INNER JOIN SLS.Quotation AS q ON q.CustomerPartyRef = p.PartyId
                    INNER JOIN SLS.QuotationItem AS qi ON qi.QuotationRef = q.QuotationId
                    INNER JOIN INV.Item AS it ON it.ItemID = qi.ItemRef
                WHERE g.Code = @VendorCode
                    {quotationWhere}
                    {itemWhereMain}
                    {searchClause}
                """;

            var sql = $"""
                {VendorDeclare}

                SELECT COUNT(*) {from};

                SELECT {BuildItemSelect(query.Fields)} {from}
                ORDER BY {ResolveOrderBy(query, quotation: false)}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            var (total, items) = await ReadCountAndRowsAsync(connection, sql, p, ct);
            return (total, null, items);
        }

        private async Task<(int Total, IReadOnlyList<IDictionary<string, object?>>? Quotations, IReadOnlyList<IDictionary<string, object?>>? Items)> QueryBothAsync(
            IDbConnection connection, QuotationQuery query, DynamicParameters p,
            string quotationWhere, string itemWhereExists, string searchClause, string searchItemsNested,
            IReadOnlyList<QuotationFilter> filters, CancellationToken ct)
        {
            var from = $"""
                FROM GNR.Grouping AS g
                    INNER JOIN GNR.Party AS p ON p.CustomerGroupingRef = g.GroupingID
                    INNER JOIN SLS.Quotation AS q ON q.CustomerPartyRef = p.PartyId
                WHERE g.Code = @VendorCode
                    {quotationWhere}
                    {ItemExistsClause(itemWhereExists)}
                    {searchClause}
                """;

            var quotationSql = $"""
                {VendorDeclare}

                SELECT COUNT(*) {from};

                SELECT {BuildQuotationSelect(query.Fields)} {from}
                ORDER BY {ResolveOrderBy(query, quotation: true)}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            var (total, quotations) = await ReadCountAndRowsAsync(connection, quotationSql, p, ct);

            var quotationIds = quotations
                .Select(r => r.TryGetValue("quotationId", out var v) && v is not null ? Convert.ToInt32(v) : 0)
                .Where(id => id != 0)
                .Distinct()
                .ToArray();

            if (quotationIds.Length == 0)
                return (total, quotations, null);

            var itemFilters = filters.Where(f => ItemFields.ContainsKey(f.Field)).ToList();
            var itemParams = new DynamicParameters();
            itemParams.Add("@QuotationIds", quotationIds);
            var itemConds = BuildItemConditions(itemFilters, itemParams, "qi", "it");

            if (!string.IsNullOrEmpty(searchItemsNested) && query.Search is not null)
                itemParams.Add("@s", "%" + NormalizeFa(query.Search.Term) + "%");

            var itemSql = $"""
                SELECT {BuildItemSelect(query.Fields)}
                FROM SLS.QuotationItem AS qi
                    INNER JOIN INV.Item AS it ON it.ItemID = qi.ItemRef
                WHERE qi.QuotationRef IN @QuotationIds
                    {itemConds}
                    {searchItemsNested}
                ORDER BY qi.QuotationRef, qi.RowID;
                """;

            var items = await ReadRowsAsync(connection, itemSql, itemParams, ct);

            var itemsByQuotation = items
                .GroupBy(r => r.TryGetValue("quotationRef", out var v) && v is not null ? Convert.ToInt32(v) : 0)
                .ToDictionary(gr => gr.Key, gr => gr.ToList());

            foreach (var q in quotations)
            {
                var id = q.TryGetValue("quotationId", out var v) && v is not null ? Convert.ToInt32(v) : 0;
                q["items"] = itemsByQuotation.TryGetValue(id, out var its)
                    ? its
                    : new List<IDictionary<string, object?>>();
            }

            return (total, quotations, null);
        }

        private static string BuildQuotationSelect(IReadOnlyList<string> fields)
        {
            var valid = fields.Where(QuotationFields.ContainsKey).ToList();
            var names = valid.Count > 0 ? valid : QuotationFields.Keys.ToList();

            var cols = new List<string> { "q.QuotationId AS [quotationId]" };
            cols.AddRange(names
                .Where(n => !n.Equals("quotationId", StringComparison.OrdinalIgnoreCase))
                .Select(n => $"{QuotationFields[n].Expr} AS [{n}]"));
            return string.Join(", ", cols);
        }

        private static string BuildItemSelect(IReadOnlyList<string> fields)
        {
            var valid = fields.Where(ItemFields.ContainsKey).ToList();
            var names = valid.Count > 0 ? valid : ItemFields.Keys.ToList();

            var cols = new List<string> { "qi.QuotationRef AS [quotationRef]" };
            cols.AddRange(names.Select(n =>
            {
                var (table, column, _) = ItemFields[n];
                var alias = table == "QI" ? "qi" : "it";
                return $"{alias}.{column} AS [{n}]";
            }));
            return string.Join(", ", cols);
        }

        private static string ResolveOrderBy(QuotationQuery query, bool quotation)
        {
            var dir = query.Descending ? "DESC" : "ASC";
            var fallback = quotation ? $"q.Number {dir}" : $"qi.QuotationRef {dir}, qi.RowID {dir}";

            if (query.OrderBy is null)
                return fallback;

            if (quotation)
                return QuotationFields.TryGetValue(query.OrderBy, out var f)
                    ? WithTieBreakers(query.OrderBy, f.Expr, dir)
                    : fallback;

            if (query.OrderBy.Equals("quotationRef", StringComparison.OrdinalIgnoreCase))
                return $"qi.QuotationRef {dir}";
            if (ItemFields.TryGetValue(query.OrderBy, out var it))
            {
                var a = it.Table == "QI" ? "qi" : "it";
                return $"{a}.{it.Column} {dir}";
            }
            return fallback;
        }

        private static string WithTieBreakers(string field, string expr, string dir)
        {
            var parts = new List<string> { $"{expr} {dir}" };

            if (!field.Equals("creationDate", StringComparison.OrdinalIgnoreCase))
                parts.Add($"q.CreationDate {dir}");

            if (!field.Equals("quotationId", StringComparison.OrdinalIgnoreCase))
                parts.Add($"q.QuotationId {dir}");

            return string.Join(", ", parts);
        }

        private static string ItemExistsClause(string itemWhereExists) =>
            string.IsNullOrEmpty(itemWhereExists)
                ? string.Empty
                : $"""
                    AND EXISTS (
                        SELECT 1
                        FROM SLS.QuotationItem AS qi2
                            INNER JOIN INV.Item AS it2 ON it2.ItemID = qi2.ItemRef
                        WHERE qi2.QuotationRef = q.QuotationId
                        {itemWhereExists})
                    """;

        private static (string QuotationWhere, string ItemWhereMain, string ItemWhereExists) BuildFilters(
            IReadOnlyList<QuotationFilter> filters, DynamicParameters p)
        {
            var quotationFilters = filters.Where(f => QuotationFields.ContainsKey(f.Field)).ToList();
            var itemFilters = filters.Where(f => ItemFields.ContainsKey(f.Field)).ToList();

            var quotationWhere = BuildQuotationConditions(quotationFilters, p);
            var itemWhereMain = BuildItemConditions(itemFilters, p, "qi", "it");
            var itemWhereExists = BuildItemConditions(itemFilters, p, "qi2", "it2");

            return (quotationWhere, itemWhereMain, itemWhereExists);
        }

        private static (string QuotationContext, string ItemsJoined, string ItemsNested) BuildSearch(
            QuotationSearch? search, DynamicParameters p)
        {
            if (search is null || string.IsNullOrWhiteSpace(search.Term))
                return (string.Empty, string.Empty, string.Empty);

            var known = search.Fields
                .Where(f => QuotationFields.ContainsKey(f) || ItemFields.ContainsKey(f))
                .ToList();

            List<string> qNames, itemNames;
            if (known.Count == 0)
            {
                qNames = QuotationFields.Keys.ToList();
                itemNames = ItemFields.Keys.ToList();
            }
            else
            {
                qNames = known.Where(QuotationFields.ContainsKey).ToList();
                itemNames = known.Where(ItemFields.ContainsKey).ToList();
            }

            p.Add("@s", "%" + NormalizeFa(search.Term) + "%");

            string QOr() => string.Join(" OR ",
                qNames.Select(n => LikeExpr(QuotationFields[n].Expr, QuotationFields[n].Kind)));

            string ItemOr(string qi, string it) => string.Join(" OR ", itemNames.Select(n =>
            {
                var (table, column, kind) = ItemFields[n];
                return LikeExpr($"{(table == "QI" ? qi : it)}.{column}", kind);
            }));

            var quotationParts = new List<string>();
            if (qNames.Count > 0) quotationParts.Add($"({QOr()})");
            if (itemNames.Count > 0)
                quotationParts.Add($"""
                    EXISTS (
                        SELECT 1
                        FROM SLS.QuotationItem AS qi3
                            INNER JOIN INV.Item AS it3 ON it3.ItemID = qi3.ItemRef
                        WHERE qi3.QuotationRef = q.QuotationId AND ({ItemOr("qi3", "it3")}))
                    """);

            var joinedParts = new List<string>();
            if (qNames.Count > 0) joinedParts.Add(QOr());
            if (itemNames.Count > 0) joinedParts.Add(ItemOr("qi", "it"));

            var quotationContext = "AND (" + string.Join(" OR ", quotationParts) + ")";
            var itemsJoined = "AND (" + string.Join(" OR ", joinedParts) + ")";

            var explicitItemSearch = known.Count > 0 && itemNames.Count > 0;
            var itemsNested = explicitItemSearch ? $"AND ({ItemOr("qi", "it")})" : string.Empty;
            return (quotationContext, itemsJoined, itemsNested);
        }

        private static string BuildQuotationConditions(IReadOnlyList<QuotationFilter> filters, DynamicParameters p)
        {
            if (filters.Count == 0) return string.Empty;

            var parts = new List<string>();
            for (var idx = 0; idx < filters.Count; idx++)
            {
                var f = filters[idx];
                var (expr, kind) = QuotationFields[f.Field];
                var pName = $"@q{idx}";
                var (sql, value) = Condition(expr, kind, f.Op, f.Value, pName);
                p.Add(pName, value);
                parts.Add(sql);
            }
            return "AND " + string.Join(" AND ", parts);
        }

        private static string BuildItemConditions(
            IReadOnlyList<QuotationFilter> filters, DynamicParameters p, string qiAlias, string itAlias)
        {
            if (filters.Count == 0) return string.Empty;

            var parts = new List<string>();
            for (var idx = 0; idx < filters.Count; idx++)
            {
                var f = filters[idx];
                var (table, column, kind) = ItemFields[f.Field];
                var expr = $"{(table == "QI" ? qiAlias : itAlias)}.{column}";
                var pName = $"@{qiAlias}f{idx}";
                var (sql, value) = Condition(expr, kind, f.Op, f.Value, pName);
                p.Add(pName, value);
                parts.Add(sql);
            }
            return "AND " + string.Join(" AND ", parts);
        }

    }
}
