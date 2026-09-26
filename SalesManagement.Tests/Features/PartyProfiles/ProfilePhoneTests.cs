using System.Reflection;
using SalesManagement.Persistence.Features.PartyProfiles.QueryProfiles;

namespace SalesManagement.Tests.Features.PartyProfiles
{
    public class ProfilePhoneTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Static;

        private static string MobileApply =>
            (string)typeof(ProfileQueryRepository).GetField("MobileApply", Private)!.GetRawConstantValue()!;

        private static IReadOnlyDictionary<string, string> Fields(string name) =>
            (IReadOnlyDictionary<string, string>)typeof(ProfileQueryRepository).GetField(name, Private)!.GetValue(null)!;

        [Fact]
        public void Phone_is_a_selectable_profile_field()
        {
            Assert.Equal("mobile.Phone", Fields("ProfileFields")["phone"]);
        }

        [Fact]
        public void Phone_field_name_is_case_insensitive()
        {
            Assert.True(Fields("ProfileFields").ContainsKey("PHONE"));
        }

        [Fact]
        public void Phone_is_not_exposed_on_posts_or_comments()
        {
            Assert.False(Fields("PostFields").ContainsKey("phone"));
            Assert.False(Fields("CommentFields").ContainsKey("phone"));
        }

        [Fact]
        public void Phone_is_not_searchable()
        {
            Assert.False(Fields("SearchableFields").ContainsKey("phone"));
        }

        [Fact]
        public void Only_the_main_mobile_of_the_same_party_is_read()
        {
            var sql = MobileApply;

            Assert.Contains("[Sepidar02].[GNR].[PartyPhone]", sql);
            Assert.Contains("ph.PartyRef = p.PartyRef", sql);
            Assert.Contains("ph.Type = 2", sql);
            Assert.Contains("ph.IsMain = 1", sql);
        }

        [Fact]
        public void Numbers_that_are_not_a_valid_mobile_become_null_instead_of_being_returned()
        {
            var sql = MobileApply;

            Assert.Contains("LIKE '09" + string.Concat(Enumerable.Repeat("[0-9]", 9)) + "'", sql);
        }

        [Fact]
        public void Profiles_without_a_phone_are_kept_because_the_join_is_outer()
        {
            Assert.StartsWith("OUTER APPLY", MobileApply.TrimStart());
            Assert.Contains("TOP (1)", MobileApply);
            Assert.EndsWith(") AS mobile", MobileApply.TrimEnd());
        }
    }
}
