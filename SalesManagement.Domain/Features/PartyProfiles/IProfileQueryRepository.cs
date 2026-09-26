namespace SalesManagement.Domain.Features.PartyProfiles
{
    public interface IProfileQueryRepository
    {
        Task<(int Total,
              IReadOnlyList<IDictionary<string, object?>>? Profiles,
              IReadOnlyList<IDictionary<string, object?>>? Posts,
              IReadOnlyList<IDictionary<string, object?>>? Comments,
              IReadOnlyList<IDictionary<string, object?>>? Provinces,
              IReadOnlyList<IDictionary<string, object?>>? Counties)> QueryAsync(
            ProfileQuery query, CancellationToken cancellationToken = default);

        Task<bool> HasProfileAsync(int partyId, CancellationToken cancellationToken = default);
    }
}
