namespace SalesManagement.Domain.Features.Invoices
{
    public interface IInvoiceQueryRepository
    {
        Task<(int Total, IReadOnlyList<IDictionary<string, object?>>? Invoices, IReadOnlyList<IDictionary<string, object?>>? Items)> QueryAsync(
            int partyId, InvoiceQuery query, CancellationToken cancellationToken = default);
    }
}
