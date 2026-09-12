namespace SalesManagement.Application.Features.Invoices.QueryInvoices
{
    public sealed record QueryInvoicesResult(
        int Total,
        IReadOnlyList<IDictionary<string, object?>>? Invoices,
        IReadOnlyList<IDictionary<string, object?>>? Items);
}
