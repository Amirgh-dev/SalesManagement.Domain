namespace SalesManagement.Domain.Features.Items
{
    public sealed record ItemGrouping(int GroupingId, string? Code, string? Title, int? ParentGroupRef);
}
