using SalesManagement.Application.Features.Invoices.GetVendorInvoices;

namespace SalesManagement.Application.Features.Invoices.GetCustomerInvoices
{
    public record GetCustomerInvoicesResult(IReadOnlyList<InvoiceListItem> Invoices);
}
