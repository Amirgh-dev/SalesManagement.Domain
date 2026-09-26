namespace SalesManagement.Application.Features.Payments.PayInvoice
{
    public record PayInvoiceCommand(int InvoiceId, int PartyRef, string Channel, string IpAddress);
}
