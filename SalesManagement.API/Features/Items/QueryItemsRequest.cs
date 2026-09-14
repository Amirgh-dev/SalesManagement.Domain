namespace SalesManagement.API.Features.Items
{
    public class QueryItemsRequest
    {
        public int? Id { get; init; }
        public string? CategoryCode { get; init; }
        public string? Scope { get; init; }
        public string? Fields { get; init; }

        public ItemSearchSpec? Search { get; init; }
        public string? OrderBy { get; init; }
        public bool Descending { get; init; } = true;
        public int? Skip { get; init; }
        public int? Take { get; init; }
        public int? Page { get; init; }
        public int? PageSize { get; init; }

        public class ItemSearchSpec
        {
            public string? Term { get; init; }
            public string? Fields { get; init; }
        }
    }
}
