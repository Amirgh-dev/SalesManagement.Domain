using SalesManagement.Domain.Common;
using static SalesManagement.Domain.Common.QueryText;

namespace SalesManagement.Domain.Features.Quotations
{
    public enum QuotationScope { Both, Quotations, Items }

    public sealed record QuotationFilter(string Field, FilterOp Op, string Value);

    public sealed record QuotationSearch(string Term, IReadOnlyList<string> Fields);

    public sealed record QuotationQuery
    {
        public const int MaxTake = 500;

        public QuotationScope Scope { get; }
        public IReadOnlyList<string> Fields { get; }
        public IReadOnlyList<QuotationFilter> Filters { get; }
        public QuotationSearch? Search { get; }
        public string? OrderBy { get; }
        public bool Descending { get; }
        public int Skip { get; }
        public int Take { get; }

        private QuotationQuery(
            QuotationScope scope, IReadOnlyList<string> fields, IReadOnlyList<QuotationFilter> filters,
            QuotationSearch? search, string? orderBy, bool descending, int skip, int take)
        {
            Scope = scope;
            Fields = fields;
            Filters = filters;
            Search = search;
            OrderBy = orderBy;
            Descending = descending;
            Skip = skip;
            Take = take;
        }

        public static QuotationQuery Create(
            string? scope,
            string? fields,
            IEnumerable<(string? Field, string? Op, string? Value)>? filters,
            (string? Term, string? Fields) search,
            string? orderBy,
            bool descending,
            int? skip,
            int? take,
            int? page,
            int? pageSize)
        {
            var parsedScope = (scope ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "quotations" => QuotationScope.Quotations,
                "items" => QuotationScope.Items,
                _ => QuotationScope.Both,
            };

            var parsedFields = SplitNames(fields);

            var parsedFilters = new List<QuotationFilter>();
            foreach (var f in filters ?? [])
            {
                if (IsBlankOrPlaceholder(f.Field) || IsBlankOrPlaceholder(f.Value))
                    continue;

                var op = ParseOp(f.Op);
                if (op is null)
                    continue;

                parsedFilters.Add(new QuotationFilter(f.Field!.Trim(), op.Value, f.Value!.Trim()));
            }

            QuotationSearch? parsedSearch = null;
            if (!IsBlankOrPlaceholder(search.Term))
            {
                var searchFields = SplitNames(search.Fields);
                if (searchFields.Count == 0)
                    searchFields = parsedFields;

                parsedSearch = new QuotationSearch(search.Term!.Trim(), searchFields);
            }

            int t, s;
            if (page is > 0)
            {
                t = pageSize is null or <= 0 ? 100 : Math.Min(pageSize.Value, MaxTake);
                s = (page.Value - 1) * t;
            }
            else
            {
                t = take is null or <= 0 ? 100 : Math.Min(take.Value, MaxTake);
                s = skip is null or < 0 ? 0 : skip.Value;
            }

            return new QuotationQuery(
                parsedScope, parsedFields, parsedFilters, parsedSearch,
                IsBlankOrPlaceholder(orderBy) ? null : orderBy.Trim(),
                descending, s, t);
        }
    }
}
