namespace SalesManagement.API.Features.Quotations
{
    public class QueryQuotationsRequest
    {
        public string? Scope { get; init; }
        public string? Fields { get; init; }

        public int? Id { get; init; }
        public int? CustomerPartyRef { get; init; }

        public IReadOnlyList<QuotationFilterItem>? Filters { get; init; }
        public QuotationSearchSpec? Search { get; init; }
        public string? OrderBy { get; init; }
        public bool Descending { get; init; } = true;
        public int? Skip { get; init; }
        public int? Take { get; init; }
        public int? Page { get; init; }
        public int? PageSize { get; init; }

        public class QuotationFilterItem
        {
            public string? Field { get; init; }
            public string? Op { get; init; }
            public string? Value { get; init; }
        }

        public class QuotationSearchSpec
        {
            public string? Term { get; init; }
            public string? Fields { get; init; }
        }
    }
}
