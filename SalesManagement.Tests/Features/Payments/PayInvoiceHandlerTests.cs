using Microsoft.Extensions.Logging.Abstractions;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Payments.PayInvoice;
using SalesManagement.Application.Features.Payments.RequestPayment;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.Invoices;
using SalesManagement.Domain.Features.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    public class PayInvoiceHandlerTests
    {
        private const int OwnerParty = 7823;
        private const int OtherParty = 9001;

        private readonly FakePaymentRepository _payments = new();
        private readonly FakePaymentGateway _gateway = new();
        private readonly FakeInvoiceRepository _invoices = new();

        private PayInvoiceHandler CreateHandler() =>
            new(
                _invoices,
                _payments,
                new RequestPaymentHandler(
                    _payments, _gateway, new FakeCallbackUrlProvider(), NullLogger<RequestPaymentHandler>.Instance));

        private static PayInvoiceCommand Command(int invoiceId = 100, int partyRef = OwnerParty) =>
            new(invoiceId, partyRef, "web", "10.0.0.1");

        [Fact]
        public async Task Unknown_invoice_throws_and_skips_gateway()
        {
            await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command(invoiceId: 555)));

            Assert.Equal(0, _gateway.RequestCalls);
            Assert.Empty(_payments.All);
        }

        [Fact]
        public async Task Invoice_of_another_customer_is_reported_as_not_found()
        {
            _invoices.Add(new PayableInvoice(100, 3833, 248_200_000), ownerPartyRef: OtherParty);

            var ex = await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command()));

            Assert.Equal("فاکتور پیدا نشد", ex.Message);
            Assert.Equal(0, _gateway.RequestCalls);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1000)]
        public async Task Non_positive_net_price_throws(long netPrice)
        {
            _invoices.Add(new PayableInvoice(100, 3833, netPrice), OwnerParty);

            await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command()));

            Assert.Equal(0, _gateway.RequestCalls);
        }

        [Fact]
        public async Task Amount_comes_from_the_invoice_net_price()
        {
            _invoices.Add(new PayableInvoice(100, 3833, 248_200_000), OwnerParty);

            await CreateHandler().Handle(Command());

            Assert.Equal(248_200_000, _gateway.LastRequestAmount);
            Assert.Equal(248_200_000, Assert.Single(_payments.All).Amount);
        }

        [Fact]
        public async Task Payment_is_tagged_with_invoice_table_and_id_and_number_in_description()
        {
            _invoices.Add(new PayableInvoice(100, 3833, 248_200_000), OwnerParty);

            await CreateHandler().Handle(Command());

            var stored = Assert.Single(_payments.All);
            Assert.Equal("Invoice", stored.TableName);
            Assert.Equal(100, stored.TargetRef);
            Assert.Equal(OwnerParty, stored.PartyRef);
            Assert.Contains("3833", stored.Description);
            Assert.Equal(PaymentState.SentToGateway, stored.State);
        }

        [Fact]
        public async Task Success_returns_gateway_payment_url()
        {
            _invoices.Add(new PayableInvoice(100, 3833, 248_200_000), OwnerParty);

            var result = await CreateHandler().Handle(Command());

            Assert.Equal("https://pay.example/AUTH-1", result.PaymentUrl);
        }

        [Fact]
        public async Task Already_paid_invoice_throws_and_skips_gateway()
        {
            _invoices.Add(new PayableInvoice(100, 3833, 248_200_000), OwnerParty);
            _payments.Seed(
                Payment.Create(OwnerParty, "Invoice", 100, "FakeGateway", 248_200_000, "d", "web", "1.1.1.1")
                    .MarkSentToGateway("A", null)
                    .MarkVerified(1, null, null));

            var ex = await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command()));

            Assert.Equal("این فاکتور قبلاً پرداخت شده است", ex.Message);
            Assert.Equal(0, _gateway.RequestCalls);
        }

        [Theory]
        [InlineData(PaymentState.SentToGateway)]
        [InlineData(PaymentState.CanceledByUser)]
        [InlineData(PaymentState.VerifyFailed)]
        public async Task Unfinished_or_failed_previous_payment_does_not_block_a_new_attempt(PaymentState previous)
        {
            _invoices.Add(new PayableInvoice(100, 3833, 248_200_000), OwnerParty);
            var payment = Payment.Create(OwnerParty, "Invoice", 100, "FakeGateway", 248_200_000, "d", "web", "1.1.1.1")
                .MarkSentToGateway("A", null);
            payment = previous switch
            {
                PaymentState.CanceledByUser => payment.MarkCanceledByUser(),
                PaymentState.VerifyFailed => payment.MarkVerifyFailed("-51", "x"),
                _ => payment,
            };
            _payments.Seed(payment);

            var result = await CreateHandler().Handle(Command());

            Assert.NotEqual(payment.Id, result.PaymentId);
            Assert.Equal(1, _gateway.RequestCalls);
        }

        [Fact]
        public async Task Verified_payment_of_another_customer_for_same_invoice_id_does_not_block()
        {
            _invoices.Add(new PayableInvoice(100, 3833, 248_200_000), OwnerParty);
            _payments.Seed(
                Payment.Create(OtherParty, "Invoice", 100, "FakeGateway", 10_000, "d", "web", "1.1.1.1")
                    .MarkSentToGateway("A", null)
                    .MarkVerified(1, null, null));

            var result = await CreateHandler().Handle(Command());

            Assert.Equal("https://pay.example/AUTH-1", result.PaymentUrl);
        }

        [Fact]
        public async Task Verified_payment_for_a_different_invoice_does_not_block()
        {
            _invoices.Add(new PayableInvoice(100, 3833, 248_200_000), OwnerParty);
            _payments.Seed(
                Payment.Create(OwnerParty, "Invoice", 200, "FakeGateway", 10_000, "d", "web", "1.1.1.1")
                    .MarkSentToGateway("A", null)
                    .MarkVerified(1, null, null));

            await CreateHandler().Handle(Command());

            Assert.Equal(1, _gateway.RequestCalls);
        }

        [Fact]
        public async Task Gateway_failure_bubbles_up_as_domain_exception()
        {
            _invoices.Add(new PayableInvoice(100, 3833, 248_200_000), OwnerParty);
            _gateway.RequestResult = new GatewayRequestResult(false, null, null, null, "-9", "invalid");

            await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command()));

            Assert.Equal(PaymentState.RequestFailed, Assert.Single(_payments.All).State);
        }
    }
}
