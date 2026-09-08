using SalesManagement.Application.Features.Quotations.GetVendorQuotations;

namespace SalesManagement.Application.Features.Quotations.GetVendorQuotationById
{
    // همان QuotationListItem اسلایسِ لیست را دوباره استفاده می‌کنیم (header + items)
    public record GetVendorQuotationByIdResult(QuotationListItem Quotation);
}
