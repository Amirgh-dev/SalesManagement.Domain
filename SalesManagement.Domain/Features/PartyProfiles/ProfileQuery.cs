using static SalesManagement.Domain.Common.QueryText;

namespace SalesManagement.Domain.Features.PartyProfiles
{
    public sealed record ProfileSearch(string Term, IReadOnlyList<string> Fields);

    public enum ProfileSection { Profiles, Posts, Comments }

    public sealed record ProfileScope(bool Profiles, bool Posts, bool Comments)
    {
        public static readonly ProfileScope All = new(true, true, true);

        public ProfileSection Primary =>
            Profiles ? ProfileSection.Profiles :
            Posts ? ProfileSection.Posts :
            Comments ? ProfileSection.Comments :
            ProfileSection.Profiles;
    }

    public sealed record ProfileQuery
    {
        public const int MaxTake = 200;

        public Guid? Id { get; }
        public int? PartyRef { get; }
        public ProfileScope Scope { get; }
        public IReadOnlyList<string> Fields { get; }
        public ProfileSearch? Search { get; }
        public string? OrderBy { get; }
        public bool Descending { get; }
        public int Skip { get; }
        public int Take { get; }
        public int? ProvinceRef { get; }
        public int? CountyRef { get; }

        private ProfileQuery(
            Guid? id, int? partyRef, ProfileScope scope, IReadOnlyList<string> fields, ProfileSearch? search,
            string? orderBy, bool descending, int skip, int take, int? provinceRef, int? countyRef)
        {
            Id = id;
            PartyRef = partyRef;
            Scope = scope;
            Fields = fields;
            Search = search;
            OrderBy = orderBy;
            Descending = descending;
            Skip = skip;
            Take = take;
            ProvinceRef = provinceRef;
            CountyRef = countyRef;
        }

        public static ProfileQuery Create(
            Guid? id,
            int? partyRef,
            string? scope,
            string? fields,
            string? searchTerm,
            string? searchFields,
            string? orderBy,
            bool descending,
            int? skip,
            int? take,
            int? page,
            int? pageSize,
            int? provinceRef = null,
            int? countyRef = null)
        {
            var parsedId = id is { } g && g != Guid.Empty ? g : (Guid?)null;
            var parsedPartyRef = partyRef is > 0 ? partyRef : null;

            var parsedScope = ParseScope(scope);
            var parsedFields = SplitNames(fields);

            ProfileSearch? parsedSearch = null;
            if (!IsBlankOrPlaceholder(searchTerm))
                parsedSearch = new ProfileSearch(searchTerm!.Trim(), SplitNames(searchFields));

            int t, s;
            if (page is > 0 || pageSize is > 0)
            {
                var pg = page is > 0 ? page.Value : 1;
                t = pageSize is null or <= 0 ? 100 : Math.Min(pageSize.Value, MaxTake);
                s = (pg - 1) * t;
            }
            else
            {
                t = take is null or <= 0 ? 100 : Math.Min(take.Value, MaxTake);
                s = skip is null or < 0 ? 0 : skip.Value;
            }

            return new ProfileQuery(
                parsedId, parsedPartyRef, parsedScope, parsedFields, parsedSearch,
                IsBlankOrPlaceholder(orderBy) ? null : orderBy.Trim(),
                descending, s, t,
                provinceRef is > 0 ? provinceRef : null,
                countyRef is > 0 ? countyRef : null);
        }

        private static ProfileScope ParseScope(string? scope)
        {
            var raw = (scope ?? string.Empty).Trim().ToLowerInvariant();
            if (raw is "" or "string" or "all")
                return ProfileScope.All;

            var parts = raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            bool profiles = false, posts = false, comments = false, recognized = false;
            foreach (var part in parts)
            {
                switch (part)
                {
                    case "profile" or "profiles":
                        profiles = true;
                        recognized = true;
                        break;
                    case "post" or "posts":
                        posts = true;
                        recognized = true;
                        break;
                    case "comment" or "comments":
                        comments = true;
                        recognized = true;
                        break;
                }
            }

            return recognized ? new ProfileScope(profiles, posts, comments) : ProfileScope.All;
        }
    }
}
