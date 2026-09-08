using SalesManagement.Domain.Entities;

namespace SalesManagement.Domain.Features.Invoices
{
    public interface IInvoiceRepository
    {
        Task<IReadOnlyList<Invoice>> GetForPartyAsync(
            int partyId, CancellationToken cancellationToken = default);

        Task<Invoice?> GetForPartyByIdAsync(
            int partyId, int invoiceId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Invoice>> GetForPartyByCustomerAsync(
            int partyId, int customerPartyRef, CancellationToken cancellationToken = default);
    }
}
