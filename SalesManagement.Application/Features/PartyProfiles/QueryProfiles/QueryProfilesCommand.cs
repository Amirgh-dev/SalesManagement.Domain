namespace SalesManagement.Application.Features.PartyProfiles.QueryProfiles
{
    public record QueryProfilesCommand(
        Guid? Id,
        string? Scope,
        string? Fields,
        string? SearchTerm,
        string? SearchFields,
        string? OrderBy,
        bool Descending,
        int? Skip,
        int? Take,
        int? Page,
        int? PageSize);
}
