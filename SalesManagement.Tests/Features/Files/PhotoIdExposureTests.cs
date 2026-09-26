using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.PartyProfiles.SaveProfilePhoto;
using SalesManagement.Application.Features.Posts.CreatePost;
using SalesManagement.Domain.Common;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.PartyProfiles;
using SalesManagement.Domain.Features.Posts;
using SalesManagement.Persistence.Features.Posts.CreatePost;

namespace SalesManagement.Tests.Features.Files
{
    public class PhotoIdExposureTests
    {
        private sealed class Storage : IFileStorage
        {
            public List<string> Saved { get; } = [];
            public List<(string Folder, string FileName)> Deleted { get; } = [];

            public Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default)
            {
                var name = $"new{Saved.Count + 1}{extension}";
                Saved.Add(name);
                return Task.FromResult(name);
            }

            public Task DeleteAsync(string folder, string fileName, CancellationToken ct = default)
            {
                Deleted.Add((folder, fileName));
                return Task.CompletedTask;
            }
        }

        private sealed class Urls : IMediaUrlBuilder
        {
            public string? Build(string? path) => path is null ? null : "https://cdn.test" + path;
        }

        private sealed class ProfilePhotos(ReplacedPhoto replaced) : IProfilePhotoRepository
        {
            public Exception? Failure { get; set; }

            public Task<ReplacedPhoto> ReplaceMainPhotoAsync(int partyRef, string fileName, CancellationToken ct = default)
            {
                if (Failure is not null) throw Failure;
                return Task.FromResult(replaced);
            }

            public Task<string?> GetMainPhotoPathAsync(int partyRef, CancellationToken ct = default) =>
                Task.FromResult<string?>(null);
        }

        private sealed class Posts(IReadOnlyList<PostPhotoRef> photos) : IPostRepository
        {
            public Task<PostSaveOutcome> CreateAsync(NewPost post, CancellationToken ct = default) =>
                Task.FromResult(new PostSaveOutcome(Guid.Parse("11111111-1111-1111-1111-111111111111"), [], photos));

            public Task<IReadOnlyList<string>> DeleteAsync(Guid postId, Guid profileRef, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<string>>([]);
        }

        private sealed class NoNotifications : IPostNotificationRepository
        {
            public Task NotifyProvinceOfUrgentPostAsync(Guid profileRef, CancellationToken ct = default) => Task.CompletedTask;
        }

        private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

        [Fact]
        public async Task Create_post_result_gives_every_photo_an_id_and_a_url_in_order()
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            var handler = new CreatePostHandler(
                new Posts([new PostPhotoRef(first, "/media/images/PostFile/a.png"), new PostPhotoRef(second, "/media/images/PostFile/b.png")]),
                new NoNotifications(), new Storage(), new Urls(), NullLogger<CreatePostHandler>.Instance);

            var result = await handler.Handle(
                new CreatePostCommand(null, Guid.NewGuid(), "title", "body", null, []));

            Assert.Equal(2, result.Photos.Count);
            Assert.Equal(first, result.Photos[0].Id);
            Assert.Equal("https://cdn.test/media/images/PostFile/a.png", result.Photos[0].Url);
            Assert.Equal(second, result.Photos[1].Id);
            Assert.Equal("https://cdn.test/media/images/PostFile/b.png", result.Photos[1].Url);
        }

        [Fact]
        public async Task Create_post_result_without_photos_has_an_empty_list()
        {
            var handler = new CreatePostHandler(
                new Posts([]), new NoNotifications(), new Storage(), new Urls(), NullLogger<CreatePostHandler>.Instance);

            var result = await handler.Handle(new CreatePostCommand(null, Guid.NewGuid(), "t", "b", null, []));

            Assert.Empty(result.Photos);
        }

