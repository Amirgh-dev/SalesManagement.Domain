using SalesManagement.Persistence.ConnectionFactories;
using static SalesManagement.Persistence.Common.PersianText;
using static SalesManagement.Persistence.Common.DapperRows;
using System.Data;
using Dapper;
using SalesManagement.Domain.Features.PartyProfiles;

namespace SalesManagement.Persistence.Features.PartyProfiles.QueryProfiles
{
    public class ProfileQueryRepository(ISepidyarhesabConnectionFactory connectionFactory)
        : IProfileQueryRepository
    {
        private const string PostCountExpr =
            "(SELECT COUNT(*) FROM dbo.PostProfile AS pc WHERE pc.ProfileRef = p.Id)";

        private const string CommentCountExpr =
            "(SELECT COUNT(*) FROM dbo.PartyComment AS cc " +
            "INNER JOIN dbo.PostProfile AS pcc ON pcc.Id = cc.PostRef WHERE pcc.ProfileRef = p.Id)";

        private const string LastPostDateExpr =
            "(SELECT MAX(x.CreationDate) FROM dbo.PostProfile AS x WHERE x.ProfileRef = p.Id)";

        private const string PostCommentCountExpr =
            "(SELECT COUNT(*) FROM dbo.PartyComment AS pcx WHERE pcx.PostRef = pp.Id)";

        private const string ProfileApply = """
            INNER JOIN dbo.ProfileParty AS p ON p.Id = pp.ProfileRef
            OUTER APPLY (
                SELECT TOP (1) x.StoreName
                FROM dbo.ExtraPartyData AS x
                WHERE x.PartyRef = p.PartyRef
            ) AS e
            OUTER APPLY (
                SELECT TOP (1) a.Address, a.Latitude, a.Longitude
                FROM [Sepidar02].[GNR].[PartyAddress] AS a
                WHERE a.PartyRef = p.PartyRef AND a.IsMain = 1
            ) AS addr
            """;

        private static readonly IReadOnlyDictionary<string, string> ProfileFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = "p.Id",
                ["partyRef"] = "p.PartyRef",
                ["userName"] = "p.UserName",
                ["aboutMe"] = "p.AboutMe",
                ["isBlackList"] = "p.IsBlackList",
                ["address"] = "addr.Address",
                ["latitude"] = "addr.Latitude",
                ["longitude"] = "addr.Longitude",
                ["rate"] = "p.Rate",
                ["storeName"] = "e.StoreName",
                ["postCount"] = PostCountExpr,
                ["commentCount"] = CommentCountExpr,
            };

