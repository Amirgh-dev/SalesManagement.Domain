using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Quotations;

namespace SalesManagement.Application.Features.Quotations.GetVendorQuotations
{
    public class GetVendorQuotationsHandler(
        IQuotationRepository quotationRepository,
        ILogger<GetVendorQuotationsHandler> logger)
    {
        public async Task<GetVendorQuotationsResult> Handle(
            GetVendorQuotationsQuery query, CancellationToken cancellationToken = default)
        {
            if (query.PartyId <= 0)
                throw new DomainException("شناسه‌ی کاربر نامعتبر است");

            var quotations = await quotationRepository.GetForPartyAsync(
                query.PartyId, cancellationToken);

            logger.LogInformation(
                "پیش‌فاکتورهای کاربر {PartyId}: {Count}", query.PartyId, quotations.Count);

            return new GetVendorQuotationsResult(
                quotations.Select(QuotationResultMapper.ToListItem).ToList());
        }
    }
}
