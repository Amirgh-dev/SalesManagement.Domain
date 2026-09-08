using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Invoices;

namespace SalesManagement.Application.Features.Invoices.GetVendorInvoices
{
    public class GetVendorInvoicesHandler(
        IInvoiceRepository invoiceRepository,
        ILogger<GetVendorInvoicesHandler> logger)
    {
        public async Task<GetVendorInvoicesResult> Handle(
            GetVendorInvoicesQuery query, CancellationToken cancellationToken = default)
        {
            if (query.PartyId <= 0)
                throw new DomainException("شناسه‌ی کاربر نامعتبر است");

            var invoices = await invoiceRepository.GetForPartyAsync(
                query.PartyId, cancellationToken);

            logger.LogInformation(
                "فاکتور های کاربر {PartyId}: {Count}", query.PartyId, invoices.Count);

            return new GetVendorInvoicesResult(
                invoices.Select(InvoiceResultMapper.ToListItem).ToList());
        }
    }
}
