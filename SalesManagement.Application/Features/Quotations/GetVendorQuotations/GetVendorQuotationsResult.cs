namespace SalesManagement.Application.Features.Quotations.GetVendorQuotations
{
    public record GetVendorQuotationsResult(IReadOnlyList<QuotationListItem> Quotations);

    public record QuotationListItem(
        int QuotationId,
        int Number,
        DateTime Date,
        DateTime? ExpirationDate,
        int CustomerPartyRef,
        string CustomerName,
        decimal Price,
        decimal Discount,
        decimal Tax,
        decimal NetPrice,
        bool Closed,
        IReadOnlyList<QuotationLineItem> Items);

    public record QuotationLineItem(
        int QuotationItemId,
        int RowId,
        int ItemRef,
        string ItemCode,
        string ItemTitle,
        decimal Quantity,
        decimal Fee,
        decimal Price,
        decimal Discount,
        decimal NetPrice,
        string? Description);
}