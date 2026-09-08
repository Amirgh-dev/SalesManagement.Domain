using SalesManagement.Application.Features.Quotations.GetVendorQuotations;
using SalesManagement.Domain.Entities;

namespace SalesManagement.Application.Features.Quotations
{
    internal static class QuotationResultMapper
    {
        public static QuotationListItem ToListItem(Quotation q) => new(
            q.Id, q.Number, q.Date, q.ExpirationDate,
            q.CustomerPartyRef, q.CustomerName,
            q.Price, q.Discount, q.Tax, q.NetPrice, q.Closed,
            q.Items.Select(i => new QuotationLineItem(
                i.Id, i.RowId, i.ItemRef, i.ItemCode, i.ItemTitle,
                i.Quantity, i.Fee, i.Price, i.Discount, i.NetPrice, i.Description)).ToList());
    }
}