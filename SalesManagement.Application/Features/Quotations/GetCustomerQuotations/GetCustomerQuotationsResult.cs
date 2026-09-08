using SalesManagement.Application.Features.Quotations.GetVendorQuotations;

namespace SalesManagement.Application.Features.Quotations.GetCustomerQuotations
{
    public record GetCustomerQuotationsResult(IReadOnlyList<QuotationListItem> Quotations);
}
