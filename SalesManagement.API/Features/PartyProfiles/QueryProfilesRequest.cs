namespace SalesManagement.API.Features.PartyProfiles
{
    public class QueryProfilesRequest
    {
        public string? Id { get; init; }

        public string? Scope { get; init; }

        public string? Fields { get; init; }

        public ProfileSearchSpec? Search { get; init; }
        public string? OrderBy { get; init; }
        public bool Descending { get; init; } = true;
        public int? Skip { get; init; }
        public int? Take { get; init; }
        public int? Page { get; init; }
        public int? PageSize { get; init; }

        public class ProfileSearchSpec
        {
            public string? Term { get; init; }
            public string? Fields { get; init; }
        }
    }
}
