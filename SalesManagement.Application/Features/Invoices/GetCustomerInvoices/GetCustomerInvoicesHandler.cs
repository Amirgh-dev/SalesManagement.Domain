using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Invoices;

namespace SalesManagement.Application.Features.Invoices.GetCustomerInvoices
{
    public class GetCustomerInvoicesHandler(
        IInvoiceRepository invoiceRepository,
        ILogger<GetCustomerInvoicesHandler> logger)
    {
        public async Task<GetCustomerInvoicesResult> Handle(
            GetCustomerInvoicesQuery query, CancellationToken cancellationToken = default)
        {
            if (query.PartyId <= 0)
                throw new DomainException("شناسه‌ی کاربر نامعتبر است");
            if (query.CustomerPartyRef <= 0)
                throw new DomainException("شناسه‌ی مشتری نامعتبر است");

            var invoices = await invoiceRepository.GetForPartyByCustomerAsync(
                query.PartyId, query.CustomerPartyRef, cancellationToken);

            logger.LogInformation(
                "فاکتورهای مشتری {CustomerPartyRef} برای کاربر {PartyId}: {Count}",
                query.CustomerPartyRef, query.PartyId, invoices.Count);

            return new GetCustomerInvoicesResult(
                invoices.Select(InvoiceResultMapper.ToListItem).ToList());
        }
    }
}
