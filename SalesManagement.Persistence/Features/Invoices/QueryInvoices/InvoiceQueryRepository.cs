using SalesManagement.Persistence.ConnectionFactories;
using SalesManagement.Persistence.Common;
using static SalesManagement.Persistence.Common.PersianText;
using static SalesManagement.Persistence.Common.DapperRows;
using static SalesManagement.Persistence.Common.SqlFilter;
using System.Data;
using Dapper;
using SalesManagement.Domain.Common;
using SalesManagement.Domain.Features.Invoices;

namespace SalesManagement.Persistence.Features.Invoices.QueryInvoices
{
    public class InvoiceQueryRepository(ISepidarConnectionFactory connectionFactory) : IInvoiceQueryRepository
    {
        private static readonly IReadOnlyDictionary<string, (string Expr, Kind Kind)> InvoiceFields =
            new Dictionary<string, (string, Kind)>(StringComparer.OrdinalIgnoreCase)
            {
                ["invoiceId"] = ("i.InvoiceId", Kind.Int),
                ["number"] = ("i.Number", Kind.Int),
                ["date"] = ("i.Date", Kind.DateTime),
                ["customerPartyRef"] = ("i.CustomerPartyRef", Kind.Int),
                ["customerName"] = ("i.CustomerRealName", Kind.String),
                ["price"] = ("i.Price", Kind.Decimal),
                ["discount"] = ("i.Discount", Kind.Decimal),
                ["tax"] = ("i.Tax", Kind.Decimal),
                ["netPrice"] = ("i.NetPrice", Kind.Decimal),
                ["closed"] = ("(CASE WHEN i.State = 0 THEN 0 ELSE 1 END)", Kind.Bool),
            };

        private static readonly IReadOnlyDictionary<string, (string Table, string Column, Kind Kind)> ItemFields =
            new Dictionary<string, (string, string, Kind)>(StringComparer.OrdinalIgnoreCase)
            {
                ["itemId"] = ("II", "InvoiceItemID", Kind.Int),
                ["itemRowId"] = ("II", "RowID", Kind.Int),
                ["itemRef"] = ("II", "ItemRef", Kind.Int),
                ["itemCode"] = ("IT", "Code", Kind.String),
                ["itemTitle"] = ("IT", "Title", Kind.String),
                ["itemQuantity"] = ("II", "Quantity", Kind.Decimal),
                ["itemFee"] = ("II", "Fee", Kind.Decimal),
                ["itemPrice"] = ("II", "Price", Kind.Decimal),
                ["itemDiscount"] = ("II", "Discount", Kind.Decimal),
                ["itemNetPrice"] = ("II", "NetPrice", Kind.Decimal),
                ["itemDescription"] = ("II", "Description", Kind.String),
            };

        private const string VendorDeclare = VendorScopeSql.Declare;

        public async Task<(int Total, IReadOnlyList<IDictionary<string, object?>>? Invoices, IReadOnlyList<IDictionary<string, object?>>? Items)> QueryAsync(
            int partyId, InvoiceQuery query, CancellationToken cancellationToken = default)
        {
            var p = new DynamicParameters();
            p.Add("@PartyId", partyId);
            p.Add("@Skip", query.Skip);
            p.Add("@Take", query.Take);

            var (invoiceWhere, itemWhereMain, itemWhereExists) = BuildFilters(query.Filters, p);
            var (searchInvoiceCtx, searchItemsJoined, searchItemsNested) = BuildSearch(query.Search, p);

            using var connection = connectionFactory.CreateConnection();

            return query.Scope switch
            {
                InvoiceScope.Items => await QueryItemsOnlyAsync(
                    connection, query, p, invoiceWhere, itemWhereMain, searchItemsJoined, cancellationToken),
                InvoiceScope.Invoices => await QueryInvoicesOnlyAsync(
                    connection, query, p, invoiceWhere, itemWhereExists, searchInvoiceCtx, cancellationToken),
                _ => await QueryBothAsync(
                    connection, query, p, invoiceWhere, itemWhereExists, searchInvoiceCtx, searchItemsNested,
                    query.Filters, cancellationToken),
            };
        }

