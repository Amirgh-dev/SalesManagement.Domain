namespace SalesManagement.Application.Features.PartyProfiles.QueryProfiles
{
    public sealed record QueryProfilesResult(
        int Total,
        IReadOnlyList<IDictionary<string, object?>>? Profiles,
        IReadOnlyList<IDictionary<string, object?>>? Posts,
        IReadOnlyList<IDictionary<string, object?>>? Comments,
        IReadOnlyList<IDictionary<string, object?>>? Provinces,
        IReadOnlyList<IDictionary<string, object?>>? Counties);
}
