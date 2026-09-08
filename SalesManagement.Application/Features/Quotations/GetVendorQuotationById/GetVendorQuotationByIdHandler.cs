using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Quotations;

namespace SalesManagement.Application.Features.Quotations.GetVendorQuotationById
{
    public class GetVendorQuotationByIdHandler(
        IQuotationRepository quotationRepository,
        ILogger<GetVendorQuotationByIdHandler> logger)
    {
        public async Task<GetVendorQuotationByIdResult> Handle(
            GetVendorQuotationByIdQuery query, CancellationToken cancellationToken = default)
        {
            if (query.PartyId <= 0)
                throw new DomainException("شناسه‌ی کاربر نامعتبر است");
            if (query.QuotationId <= 0)
                throw new DomainException("شناسه‌ی پیش‌فاکتور نامعتبر است");

            var quotation = await quotationRepository.GetForPartyByIdAsync(
                query.PartyId, query.QuotationId, cancellationToken);

            if (quotation is null)
            {
                logger.LogWarning(
                    "پیش‌فاکتور {QuotationId} برای کاربر {PartyId} پیدا نشد",
                    query.QuotationId, query.PartyId);
                throw new NotFoundException($"پیش‌فاکتوری با شناسه‌ی {query.QuotationId} پیدا نشد");
            }

            logger.LogInformation(
                "پیش‌فاکتور {QuotationId} برای کاربر {PartyId} خوانده شد",
                query.QuotationId, query.PartyId);

            return new GetVendorQuotationByIdResult(QuotationResultMapper.ToListItem(quotation));
        }
    }
}
