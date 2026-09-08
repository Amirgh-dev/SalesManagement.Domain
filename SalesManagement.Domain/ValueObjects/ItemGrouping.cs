namespace SalesManagement.Domain.ValueObjects
{
    public sealed record ItemGrouping(int GroupingId, string? Code, string? Title, int? ParentGroupRef);
}
