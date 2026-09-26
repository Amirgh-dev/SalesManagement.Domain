namespace SalesManagement.Domain.Features.Invoices
{
    public interface IInvoicePayableRepository
    {
        Task<PayableInvoice?> GetAsync(int invoiceId, int partyRef, CancellationToken cancellationToken = default);
    }
}
