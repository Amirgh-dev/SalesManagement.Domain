using SalesManagement.Application.Features.Invoices.GetVendorInvoices;
using SalesManagement.Domain.Entities;

namespace SalesManagement.Application.Features.Invoices
{
    internal static class InvoiceResultMapper
    {
        public static InvoiceListItem ToListItem(Invoice q) => new(
            q.Id, q.Number, q.Date, q.ExpirationDate,
            q.CustomerPartyRef, q.CustomerName,
            q.Price, q.Discount, q.Tax, q.NetPrice, q.Closed,
            q.Items.Select(i => new InvoiceLineItem(
                i.Id, i.RowId, i.ItemRef, i.ItemCode, i.ItemTitle,
                i.Quantity, i.Fee, i.Price, i.Discount, i.NetPrice, i.Description)).ToList());
    }
}