        private async Task<(int Total, IReadOnlyList<IDictionary<string, object?>>? Invoices, IReadOnlyList<IDictionary<string, object?>>? Items)> QueryInvoicesOnlyAsync(
            IDbConnection connection, InvoiceQuery query, DynamicParameters p,
            string invoiceWhere, string itemWhereExists, string searchClause, CancellationToken ct)
        {
            var from = $"""
                FROM GNR.Grouping AS g
                    INNER JOIN GNR.Party AS p ON p.CustomerGroupingRef = g.GroupingID
                    INNER JOIN SLS.Invoice AS i ON i.CustomerPartyRef = p.PartyId
                WHERE g.Code = @VendorCode
                    {invoiceWhere}
                    {ItemExistsClause(itemWhereExists)}
                    {searchClause}
                """;

            var sql = $"""
                {VendorDeclare}

                SELECT COUNT(*) {from};

                SELECT {BuildInvoiceSelect(query.Fields)} {from}
                ORDER BY {ResolveOrderBy(query, invoice: true)}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            var (total, invoices) = await ReadCountAndRowsAsync(connection, sql, p, ct);
            return (total, invoices, null);
        }

        private async Task<(int Total, IReadOnlyList<IDictionary<string, object?>>? Invoices, IReadOnlyList<IDictionary<string, object?>>? Items)> QueryItemsOnlyAsync(
            IDbConnection connection, InvoiceQuery query, DynamicParameters p,
            string invoiceWhere, string itemWhereMain, string searchClause, CancellationToken ct)
        {
            var from = $"""
                FROM GNR.Grouping AS g
                    INNER JOIN GNR.Party AS p ON p.CustomerGroupingRef = g.GroupingID
                    INNER JOIN SLS.Invoice AS i ON i.CustomerPartyRef = p.PartyId
                    INNER JOIN SLS.InvoiceItem AS ii ON ii.InvoiceRef = i.InvoiceId
                    INNER JOIN INV.Item AS it ON it.ItemID = ii.ItemRef
                WHERE g.Code = @VendorCode
                    {invoiceWhere}
                    {itemWhereMain}
                    {searchClause}
                """;

            var sql = $"""
                {VendorDeclare}

                SELECT COUNT(*) {from};

                SELECT {BuildItemSelect(query.Fields)} {from}
                ORDER BY {ResolveOrderBy(query, invoice: false)}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            var (total, items) = await ReadCountAndRowsAsync(connection, sql, p, ct);
            return (total, null, items);
        }

