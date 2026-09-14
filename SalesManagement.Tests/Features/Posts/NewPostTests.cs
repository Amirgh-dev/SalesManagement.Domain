using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.Posts;

namespace SalesManagement.Tests.Features.Posts
{
    public class NewPostTests
    {
        private static readonly Guid ValidProfileRef = Guid.NewGuid();

        private static NewPost Make(
            Guid? id = null,
            Guid? profileRef = null,
            string? title = "Title",
            string? body = "Body",
            TypePost? type = null)
            => NewPost.Create(id, profileRef ?? ValidProfileRef, title, body, type);

        // ---- validation ---------------------------------------------

        [Fact]
        public void ProfileRef_empty_throws()
        {
            Assert.Throws<DomainException>(() => Make(profileRef: Guid.Empty));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_title_throws(string? title)
        {
            Assert.Throws<DomainException>(() => Make(title: title));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_body_throws(string? body)
        {
            Assert.Throws<DomainException>(() => Make(body: body));
        }

        [Fact]
        public void Title_and_body_are_trimmed()
        {
            var post = Make(title: "  Hello  ", body: "  World  ");
            Assert.Equal("Hello", post.Title);
            Assert.Equal("World", post.Body);
        }

        // ---- id / IsUpdate --------------------------------------------

        [Fact]
        public void Id_null_means_create_and_IsUpdate_is_false()
        {
            var post = Make(id: null);
            Assert.Null(post.Id);
            Assert.False(post.IsUpdate);
        }

        [Fact]
        public void Id_empty_guid_is_treated_as_create()
        {
            var post = Make(id: Guid.Empty);
            Assert.Null(post.Id);
            Assert.False(post.IsUpdate);
        }

        [Fact]
        public void Id_valid_guid_means_update()
        {
            var id = Guid.NewGuid();
            var post = Make(id: id);
            Assert.Equal(id, post.Id);
            Assert.True(post.IsUpdate);
        }

        // ---- type -------------------------------------------------------

        [Fact]
        public void Type_defaults_to_post_when_not_given()
        {
            Assert.Equal(TypePost.Post, Make().Type);
        }

        [Theory]
        [InlineData(TypePost.Urgent)]
        [InlineData(TypePost.Sale)]
        [InlineData(TypePost.Buy)]
        [InlineData(TypePost.Post)]
        public void Type_is_preserved_when_given(TypePost type)
        {
            Assert.Equal(type, Make(type: type).Type);
        }

        // ---- photos -----------------------------------------------------

        [Fact]
        public void PhotoFileNames_default_to_empty()
        {
            Assert.Empty(Make().PhotoFileNames);
        }

        [Fact]
        public void WithPhotos_sets_photo_file_names()
        {
            var post = Make().WithPhotos(new[] { "a.png", "b.png" });
            Assert.Equal(new[] { "a.png", "b.png" }, post.PhotoFileNames);
        }
    }
}
