namespace SalesManagement.Domain.Features.PartyProfiles
{
    public interface IPartyProfileRepository
    {
        Task<(bool Created, IReadOnlyList<string> DeletedFileNames)> UpsertAsync(
            PartyProfile profile, CancellationToken cancellationToken = default);
    }
}
