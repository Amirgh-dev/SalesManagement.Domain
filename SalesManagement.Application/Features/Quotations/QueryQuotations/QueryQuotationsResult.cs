namespace SalesManagement.Application.Features.Quotations.QueryQuotations
{
    public sealed record QueryQuotationsResult(
        int Total,
        IReadOnlyList<IDictionary<string, object?>>? Quotations,
        IReadOnlyList<IDictionary<string, object?>>? Items);
}