        [Fact]
        public void Create_post_json_contains_id_and_url_for_each_photo()
        {
            var id = Guid.NewGuid();
            var json = JsonSerializer.Serialize(
                new CreatePostResult(Guid.NewGuid(), false, [new PostPhotoResult(id, "/x.png")]), Web);

            using var doc = JsonDocument.Parse(json);
            var photo = doc.RootElement.GetProperty("photos")[0];

            Assert.Equal(id, photo.GetProperty("id").GetGuid());
            Assert.Equal("/x.png", photo.GetProperty("url").GetString());
        }

        [Fact]
        public async Task Saving_a_profile_photo_returns_the_id_the_client_needs_to_delete_it()
        {
            var newId = Guid.NewGuid();
            var storage = new Storage();
            var handler = new SaveProfilePhotoHandler(
                new ProfilePhotos(new ReplacedPhoto(newId, "old.jpg")), storage, new Urls(),
                NullLogger<SaveProfilePhotoHandler>.Instance);

            var result = await handler.Handle(new SaveProfilePhotoCommand(7823, new MemoryStream([1, 2, 3]), ".png", 3));

            Assert.Equal(newId, result.Id);
            Assert.Equal("new1.png", result.FileName);
            Assert.Equal("https://cdn.test/media/images/Profile/new1.png", result.Url);
        }

        [Fact]
        public async Task Saving_a_profile_photo_removes_the_replaced_file_from_disk()
        {
            var storage = new Storage();
            var handler = new SaveProfilePhotoHandler(
                new ProfilePhotos(new ReplacedPhoto(Guid.NewGuid(), "old.jpg")), storage, new Urls(),
                NullLogger<SaveProfilePhotoHandler>.Instance);

            await handler.Handle(new SaveProfilePhotoCommand(7823, new MemoryStream([1]), ".jpg", 1));

            Assert.Equal(("Profile", "old.jpg"), Assert.Single(storage.Deleted));
        }

        [Fact]
        public async Task First_ever_profile_photo_has_nothing_to_remove()
        {
            var storage = new Storage();
            var handler = new SaveProfilePhotoHandler(
                new ProfilePhotos(new ReplacedPhoto(Guid.NewGuid(), null)), storage, new Urls(),
                NullLogger<SaveProfilePhotoHandler>.Instance);

            await handler.Handle(new SaveProfilePhotoCommand(7823, new MemoryStream([1]), ".jpg", 1));

            Assert.Empty(storage.Deleted);
        }

        [Fact]
        public async Task A_failed_profile_photo_save_removes_the_new_file_again()
        {
            var storage = new Storage();
            var photos = new ProfilePhotos(new ReplacedPhoto(Guid.NewGuid(), null)) { Failure = new DomainException("ابتدا پروفایل خود را بسازید") };
            var handler = new SaveProfilePhotoHandler(photos, storage, new Urls(), NullLogger<SaveProfilePhotoHandler>.Instance);

            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(new SaveProfilePhotoCommand(7823, new MemoryStream([1]), ".jpg", 1)));

            Assert.Equal(("Profile", "new1.jpg"), Assert.Single(storage.Deleted));
        }

        [Fact]
        public void Save_profile_photo_json_contains_id_fileName_and_url()
        {
            var id = Guid.NewGuid();
            var json = JsonSerializer.Serialize(new SaveProfilePhotoResult(id, "a.png", "/a.png"), Web);

            using var doc = JsonDocument.Parse(json);

            Assert.Equal(id, doc.RootElement.GetProperty("id").GetGuid());
            Assert.Equal("a.png", doc.RootElement.GetProperty("fileName").GetString());
            Assert.Equal("/a.png", doc.RootElement.GetProperty("url").GetString());
        }

        [Fact]
        public void Post_repository_reads_photo_ids_together_with_paths_main_first()
        {
            var sql = (string)typeof(PostRepository)
                .GetField("SqlSelectPhotoRefs", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;

            Assert.Contains("SELECT Id, Path FROM dbo.PostFile", sql);
            Assert.Contains("WHERE PostRef = @PostRef", sql);
            Assert.Contains("ORDER BY IsMain DESC, Id", sql);
        }
    }
}
