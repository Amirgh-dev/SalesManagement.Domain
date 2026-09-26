using System.Reflection;
using SalesManagement.Domain.Features.PartyProfiles;
using SalesManagement.Persistence.Features.PartyProfiles.QueryProfiles;

namespace SalesManagement.Tests.Features.PartyProfiles
{
    public class CommentOrderingTests
    {
        private static ProfileQuery Query(bool descending, string? orderBy = null, string? scope = "posts,comments") =>
            ProfileQuery.Create(null, null, scope, null, null, null, orderBy, descending, null, null, null, null);

        private static string Order(ProfileQuery query) =>
            (string)typeof(ProfileQueryRepository)
                .GetMethod("ResolveCommentOrderBy", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [query])!;

        [Fact]
        public void Newest_first_when_descending()
        {
            Assert.Equal("c.CreationDate DESC, c.Id ASC", Order(Query(descending: true)));
        }

        [Fact]
        public void Oldest_first_when_ascending()
        {
            Assert.Equal("c.CreationDate ASC, c.Id ASC", Order(Query(descending: false)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("creationDate")]
        [InlineData("date")]
        [InlineData("title")]
        [InlineData("likePost")]
        [InlineData("doesNotExist")]
        public void Direction_does_not_depend_on_which_field_the_posts_are_ordered_by(string? orderBy)
        {
            Assert.Equal("c.CreationDate DESC, c.Id ASC", Order(Query(descending: true, orderBy: orderBy)));
            Assert.Equal("c.CreationDate ASC, c.Id ASC", Order(Query(descending: false, orderBy: orderBy)));
        }

        [Theory]
        [InlineData("posts,comments")]
        [InlineData("comments")]
        [InlineData("profiles,posts,comments")]
        [InlineData(null)]
        public void Ordering_is_the_same_for_every_scope_that_returns_comments(string? scope)
        {
            Assert.Equal("c.CreationDate DESC, c.Id ASC", Order(Query(descending: true, scope: scope)));
        }

        [Fact]
        public void Ties_are_broken_by_id_so_paging_and_repeat_calls_are_stable()
        {
            Assert.EndsWith("c.Id ASC", Order(Query(descending: true)));
            Assert.EndsWith("c.Id ASC", Order(Query(descending: false)));
        }

        [Fact]
        public void The_ordering_column_is_never_listed_twice()
        {
            var columns = Order(Query(descending: true)).Split(',').Select(p => p.Trim().Split(' ')[0]).ToArray();

            Assert.Equal(columns.Length, columns.Distinct().Count());
        }
    }
}
