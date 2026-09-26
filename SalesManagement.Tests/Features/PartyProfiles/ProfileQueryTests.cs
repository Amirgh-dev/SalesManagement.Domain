using SalesManagement.Domain.Features.PartyProfiles;

namespace SalesManagement.Tests.Features.PartyProfiles
{
    public class ProfileQueryTests
    {
        private static ProfileQuery Make(
            Guid? id = null,
            int? partyRef = null,
            string? scope = null,
            string? fields = null,
            string? searchTerm = null,
            string? searchFields = null,
            string? orderBy = null,
            bool descending = true,
            int? skip = null,
            int? take = null,
            int? page = null,
            int? pageSize = null)
            => ProfileQuery.Create(
                id, partyRef, scope, fields, searchTerm, searchFields, orderBy, descending, skip, take, page, pageSize);

        [Fact]
        public void Id_null_stays_null()
        {
            Assert.Null(Make(id: null).Id);
        }

        [Fact]
        public void Id_empty_guid_becomes_null()
        {
            Assert.Null(Make(id: Guid.Empty).Id);
        }

        [Fact]
        public void Id_valid_guid_is_kept()
        {
            var id = Guid.NewGuid();
            Assert.Equal(id, Make(id: id).Id);
        }

        [Fact]
        public void PartyRef_null_stays_null()
        {
            Assert.Null(Make(partyRef: null).PartyRef);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void PartyRef_zero_or_negative_becomes_null(int partyRef)
        {
            Assert.Null(Make(partyRef: partyRef).PartyRef);
        }

        [Fact]
        public void PartyRef_positive_is_kept()
        {
            Assert.Equal(7823, Make(partyRef: 7823).PartyRef);
        }

        [Theory]
        [InlineData(null, true, true, true)]
        [InlineData("", true, true, true)]
        [InlineData("string", true, true, true)]
        [InlineData("all", true, true, true)]
        [InlineData("garbage", true, true, true)]
        [InlineData("profiles", true, false, false)]
        [InlineData("profile", true, false, false)]
        [InlineData("posts", false, true, false)]
        [InlineData("post", false, true, false)]
        [InlineData("comments", false, false, true)]
        [InlineData("comment", false, false, true)]
        [InlineData("posts,comments", false, true, true)]
        [InlineData("profiles,posts,comments", true, true, true)]
        [InlineData("  POSTS , Comments  ", false, true, true)]
        public void Scope_is_parsed_leniently(
            string? scope, bool profiles, bool posts, bool comments)
        {
            var s = Make(scope: scope).Scope;
            Assert.Equal(profiles, s.Profiles);
            Assert.Equal(posts, s.Posts);
            Assert.Equal(comments, s.Comments);
        }

        [Fact]
        public void Scope_default_all_has_profiles_as_primary()
        {
            Assert.Equal(ProfileSection.Profiles, Make().Scope.Primary);
        }

        [Fact]
        public void Scope_posts_comments_has_posts_as_primary()
        {
            Assert.Equal(ProfileSection.Posts, Make(scope: "posts,comments").Scope.Primary);
        }

        [Fact]
        public void Scope_comments_only_has_comments_as_primary()
        {
            Assert.Equal(ProfileSection.Comments, Make(scope: "comments").Scope.Primary);
        }

        [Fact]
        public void Fields_null_gives_empty_list()
        {
            Assert.Empty(Make(fields: null).Fields);
        }

        [Fact]
        public void Fields_are_split_trimmed_and_deduplicated()
        {
            var q = Make(fields: "userName, address , userName ,  rate");
            Assert.Equal(new[] { "userName", "address", "rate" }, q.Fields);
        }

        [Fact]
        public void Fields_placeholder_string_token_is_ignored()
        {
            Assert.Empty(Make(fields: "string").Fields);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("string")]
        public void Search_is_null_when_term_is_blank_or_placeholder(string? term)
        {
            Assert.Null(Make(searchTerm: term).Search);
        }

        [Fact]
        public void Search_term_is_trimmed()
        {
            Assert.Equal("mahdi", Make(searchTerm: "  mahdi  ").Search!.Term);
        }

        [Fact]
        public void Search_uses_its_own_fields_when_given()
        {
            var q = Make(searchTerm: "x", searchFields: "storeName, address");
            Assert.Equal(new[] { "storeName", "address" }, q.Search!.Fields);
        }

        [Fact]
        public void Search_fields_empty_when_none_given()
        {
            var q = Make(searchTerm: "x", searchFields: null);
            Assert.Empty(q.Search!.Fields);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("string")]
        public void OrderBy_blank_or_placeholder_becomes_null(string? orderBy)
        {
            Assert.Null(Make(orderBy: orderBy).OrderBy);
        }

        [Fact]
        public void OrderBy_is_trimmed()
        {
            Assert.Equal("rate", Make(orderBy: "  rate  ").OrderBy);
        }

        [Fact]
        public void Descending_flag_is_passed_through()
        {
            Assert.True(Make(descending: true).Descending);
            Assert.False(Make(descending: false).Descending);
        }

        [Fact]
        public void Default_paging_is_skip_0_take_100()
        {
            var q = Make();
            Assert.Equal(0, q.Skip);
            Assert.Equal(100, q.Take);
        }

        [Theory]
        [InlineData(0, 20, 0, 20)]
        [InlineData(5, 20, 5, 20)]
        [InlineData(-3, 20, 0, 20)]
        [InlineData(10, null, 10, 100)]
        [InlineData(0, 0, 0, 100)]
        [InlineData(0, 9999, 0, 200)]
        public void Skip_take_mode(int? skip, int? take, int expectedSkip, int expectedTake)
        {
            var q = Make(skip: skip, take: take);
            Assert.Equal(expectedSkip, q.Skip);
            Assert.Equal(expectedTake, q.Take);
        }

        [Theory]
        [InlineData(1, 10, 0, 10)]
        [InlineData(2, 10, 10, 10)]
        [InlineData(3, 25, 50, 25)]
        [InlineData(1, null, 0, 100)]
        [InlineData(2, 0, 100, 100)]
        [InlineData(2, 99999, 200, 200)]
        public void Page_mode(int? page, int? pageSize, int expectedSkip, int expectedTake)
        {
            var q = Make(page: page, pageSize: pageSize);
            Assert.Equal(expectedSkip, q.Skip);
            Assert.Equal(expectedTake, q.Take);
        }

        [Fact]
        public void PageSize_alone_still_triggers_page_mode_unlike_invoice_query()
        {
            var q = Make(pageSize: 15);
            Assert.Equal(15, q.Take);
            Assert.Equal(0, q.Skip);
        }

        [Fact]
        public void The_full_swagger_placeholder_body_produces_a_safe_default_query()
        {
            var q = Make(
                id: Guid.Empty,
                scope: "string",
                fields: "string",
                searchTerm: "string",
                searchFields: "string",
                orderBy: "string",
                descending: true,
                skip: 0, take: 0, page: 0, pageSize: 0);

            Assert.Null(q.Id);
            Assert.True(q.Scope.Profiles);
            Assert.True(q.Scope.Posts);
            Assert.True(q.Scope.Comments);
            Assert.Empty(q.Fields);
            Assert.Null(q.Search);
            Assert.Null(q.OrderBy);
            Assert.Equal(0, q.Skip);
            Assert.Equal(100, q.Take);
        }
    }
}
