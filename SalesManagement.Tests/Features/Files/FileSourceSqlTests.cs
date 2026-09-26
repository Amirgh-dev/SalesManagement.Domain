using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using SalesManagement.Persistence.ConnectionFactories;
using SalesManagement.Persistence.Features.Files;

namespace SalesManagement.Tests.Features.Files
{
    public class FileSourceSqlTests
    {
        private sealed class NoConnectionFactory : IZanisServicesConnectionFactory
        {
            public IDbConnection CreateConnection() => throw new InvalidOperationException("no database in unit tests");
        }

        private static string SqlOf(DapperFileSource source) =>
            (string)typeof(DapperFileSource)
                .GetProperty("DeleteSql", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(source)!;

        private static readonly PostFileSource Posts = new(new NoConnectionFactory());
        private static readonly PartyFileSource Parties = new(new NoConnectionFactory());

        [Fact]
        public void Source_names_are_stable()
        {
            Assert.Equal("PostFile", Posts.Name);
            Assert.Equal("PartyFile", Parties.Name);
        }

        [Theory]
        [InlineData("PostFile")]
        [InlineData("PartyFile")]
        public void Every_source_deletes_and_returns_the_file_name_and_folder_in_one_statement(string which)
        {
            var sql = SqlOf(which == "PostFile" ? Posts : Parties);

            Assert.StartsWith("DELETE", sql.TrimStart());
            Assert.Contains("OUTPUT deleted.FileName AS FileName", sql);
            Assert.Contains("deleted.[Tables] AS Folder", sql);
            Assert.DoesNotContain("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("PostFile")]
        [InlineData("PartyFile")]
        public void Every_source_only_uses_the_id_and_party_parameters(string which)
        {
            var sql = SqlOf(which == "PostFile" ? Posts : Parties);

            var parameters = Regex.Matches(sql, @"@(\w+)").Select(m => m.Groups[1].Value).ToHashSet();

            Assert.Equal(new HashSet<string> { "Id", "PartyId" }, parameters);
        }

        [Theory]
        [InlineData("PostFile")]
        [InlineData("PartyFile")]
        public void Every_source_restricts_the_delete_to_one_id_and_one_owner(string which)
        {
            var sql = SqlOf(which == "PostFile" ? Posts : Parties);

            Assert.Contains("@Id", sql);
            Assert.Contains("@PartyId", sql);
            Assert.Contains("WHERE", sql);
            Assert.DoesNotMatch(@"(?i)\bOR\b", sql);
        }

        [Fact]
        public void Post_files_are_owned_through_the_profile_of_the_party()
        {
            var sql = SqlOf(Posts);

            Assert.Contains("dbo.PostFile", sql);
            Assert.Contains("INNER JOIN dbo.ProfileParty AS p ON p.Id = pf.ProfileRef", sql);
            Assert.Contains("p.PartyRef = @PartyId", sql);
            Assert.Contains("pf.Id = @Id", sql);
        }

        [Fact]
        public void Party_files_are_owned_directly_and_limited_to_profile_images()
        {
            var sql = SqlOf(Parties);

            Assert.Contains("dbo.PartyFile", sql);
            Assert.Contains("PartyRef = @PartyId", sql);
            Assert.Contains("[Tables] = N'Profile'", sql);
        }

        [Fact]
        public void Customer_license_files_are_not_deletable_through_the_party_source()
        {
            Assert.DoesNotContain("'Party'", SqlOf(Parties));
            Assert.Contains("N'Profile'", SqlOf(Parties));
        }

        [Fact]
        public async Task Sources_open_their_connection_only_when_used()
        {
            var source = new PostFileSource(new NoConnectionFactory());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                source.DeleteOwnedAsync(Guid.NewGuid(), 1));

            Assert.Contains("no database", ex.Message);
        }
    }
}
