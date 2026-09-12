using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Posts;

namespace SalesManagement.Tests.Features.Posts
{
    public class NewCommentTests
    {
        private static readonly Guid ValidPostRef = Guid.NewGuid();
        private static readonly Guid ValidProfileRef = Guid.NewGuid();

        private static NewComment Make(
            Guid? id = null,
            Guid? postRef = null,
            Guid? profileRef = null,
            string? text = "Nice post")
            => NewComment.Create(id, postRef ?? ValidPostRef, profileRef ?? ValidProfileRef, text);

        // ---- validation ---------------------------------------------

        [Fact]
        public void PostRef_empty_throws()
        {
            Assert.Throws<DomainException>(() => Make(postRef: Guid.Empty));
        }

        [Fact]
        public void ProfileRef_empty_throws()
        {
            Assert.Throws<DomainException>(() => Make(profileRef: Guid.Empty));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_text_throws(string? text)
        {
            Assert.Throws<DomainException>(() => Make(text: text));
        }

        [Fact]
        public void Text_is_trimmed()
        {
            Assert.Equal("Nice post", Make(text: "  Nice post  ").Text);
        }

        // ---- id / IsUpdate --------------------------------------------

        [Fact]
        public void Id_null_means_create_and_IsUpdate_is_false()
        {
            var comment = Make(id: null);
            Assert.Null(comment.Id);
            Assert.False(comment.IsUpdate);
        }

        [Fact]
        public void Id_empty_guid_is_treated_as_create()
        {
            var comment = Make(id: Guid.Empty);
            Assert.Null(comment.Id);
            Assert.False(comment.IsUpdate);
        }

        [Fact]
        public void Id_valid_guid_means_update()
        {
            var id = Guid.NewGuid();
            var comment = Make(id: id);
            Assert.Equal(id, comment.Id);
            Assert.True(comment.IsUpdate);
        }

        [Fact]
        public void PostRef_and_ProfileRef_are_preserved()
        {
            var postRef = Guid.NewGuid();
            var profileRef = Guid.NewGuid();
            var comment = Make(postRef: postRef, profileRef: profileRef);
            Assert.Equal(postRef, comment.PostRef);
            Assert.Equal(profileRef, comment.ProfileRef);
        }
    }
}
