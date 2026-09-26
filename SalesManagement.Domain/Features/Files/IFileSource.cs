namespace SalesManagement.Domain.Features.Files
{
    public interface IFileSource
    {
        string Name { get; }

        Task<StoredFile?> DeleteOwnedAsync(Guid fileId, int partyId, CancellationToken cancellationToken = default);
    }
}
