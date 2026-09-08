using SalesManagement.Domain.Entities;

namespace SalesManagement.Domain.Features.Quotations
{
    public interface IQuotationRepository
    {
        Task<IReadOnlyList<Quotation>> GetForPartyAsync(
            int partyId, CancellationToken cancellationToken = default);

        Task<Quotation?> GetForPartyByIdAsync(
            int partyId, int quotationId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Quotation>> GetForPartyByCustomerAsync(
            int partyId, int customerPartyRef, CancellationToken cancellationToken = default);
    }
}
