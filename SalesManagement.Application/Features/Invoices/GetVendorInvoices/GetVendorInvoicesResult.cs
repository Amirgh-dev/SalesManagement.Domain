namespace SalesManagement.Application.Features.Invoices.GetVendorInvoices
{
    public record GetVendorInvoicesResult(IReadOnlyList<InvoiceListItem> Invoices);

    public record InvoiceListItem(
        int InvoiceId,
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
        IReadOnlyList<InvoiceLineItem> Items);

    public record InvoiceLineItem(
        int InvoiceItemId,
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