namespace SalesManagement.Domain.Features.PartyProfiles
{
    public interface IProfileQueryRepository
    {
        Task<(int Total,
              IReadOnlyList<IDictionary<string, object?>>? Profiles,
              IReadOnlyList<IDictionary<string, object?>>? Posts,
              IReadOnlyList<IDictionary<string, object?>>? Comments)> QueryAsync(
            ProfileQuery query, CancellationToken cancellationToken = default);
    }
}