        private static readonly IReadOnlyDictionary<string, string> PostFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = "pp.Id",
                ["profileRef"] = "pp.ProfileRef",
                ["title"] = "pp.Title",
                ["post"] = "pp.Post",
                ["likePost"] = "pp.LikePost",
                ["creationDate"] = "pp.CreationDate",
                ["commentCount"] = PostCommentCountExpr,
            };

        private static readonly IReadOnlyDictionary<string, string> CommentFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = "c.Id",
                ["postRef"] = "c.PostRef",
                ["profileRef"] = "c.ProfileRef",
                ["comment"] = "c.Comment",
                ["creationDate"] = "c.CreationDate",
            };

        private static readonly IReadOnlyDictionary<string, string> SearchableFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["userName"] = "p.UserName",
                ["storeName"] = "e.StoreName",
                ["aboutMe"] = "p.AboutMe",
                ["address"] = "addr.Address",
            };

        private static readonly string[] DefaultSearchFields = ["userName", "storeName"];

        private static readonly IReadOnlyDictionary<string, string> ProfileOrderFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["userName"] = "p.UserName",
                ["storeName"] = "e.StoreName",
                ["rate"] = "p.Rate",
                ["partyRef"] = "p.PartyRef",
                ["isBlackList"] = "p.IsBlackList",
                ["postCount"] = PostCountExpr,
                ["commentCount"] = CommentCountExpr,
                ["lastPostDate"] = LastPostDateExpr,
                ["creationDate"] = LastPostDateExpr,
                ["date"] = LastPostDateExpr,
            };

        private static readonly IReadOnlyDictionary<string, string> PostOrderFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["creationDate"] = "pp.CreationDate",
                ["date"] = "pp.CreationDate",
                ["likePost"] = "pp.LikePost",
                ["title"] = "pp.Title",
                ["commentCount"] = PostCommentCountExpr,
            };

        public async Task<(int Total,
            IReadOnlyList<IDictionary<string, object?>>? Profiles,
            IReadOnlyList<IDictionary<string, object?>>? Posts,
            IReadOnlyList<IDictionary<string, object?>>? Comments)> QueryAsync(
            ProfileQuery query, CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();

            return query.Scope.Primary switch
            {
                ProfileSection.Posts => await QueryPostsRootAsync(connection, query, cancellationToken),
                ProfileSection.Comments => await QueryCommentsRootAsync(connection, query, cancellationToken),
                _ => await QueryProfilesRootAsync(connection, query, cancellationToken),
            };
        }

        private async Task<(int, IReadOnlyList<IDictionary<string, object?>>?,
            IReadOnlyList<IDictionary<string, object?>>?, IReadOnlyList<IDictionary<string, object?>>?)>
            QueryProfilesRootAsync(IDbConnection connection, ProfileQuery query, CancellationToken ct)
        {
            var p = new DynamicParameters();
            p.Add("@Skip", query.Skip);
            p.Add("@Take", query.Take);

            var (idClause, searchClause) = BuildProfileWhere(query, p);

            var from = $"""
                FROM dbo.ProfileParty AS p
                    OUTER APPLY (
                        SELECT TOP (1) x.StoreName
                        FROM dbo.ExtraPartyData AS x
                        WHERE x.PartyRef = p.PartyRef
                    ) AS e
                    OUTER APPLY (
                        SELECT TOP (1) a.Address, a.Latitude, a.Longitude
                        FROM [Sepidar02].[GNR].[PartyAddress] AS a
                        WHERE a.PartyRef = p.PartyRef AND a.IsMain = 1
                    ) AS addr
                WHERE 1 = 1
                    {idClause}
                    {searchClause}
                """;

            var sql = $"""
                SELECT COUNT(*) {from};

                SELECT {BuildSelect(query.Fields, ProfileFields, "id")}
                {from}
                ORDER BY {ResolveOrderBy(query, ProfileOrderFields, "p.Rate", "p.Id")}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            var (total, profiles) = await ReadCountAndRowsAsync(connection, sql, p, ct);
            if (profiles.Count == 0 || !query.Scope.Posts)
                return (total, profiles, null, null);

            var profileIds = GuidKeys(profiles, "id");
            if (profileIds.Length == 0)
                return (total, profiles, null, null);

            var postSql = $"""
                SELECT {BuildSelect(query.Fields, PostFields, "id", "profileRef")}
                FROM dbo.PostProfile AS pp
                WHERE pp.ProfileRef IN @ProfileIds
                ORDER BY pp.CreationDate DESC, pp.Id;
                """;

            var posts = await ReadRowsAsync(connection, postSql, new { ProfileIds = profileIds }, ct);

            await AttachPostPhotosAsync(connection, posts, ct);

            if (query.Scope.Comments)
                await AttachCommentsAsync(connection, query, posts, ct);

            var postsByProfile = posts
                .GroupBy(r => GuidOf(r, "profileRef"))
                .ToDictionary(gr => gr.Key, gr => gr.ToList());

            foreach (var profile in profiles)
                profile["posts"] = postsByProfile.TryGetValue(GuidOf(profile, "id"), out var ps)
                    ? ps
                    : new List<IDictionary<string, object?>>();

            return (total, profiles, null, null);
        }

        private async Task<(int, IReadOnlyList<IDictionary<string, object?>>?,
            IReadOnlyList<IDictionary<string, object?>>?, IReadOnlyList<IDictionary<string, object?>>?)>
            QueryPostsRootAsync(IDbConnection connection, ProfileQuery query, CancellationToken ct)
        {
            var p = new DynamicParameters();
            p.Add("@Skip", query.Skip);
            p.Add("@Take", query.Take);

            var (idClause, searchClause) = BuildProfileWhere(query, p);

            var from = $"""
                FROM dbo.PostProfile AS pp
                    {ProfileApply}
                WHERE 1 = 1
                    {idClause}
                    {searchClause}
                """;

            var sql = $"""
                SELECT COUNT(*) {from};

                SELECT {BuildSelect(query.Fields, PostFields, "id", "profileRef")}
                {from}
                ORDER BY {ResolveOrderBy(query, PostOrderFields, "pp.CreationDate", "pp.Id")}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            var (total, posts) = await ReadCountAndRowsAsync(connection, sql, p, ct);

            if (posts.Count > 0)
            {
                await AttachPostPhotosAsync(connection, posts, ct);

                if (query.Scope.Comments)
                    await AttachCommentsAsync(connection, query, posts, ct);
            }

            return (total, null, posts, null);
        }

        private async Task<(int, IReadOnlyList<IDictionary<string, object?>>?,
            IReadOnlyList<IDictionary<string, object?>>?, IReadOnlyList<IDictionary<string, object?>>?)>
            QueryCommentsRootAsync(IDbConnection connection, ProfileQuery query, CancellationToken ct)
        {
            var p = new DynamicParameters();
            p.Add("@Skip", query.Skip);
            p.Add("@Take", query.Take);

            var (idClause, searchClause) = BuildProfileWhere(query, p);

            var from = $"""
                FROM dbo.PartyComment AS c
                    INNER JOIN dbo.PostProfile AS pp ON pp.Id = c.PostRef
                    {ProfileApply}
                WHERE 1 = 1
                    {idClause}
                    {searchClause}
                """;

            var sql = $"""
                SELECT COUNT(*) {from};

                SELECT {BuildSelect(query.Fields, CommentFields, "id", "postRef")}
                {from}
                ORDER BY {ResolveCommentOrderBy(query)}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            var (total, comments) = await ReadCountAndRowsAsync(connection, sql, p, ct);
            return (total, null, null, comments);
        }

        private static async Task AttachPostPhotosAsync(
            IDbConnection connection, List<IDictionary<string, object?>> posts, CancellationToken ct)
        {
            var postIds = GuidKeys(posts, "id");
            if (postIds.Length == 0)
            {
                foreach (var post in posts)
                    post["photos"] = new List<IDictionary<string, object?>>();
                return;
            }

            const string photoSql = """
                SELECT Id AS [id], PostRef AS [postRef], FileName AS [fileName],
                       FileExtensions AS [fileExtensions], Path AS [path], IsMain AS [isMain]
                FROM dbo.PostFile
                WHERE PostRef IN @PostIds
                ORDER BY IsMain DESC, Id;
                """;

            var photos = await ReadRowsAsync(connection, photoSql, new { PostIds = postIds }, ct);

            var byPost = photos
                .GroupBy(r => GuidOf(r, "postRef"))
                .ToDictionary(gr => gr.Key, gr => gr.ToList());

            foreach (var post in posts)
                post["photos"] = byPost.TryGetValue(GuidOf(post, "id"), out var ps)
                    ? ps
                    : new List<IDictionary<string, object?>>();
        }

        private static async Task AttachCommentsAsync(
            IDbConnection connection, ProfileQuery query,
            List<IDictionary<string, object?>> posts, CancellationToken ct)
        {
            var postIds = GuidKeys(posts, "id");
            if (postIds.Length == 0)
            {
                foreach (var post in posts)
                    post["comments"] = new List<IDictionary<string, object?>>();
                return;
            }

            var commentSql = $"""
                SELECT {BuildSelect(query.Fields, CommentFields, "id", "postRef")}
                FROM dbo.PartyComment AS c
                WHERE c.PostRef IN @PostIds
                ORDER BY c.CreationDate, c.Id;
                """;

            var comments = await ReadRowsAsync(connection, commentSql, new { PostIds = postIds }, ct);

            var byPost = comments
                .GroupBy(r => GuidOf(r, "postRef"))
                .ToDictionary(gr => gr.Key, gr => gr.ToList());

            foreach (var post in posts)
                post["comments"] = byPost.TryGetValue(GuidOf(post, "id"), out var cs)
                    ? cs
                    : new List<IDictionary<string, object?>>();
        }

        private static (string IdClause, string SearchClause) BuildProfileWhere(
            ProfileQuery query, DynamicParameters p)
        {
            var idClause = string.Empty;
            if (query.Id is { } gid)
            {
                p.Add("@Id", gid);
                idClause = "AND p.Id = @Id";
            }

            return (idClause, BuildSearch(query.Search, p));
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

        private static string BuildSearch(ProfileSearch? search, DynamicParameters p)
        {
            if (search is null || string.IsNullOrWhiteSpace(search.Term))
                return string.Empty;

            var known = search.Fields.Where(SearchableFields.ContainsKey).ToList();
            var names = known.Count > 0 ? known : DefaultSearchFields.ToList();

            p.Add("@s", "%" + NormalizeFa(search.Term) + "%");

            var ors = names.Select(n =>
                $"{FaNormExpr($"CAST({SearchableFields[n]} AS nvarchar(400))")} LIKE @s");

            return "AND (" + string.Join(" OR ", ors) + ")";
        }

        private static string ResolveOrderBy(
            ProfileQuery query, IReadOnlyDictionary<string, string> fields, string fallbackExpr, string tieBreaker)
        {
            var dir = query.Descending ? "DESC" : "ASC";

            if (query.OrderBy is not null && fields.TryGetValue(query.OrderBy, out var expr))
                return $"{expr} {dir}, {tieBreaker} ASC";

            return $"{fallbackExpr} {dir}, {tieBreaker} ASC";
        }

        private static string ResolveCommentOrderBy(ProfileQuery query)
        {
            var dir = query.Descending ? "DESC" : "ASC";
            if (query.OrderBy is not null &&
                (query.OrderBy.Equals("creationDate", StringComparison.OrdinalIgnoreCase) ||
                 query.OrderBy.Equals("date", StringComparison.OrdinalIgnoreCase)))
                return $"c.CreationDate {dir}, c.Id ASC";

            return $"c.CreationDate {dir}, c.Id ASC";
        }

        private static Guid[] GuidKeys(IEnumerable<IDictionary<string, object?>> rows, string key) => rows
            .Select(r => GuidOf(r, key))
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToArray();

        private static Guid GuidOf(IDictionary<string, object?> row, string key) =>
            row.TryGetValue(key, out var v) && v is Guid g ? g : Guid.Empty;

    }
}
