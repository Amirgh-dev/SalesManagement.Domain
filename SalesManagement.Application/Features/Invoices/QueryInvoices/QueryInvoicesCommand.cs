using SalesManagement.Application.Common;

namespace SalesManagement.Application.Features.Invoices.QueryInvoices
{
    public record QueryInvoicesCommand(
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
