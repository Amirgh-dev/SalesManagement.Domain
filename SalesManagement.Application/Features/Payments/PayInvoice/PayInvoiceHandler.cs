using SalesManagement.Application.Features.Payments.RequestPayment;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Invoices;
using SalesManagement.Domain.Features.Payments;

namespace SalesManagement.Application.Features.Payments.PayInvoice
{
    public class PayInvoiceHandler(
        IInvoicePayableRepository invoiceRepository,
        IPaymentRepository paymentRepository,
        RequestPaymentHandler requestPaymentHandler)
    {
        private const string InvoiceTable = "Invoice";

        public async Task<RequestPaymentResult> Handle(
            PayInvoiceCommand cmd, CancellationToken cancellationToken = default)
        {
            var invoice = await invoiceRepository.GetAsync(cmd.InvoiceId, cmd.PartyRef, cancellationToken)
                          ?? throw new DomainException("فاکتور پیدا نشد");

            if (invoice.NetPrice <= 0)
                throw new DomainException("مبلغ فاکتور نامعتبر است");

            if (await paymentRepository.ExistsVerifiedAsync(
                    cmd.PartyRef, InvoiceTable, invoice.InvoiceId, cancellationToken))
                throw new DomainException("این فاکتور قبلاً پرداخت شده است");

            return await requestPaymentHandler.Handle(
                new RequestPaymentCommand(
                    cmd.PartyRef,
                    InvoiceTable,
                    invoice.InvoiceId,
                    invoice.NetPrice,
                    $"پرداخت فاکتور شماره {invoice.Number}",
                    cmd.Channel,
                    cmd.IpAddress),
                cancellationToken);
        }
    }
}
