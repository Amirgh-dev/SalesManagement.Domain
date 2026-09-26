using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Features.Files;

namespace SalesManagement.Tests.Features.Files
{
    public sealed class FakeFileSource(string name) : IFileSource
    {
        private readonly Dictionary<(Guid FileId, int PartyId), StoredFile> _owned = [];

        public string Name => name;
        public List<(Guid FileId, int PartyId)> Calls { get; } = [];
        public Exception? Failure { get; set; }

        public FakeFileSource Owns(Guid fileId, int partyId, StoredFile file)
        {
            _owned[(fileId, partyId)] = file;
            return this;
        }

        public void Clear()
        {
            _owned.Clear();
            Calls.Clear();
            Failure = null;
        }

        public Task<StoredFile?> DeleteOwnedAsync(Guid fileId, int partyId, CancellationToken cancellationToken = default)
        {
            Calls.Add((fileId, partyId));
            if (Failure is not null) throw Failure;

            if (_owned.Remove((fileId, partyId), out var file))
                return Task.FromResult<StoredFile?>(file);

            return Task.FromResult<StoredFile?>(null);
        }
    }

    public sealed class FakeFileStorage : IFileStorage
    {
        public List<(string Folder, string FileName)> Deleted { get; } = [];
        public Exception? DeleteFailure { get; set; }

        public void Clear()
        {
            Deleted.Clear();
            DeleteFailure = null;
        }

        public Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string folder, string fileName, CancellationToken ct = default)
        {
            if (DeleteFailure is not null) throw DeleteFailure;
            Deleted.Add((folder, fileName));
            return Task.CompletedTask;
        }
    }
}
