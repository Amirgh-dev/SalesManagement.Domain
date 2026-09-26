using Microsoft.Extensions.Logging.Abstractions;
using SalesManagement.Application.Features.Files.DeleteFile;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Files;

namespace SalesManagement.Tests.Features.Files
{
    public class DeleteFileHandlerTests
    {
        private const int Party = 7823;

        private readonly FakeFileSource _posts = new("PostFile");
        private readonly FakeFileSource _parties = new("PartyFile");
        private readonly FakeFileStorage _storage = new();

        private DeleteFileHandler CreateHandler(params IFileSource[] sources) =>
            new(sources.Length > 0 ? sources : [_posts, _parties], _storage, NullLogger<DeleteFileHandler>.Instance);

        [Fact]
        public async Task Deletes_the_physical_file_using_the_folder_and_name_from_the_source()
        {
            var id = Guid.NewGuid();
            _posts.Owns(id, Party, new StoredFile("PostFile", "abc.png"));

            await CreateHandler().Handle(new DeleteFileCommand(id, Party));

            Assert.Equal(("PostFile", "abc.png"), Assert.Single(_storage.Deleted));
        }

        [Fact]
        public async Task Falls_through_to_the_next_source_when_the_first_does_not_own_the_file()
        {
            var id = Guid.NewGuid();
            _parties.Owns(id, Party, new StoredFile("Profile", "me.jpg"));

            await CreateHandler().Handle(new DeleteFileCommand(id, Party));

            Assert.Single(_posts.Calls);
            Assert.Single(_parties.Calls);
            Assert.Equal(("Profile", "me.jpg"), Assert.Single(_storage.Deleted));
        }

        [Fact]
        public async Task Stops_at_the_first_source_that_deleted_the_file()
        {
            var id = Guid.NewGuid();
            _posts.Owns(id, Party, new StoredFile("PostFile", "a.png"));
            _parties.Owns(id, Party, new StoredFile("Profile", "b.png"));

            await CreateHandler().Handle(new DeleteFileCommand(id, Party));

            Assert.Empty(_parties.Calls);
            Assert.Equal("a.png", Assert.Single(_storage.Deleted).FileName);
        }

        [Fact]
        public async Task Passes_the_file_id_and_party_to_every_source_it_asks()
        {
            var id = Guid.NewGuid();

            await Assert.ThrowsAsync<NotFoundException>(() => CreateHandler().Handle(new DeleteFileCommand(id, Party)));

            Assert.Equal((id, Party), Assert.Single(_posts.Calls));
            Assert.Equal((id, Party), Assert.Single(_parties.Calls));
        }

        [Fact]
        public async Task Unknown_file_is_not_found_and_nothing_is_deleted_from_disk()
        {
            var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
                CreateHandler().Handle(new DeleteFileCommand(Guid.NewGuid(), Party)));

            Assert.Equal("فایل پیدا نشد", ex.Message);
            Assert.Empty(_storage.Deleted);
        }

        [Fact]
        public async Task File_owned_by_someone_else_is_reported_exactly_like_a_missing_file()
        {
            var id = Guid.NewGuid();
            _posts.Owns(id, partyId: 1, new StoredFile("PostFile", "x.png"));

            var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
                CreateHandler().Handle(new DeleteFileCommand(id, Party)));

