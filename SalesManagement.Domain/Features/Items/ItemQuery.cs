using static SalesManagement.Domain.Common.QueryText;

namespace SalesManagement.Domain.Features.Items
{
    public sealed record ItemSearch(string Term, IReadOnlyList<string> Fields);

    public enum ItemSection { Items, Categories }

    public sealed record ItemScope(bool Items, bool Categories)
    {
        public static readonly ItemScope All = new(true, true);

        public ItemSection Primary => Items ? ItemSection.Items : ItemSection.Categories;
    }

    public sealed record ItemQuery
    {
        public const int MaxTake = 500;

        public int? Id { get; }
        public string? CategoryCode { get; }
        public ItemScope Scope { get; }
        public IReadOnlyList<string> Fields { get; }
        public ItemSearch? Search { get; }
        public string? OrderBy { get; }
        public bool Descending { get; }
        public int Skip { get; }
        public int Take { get; }

        private ItemQuery(
            int? id, string? categoryCode, ItemScope scope, IReadOnlyList<string> fields, ItemSearch? search,
            string? orderBy, bool descending, int skip, int take)
        {
            Id = id;
            CategoryCode = categoryCode;
            Scope = scope;
            Fields = fields;
            Search = search;
            OrderBy = orderBy;
            Descending = descending;
            Skip = skip;
            Take = take;
        }

        public static ItemQuery Create(
            int? id,
            string? categoryCode,
            string? scope,
            string? fields,
            string? searchTerm,
            string? searchFields,
            string? orderBy,
            bool descending,
            int? skip,
            int? take,
            int? page,
            int? pageSize)
        {
            var parsedId = id is > 0 ? id : null;
            var parsedCategoryCode = IsBlankOrPlaceholder(categoryCode) ? null : categoryCode.Trim();

            var parsedScope = ParseScope(scope);
            var parsedFields = SplitNames(fields);

            ItemSearch? parsedSearch = null;
            if (!IsBlankOrPlaceholder(searchTerm))
            {
                var searchFieldList = SplitNames(searchFields);
                if (searchFieldList.Count == 0)
                    searchFieldList = parsedFields;

                parsedSearch = new ItemSearch(searchTerm!.Trim(), searchFieldList);
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

            return new ItemQuery(
                parsedId, parsedCategoryCode, parsedScope, parsedFields, parsedSearch,
                IsBlankOrPlaceholder(orderBy) ? null : orderBy.Trim(),
                descending, s, t);
        }

        private static ItemScope ParseScope(string? scope)
        {
            var raw = (scope ?? string.Empty).Trim().ToLowerInvariant();
            if (raw is "" or "string" or "all" or "both")
                return ItemScope.All;

            var parts = raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            bool items = false, categories = false, recognized = false;
            foreach (var part in parts)
            {
                switch (part)
                {
                    case "item" or "items":
                        items = true;
                        recognized = true;
                        break;
                    case "category" or "categories":
                        categories = true;
                        recognized = true;
                        break;
                }
            }

            return recognized ? new ItemScope(items, categories) : ItemScope.All;
        }
    }
}
