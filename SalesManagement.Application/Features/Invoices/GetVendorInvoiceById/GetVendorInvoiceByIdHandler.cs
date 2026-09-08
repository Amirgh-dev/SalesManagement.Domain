using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Invoices;

namespace SalesManagement.Application.Features.Invoices.GetVendorInvoiceById
{
    public class GetVendorInvoiceByIdHandler(
        IInvoiceRepository invoiceRepository,
        ILogger<GetVendorInvoiceByIdHandler> logger)
    {
        public async Task<GetVendorInvoiceByIdResult> Handle(
            GetVendorInvoiceByIdQuery query, CancellationToken cancellationToken = default)
        {
            if (query.PartyId <= 0)
                throw new DomainException("شناسه‌ی کاربر نامعتبر است");
            if (query.InvoiceId <= 0)
                throw new DomainException("شناسه‌ی فاکتور نامعتبر است");

            var invoice = await invoiceRepository.GetForPartyByIdAsync(
                query.PartyId, query.InvoiceId, cancellationToken);

            if (invoice is null)
            {
                logger.LogWarning(
                    "فاکتور {InvoiceId} برای کاربر {PartyId} پیدا نشد",
                    query.InvoiceId, query.PartyId);
                throw new NotFoundException($"فاکتوری با شناسه‌ی {query.InvoiceId} پیدا نشد");
            }

            logger.LogInformation(
                "فاکتور {InvoiceId} برای کاربر {PartyId} خوانده شد",
                query.InvoiceId, query.PartyId);

            return new GetVendorInvoiceByIdResult(InvoiceResultMapper.ToListItem(invoice));
        }
    }
}
