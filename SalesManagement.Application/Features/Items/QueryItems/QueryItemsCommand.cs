namespace SalesManagement.Application.Features.Items.QueryItems
{
    public record QueryItemsCommand(
        int? Id,
        string? CategoryCode,
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
