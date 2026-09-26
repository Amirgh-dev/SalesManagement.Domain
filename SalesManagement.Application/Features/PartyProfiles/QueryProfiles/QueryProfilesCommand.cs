namespace SalesManagement.Application.Features.PartyProfiles.QueryProfiles
{
    public record QueryProfilesCommand(
        int RequesterPartyId,
        Guid? Id,
        int? PartyRef,
        string? Scope,
        string? Fields,
        string? SearchTerm,
        string? SearchFields,
        string? OrderBy,
        bool Descending,
        int? Skip,
        int? Take,
        int? Page,
        int? PageSize,
        int? ProvinceRef,
        int? CountyRef);
}
