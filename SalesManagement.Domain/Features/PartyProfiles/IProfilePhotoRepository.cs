namespace SalesManagement.Domain.Features.PartyProfiles
{
    public interface IProfilePhotoRepository
    {
        Task<string?> ReplaceMainPhotoAsync(int partyRef, string fileName, CancellationToken cancellationToken = default);
    }
}
