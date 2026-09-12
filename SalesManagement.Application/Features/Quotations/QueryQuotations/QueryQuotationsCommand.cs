using SalesManagement.Application.Common;

namespace SalesManagement.Application.Features.Quotations.QueryQuotations
{
    public record QueryQuotationsCommand(
        int PartyId,
        string? Scope,
        string? Fields,
        IReadOnlyList<QueryFilterInput>? Filters,
        QuerySearchInput? Search,
        string? OrderBy,
        bool Descending,
        int? Skip,
        int? Take,
        int? Page,
        int? PageSize);
}
