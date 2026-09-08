using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Quotations;

namespace SalesManagement.Application.Features.Quotations.GetCustomerQuotations
{
    public class GetCustomerQuotationsHandler(
        IQuotationRepository quotationRepository,
        ILogger<GetCustomerQuotationsHandler> logger)
    {
        public async Task<GetCustomerQuotationsResult> Handle(
            GetCustomerQuotationsQuery query, CancellationToken cancellationToken = default)
        {
            if (query.PartyId <= 0)
                throw new DomainException("شناسه‌ی کاربر نامعتبر است");
            if (query.CustomerPartyRef <= 0)
                throw new DomainException("شناسه‌ی مشتری نامعتبر است");

            var quotations = await quotationRepository.GetForPartyByCustomerAsync(
                query.PartyId, query.CustomerPartyRef, cancellationToken);

            logger.LogInformation(
                "پیش‌فاکتورهای مشتری {CustomerPartyRef} برای کاربر {PartyId}: {Count}",
                query.CustomerPartyRef, query.PartyId, quotations.Count);

            return new GetCustomerQuotationsResult(
                quotations.Select(QuotationResultMapper.ToListItem).ToList());
        }
    }
}