            Assert.Equal("فایل پیدا نشد", ex.Message);
            Assert.Empty(_storage.Deleted);
        }

        [Fact]
        public async Task No_registered_sources_means_not_found()
        {
            await Assert.ThrowsAsync<NotFoundException>(() =>
                new DeleteFileHandler([], _storage, NullLogger<DeleteFileHandler>.Instance)
                    .Handle(new DeleteFileCommand(Guid.NewGuid(), Party)));
        }

        [Theory]
        [MemberData(nameof(InvalidIds))]
        public async Task Missing_or_empty_id_is_rejected_before_asking_any_source(Guid? id)
        {
            await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(new DeleteFileCommand(id, Party)));

            Assert.Empty(_posts.Calls);
            Assert.Empty(_parties.Calls);
        }

        public static TheoryData<Guid?> InvalidIds() => new() { null, Guid.Empty };

        [Fact]
        public async Task A_failing_source_propagates_and_no_file_is_removed()
        {
            _posts.Failure = new InvalidOperationException("db down");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateHandler().Handle(new DeleteFileCommand(Guid.NewGuid(), Party)));

            Assert.Empty(_storage.Deleted);
            Assert.Empty(_parties.Calls);
        }

        [Theory]
        [InlineData(typeof(IOException))]
        [InlineData(typeof(UnauthorizedAccessException))]
        public async Task Disk_failure_after_the_row_is_deleted_does_not_fail_the_request(Type exceptionType)
        {
            var id = Guid.NewGuid();
            _posts.Owns(id, Party, new StoredFile("PostFile", "locked.png"));
            _storage.DeleteFailure = (Exception)Activator.CreateInstance(exceptionType)!;

            await CreateHandler().Handle(new DeleteFileCommand(id, Party));

            Assert.Empty(_storage.Deleted);
        }

        [Fact]
        public async Task Unexpected_disk_errors_are_not_swallowed()
        {
            var id = Guid.NewGuid();
            _posts.Owns(id, Party, new StoredFile("PostFile", "x.png"));
            _storage.DeleteFailure = new InvalidOperationException("bug");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateHandler().Handle(new DeleteFileCommand(id, Party)));
        }

        [Theory]
        [InlineData("..", "a.png")]
        [InlineData(".", "a.png")]
        [InlineData("", "a.png")]
        [InlineData("PostFile", "")]
        [InlineData("PostFile", "..")]
        [InlineData("PostFile", "../secret.txt")]
        [InlineData("PostFile", "..\\secret.txt")]
        [InlineData("PostFile", "sub/a.png")]
        [InlineData("../PostFile", "a.png")]
        [InlineData("..\\PostFile", "a.png")]
        [InlineData("C:\\Windows", "a.png")]
        [InlineData("PostFile/nested", "a.png")]
        public async Task Unsafe_folder_or_file_names_never_reach_the_disk(string folder, string fileName)
        {
            var id = Guid.NewGuid();
            _posts.Owns(id, Party, new StoredFile(folder, fileName));

            await CreateHandler().Handle(new DeleteFileCommand(id, Party));

            Assert.Empty(_storage.Deleted);
        }

        [Fact]
        public async Task Sources_are_asked_in_registration_order()
        {
            var order = new List<string>();
            var first = new OrderedSource("first", order);
            var second = new OrderedSource("second", order);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                CreateHandler(first, second).Handle(new DeleteFileCommand(Guid.NewGuid(), Party)));

            Assert.Equal(new[] { "first", "second" }, order);
        }

        [Fact]
        public async Task A_new_source_added_later_is_used_without_changing_the_handler()
        {
            var id = Guid.NewGuid();
            var future = new FakeFileSource("FutureTable").Owns(id, Party, new StoredFile("Future", "z.webp"));

            await CreateHandler(_posts, _parties, future).Handle(new DeleteFileCommand(id, Party));

            Assert.Equal(("Future", "z.webp"), Assert.Single(_storage.Deleted));
        }

        [Fact]
        public async Task Deleting_the_same_file_twice_succeeds_once_then_reports_not_found()
        {
            var id = Guid.NewGuid();
            _posts.Owns(id, Party, new StoredFile("PostFile", "a.png"));
            var handler = CreateHandler();

            await handler.Handle(new DeleteFileCommand(id, Party));

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new DeleteFileCommand(id, Party)));
            Assert.Single(_storage.Deleted);
        }

        private sealed class OrderedSource(string name, List<string> order) : IFileSource
        {
            public string Name => name;

            public Task<StoredFile?> DeleteOwnedAsync(Guid fileId, int partyId, CancellationToken cancellationToken = default)
            {
                order.Add(name);
                return Task.FromResult<StoredFile?>(null);
            }
        }
    }
}
