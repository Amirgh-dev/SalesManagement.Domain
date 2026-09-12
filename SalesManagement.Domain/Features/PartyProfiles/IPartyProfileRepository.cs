namespace SalesManagement.Domain.Features.PartyProfiles
{
    public interface IPartyProfileRepository
    {
        Task<(bool Created, string? OldProfilePhotoFileName)> UpsertAsync(
            PartyProfile profile, CancellationToken cancellationToken = default);
    }
}