        private async Task<(int Total, IReadOnlyList<IDictionary<string, object?>>? Invoices, IReadOnlyList<IDictionary<string, object?>>? Items)> QueryBothAsync(
            IDbConnection connection, InvoiceQuery query, DynamicParameters p,
            string invoiceWhere, string itemWhereExists, string searchClause, string searchItemsNested,
            IReadOnlyList<InvoiceFilter> filters, CancellationToken ct)
        {
            var from = $"""
                FROM GNR.Grouping AS g
                    INNER JOIN GNR.Party AS p ON p.CustomerGroupingRef = g.GroupingID
                    INNER JOIN SLS.Invoice AS i ON i.CustomerPartyRef = p.PartyId
                WHERE g.Code = @VendorCode
                    {invoiceWhere}
                    {ItemExistsClause(itemWhereExists)}
                    {searchClause}
                """;

            var invoiceSql = $"""
                {VendorDeclare}

                SELECT COUNT(*) {from};

                SELECT {BuildInvoiceSelect(query.Fields)} {from}
                ORDER BY {ResolveOrderBy(query, invoice: true)}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            var (total, invoices) = await ReadCountAndRowsAsync(connection, invoiceSql, p, ct);

            var invoiceIds = invoices
                .Select(r => r.TryGetValue("invoiceId", out var v) && v is not null ? Convert.ToInt32(v) : 0)
                .Where(id => id != 0)
                .Distinct()
                .ToArray();

            if (invoiceIds.Length == 0)
                return (total, invoices, null);

            var itemFilters = filters.Where(f => ItemFields.ContainsKey(f.Field)).ToList();
            var itemParams = new DynamicParameters();
            itemParams.Add("@InvoiceIds", invoiceIds);
            var itemConds = BuildItemConditions(itemFilters, itemParams, "ii", "it");

            if (!string.IsNullOrEmpty(searchItemsNested) && query.Search is not null)
                itemParams.Add("@s", "%" + NormalizeFa(query.Search.Term) + "%");

            var itemSql = $"""
                SELECT {BuildItemSelect(query.Fields)}
                FROM SLS.InvoiceItem AS ii
                    INNER JOIN INV.Item AS it ON it.ItemID = ii.ItemRef
                WHERE ii.InvoiceRef IN @InvoiceIds
                    {itemConds}
                    {searchItemsNested}
                ORDER BY ii.InvoiceRef, ii.RowID;
                """;

            var items = await ReadRowsAsync(connection, itemSql, itemParams, ct);

            var itemsByInvoice = items
                .GroupBy(r => r.TryGetValue("invoiceRef", out var v) && v is not null ? Convert.ToInt32(v) : 0)
                .ToDictionary(gr => gr.Key, gr => gr.ToList());

            foreach (var inv in invoices)
            {
                var id = inv.TryGetValue("invoiceId", out var v) && v is not null ? Convert.ToInt32(v) : 0;
                inv["items"] = itemsByInvoice.TryGetValue(id, out var its)
                    ? its
                    : new List<IDictionary<string, object?>>();
            }

            return (total, invoices, null);
        }

        private static string BuildInvoiceSelect(IReadOnlyList<string> fields)
        {
            var valid = fields.Where(InvoiceFields.ContainsKey).ToList();
            var names = valid.Count > 0 ? valid : InvoiceFields.Keys.ToList();

            var cols = new List<string> { "i.InvoiceId AS [invoiceId]" };
            cols.AddRange(names
                .Where(n => !n.Equals("invoiceId", StringComparison.OrdinalIgnoreCase))
                .Select(n => $"{InvoiceFields[n].Expr} AS [{n}]"));
            return string.Join(", ", cols);
        }

        private static string BuildItemSelect(IReadOnlyList<string> fields)
        {
            var valid = fields.Where(ItemFields.ContainsKey).ToList();
            var names = valid.Count > 0 ? valid : ItemFields.Keys.ToList();

            var cols = new List<string> { "ii.InvoiceRef AS [invoiceRef]" };
            cols.AddRange(names.Select(n =>
            {
                var (table, column, _) = ItemFields[n];
                var alias = table == "II" ? "ii" : "it";
                return $"{alias}.{column} AS [{n}]";
            }));
            return string.Join(", ", cols);
        }

        private static string ResolveOrderBy(InvoiceQuery query, bool invoice)
        {
            var dir = query.Descending ? "DESC" : "ASC";
            var fallback = invoice ? $"i.Number {dir}" : $"ii.InvoiceRef {dir}, ii.RowID {dir}";

            if (query.OrderBy is null)
                return fallback;

            if (invoice)
                return InvoiceFields.TryGetValue(query.OrderBy, out var f) ? $"{f.Expr} {dir}" : fallback;

            if (query.OrderBy.Equals("invoiceRef", StringComparison.OrdinalIgnoreCase))
                return $"ii.InvoiceRef {dir}";
            if (ItemFields.TryGetValue(query.OrderBy, out var it))
            {
                var a = it.Table == "II" ? "ii" : "it";
                return $"{a}.{it.Column} {dir}";
            }
            return fallback;
        }

        private static string ItemExistsClause(string itemWhereExists) =>
            string.IsNullOrEmpty(itemWhereExists)
                ? string.Empty
                : $"""
                    AND EXISTS (
                        SELECT 1
                        FROM SLS.InvoiceItem AS ii2
                            INNER JOIN INV.Item AS it2 ON it2.ItemID = ii2.ItemRef
                        WHERE ii2.InvoiceRef = i.InvoiceId
                        {itemWhereExists})
                    """;

        private static (string InvoiceWhere, string ItemWhereMain, string ItemWhereExists) BuildFilters(
            IReadOnlyList<InvoiceFilter> filters, DynamicParameters p)
        {
            var invoiceFilters = filters.Where(f => InvoiceFields.ContainsKey(f.Field)).ToList();
            var itemFilters = filters.Where(f => ItemFields.ContainsKey(f.Field)).ToList();

            var invoiceWhere = BuildInvoiceConditions(invoiceFilters, p);
            var itemWhereMain = BuildItemConditions(itemFilters, p, "ii", "it");
            var itemWhereExists = BuildItemConditions(itemFilters, p, "ii2", "it2");

            return (invoiceWhere, itemWhereMain, itemWhereExists);
        }

        private static (string InvoiceContext, string ItemsJoined, string ItemsNested) BuildSearch(
            InvoiceSearch? search, DynamicParameters p)
        {
            if (search is null || string.IsNullOrWhiteSpace(search.Term))
                return (string.Empty, string.Empty, string.Empty);

            var known = search.Fields
                .Where(f => InvoiceFields.ContainsKey(f) || ItemFields.ContainsKey(f))
                .ToList();

            List<string> invNames, itemNames;
            if (known.Count == 0)
            {
                invNames = InvoiceFields.Keys.ToList();
                itemNames = ItemFields.Keys.ToList();
            }
            else
            {
                invNames = known.Where(InvoiceFields.ContainsKey).ToList();
                itemNames = known.Where(ItemFields.ContainsKey).ToList();
            }

            p.Add("@s", "%" + NormalizeFa(search.Term) + "%");

            string InvOr() => string.Join(" OR ",
                invNames.Select(n => LikeExpr(InvoiceFields[n].Expr, InvoiceFields[n].Kind)));

            string ItemOr(string ii, string it) => string.Join(" OR ", itemNames.Select(n =>
            {
                var (table, column, kind) = ItemFields[n];
                return LikeExpr($"{(table == "II" ? ii : it)}.{column}", kind);
            }));

            var invoiceParts = new List<string>();
            if (invNames.Count > 0) invoiceParts.Add($"({InvOr()})");
            if (itemNames.Count > 0)
                invoiceParts.Add($"""
                    EXISTS (
                        SELECT 1
                        FROM SLS.InvoiceItem AS ii3
                            INNER JOIN INV.Item AS it3 ON it3.ItemID = ii3.ItemRef
                        WHERE ii3.InvoiceRef = i.InvoiceId AND ({ItemOr("ii3", "it3")}))
                    """);

            var joinedParts = new List<string>();
            if (invNames.Count > 0) joinedParts.Add(InvOr());
            if (itemNames.Count > 0) joinedParts.Add(ItemOr("ii", "it"));

            var invoiceContext = "AND (" + string.Join(" OR ", invoiceParts) + ")";
            var itemsJoined = "AND (" + string.Join(" OR ", joinedParts) + ")";

            var explicitItemSearch = known.Count > 0 && itemNames.Count > 0;
            var itemsNested = explicitItemSearch ? $"AND ({ItemOr("ii", "it")})" : string.Empty;
            return (invoiceContext, itemsJoined, itemsNested);
        }

        private static string BuildInvoiceConditions(IReadOnlyList<InvoiceFilter> filters, DynamicParameters p)
        {
            if (filters.Count == 0) return string.Empty;

            var parts = new List<string>();
            for (var idx = 0; idx < filters.Count; idx++)
            {
                var f = filters[idx];
                var (expr, kind) = InvoiceFields[f.Field];
                var pName = $"@inv{idx}";
                var (sql, value) = Condition(expr, kind, f.Op, f.Value, pName);
                p.Add(pName, value);
                parts.Add(sql);
            }
            return "AND " + string.Join(" AND ", parts);
        }

        private static string BuildItemConditions(
            IReadOnlyList<InvoiceFilter> filters, DynamicParameters p, string iiAlias, string itAlias)
        {
            if (filters.Count == 0) return string.Empty;

            var parts = new List<string>();
            for (var idx = 0; idx < filters.Count; idx++)
            {
                var f = filters[idx];
                var (table, column, kind) = ItemFields[f.Field];
                var expr = $"{(table == "II" ? iiAlias : itAlias)}.{column}";
                var pName = $"@{iiAlias}f{idx}";
                var (sql, value) = Condition(expr, kind, f.Op, f.Value, pName);
                p.Add(pName, value);
                parts.Add(sql);
            }
            return "AND " + string.Join(" AND ", parts);
        }

    }
}
