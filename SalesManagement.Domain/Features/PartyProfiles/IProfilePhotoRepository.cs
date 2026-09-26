namespace SalesManagement.Domain.Features.PartyProfiles
{
    public sealed record ReplacedPhoto(Guid NewPhotoId, string? OldFileName);

    public interface IProfilePhotoRepository
    {
        Task<ReplacedPhoto> ReplaceMainPhotoAsync(int partyRef, string fileName, CancellationToken cancellationToken = default);

        Task<string?> GetMainPhotoPathAsync(int partyRef, CancellationToken cancellationToken = default);
    }
}
