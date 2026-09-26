namespace SalesManagement.Domain.Features.Invoices
{
    public sealed record PayableInvoice(int InvoiceId, int Number, long NetPrice);
}
