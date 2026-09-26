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
    public class RequestPaymentEdgeCaseTests
    {
        private readonly FakePaymentRepository _repository = new();
        private readonly FakePaymentGateway _gateway = new();

        private RequestPaymentHandler CreateHandler() =>
            new(_repository, _gateway, new FakeCallbackUrlProvider(), NullLogger<RequestPaymentHandler>.Instance);

        private static RequestPaymentCommand Command(
            int partyRef = 7823,
            string? tableName = null,
            int? targetRef = null,
            long amount = 1000,
            string description = "d",
            string channel = "web",
            string ip = "1.1.1.1") =>
            new(partyRef, tableName, targetRef, amount, description, channel, ip);

        [Fact]
        public async Task Successful_request_inserts_once_and_updates_once()
        {
            await CreateHandler().Handle(Command());

            Assert.Equal(1, _repository.InsertCalls);
            Assert.Equal(1, _repository.UpdateCalls);
            Assert.Equal(1, _gateway.RequestCalls);
        }

        [Fact]
        public async Task Failed_request_inserts_once_and_updates_once()
        {
            _gateway.RequestResult = new GatewayRequestResult(false, null, null, null, "-9", "x");

            await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command()));

            Assert.Equal(1, _repository.InsertCalls);
            Assert.Equal(1, _repository.UpdateCalls);
        }

        [Fact]
        public async Task Failure_message_shown_to_the_user_hides_gateway_details()
        {
            _gateway.RequestResult = new GatewayRequestResult(false, null, null, null, "-9", "secret gateway detail");

            var ex = await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command()));

            Assert.Equal("امکان اتصال به درگاه پرداخت وجود ندارد", ex.Message);
            Assert.DoesNotContain("secret", ex.Message);
        }

        [Fact]
        public async Task Unexpected_gateway_exception_propagates_and_leaves_a_created_payment_behind()
        {
            _gateway.RequestException = new InvalidOperationException("boom");

            await Assert.ThrowsAsync<InvalidOperationException>(() => CreateHandler().Handle(Command()));

            var stored = Assert.Single(_repository.All);
            Assert.Equal(PaymentState.Created, stored.State);
            Assert.Null(stored.Authority);
            Assert.Equal(0, _repository.UpdateCalls);
        }

        [Theory]
        [InlineData(0, "d", "web", "1.1.1.1")]
        [InlineData(-3, "d", "web", "1.1.1.1")]
        [InlineData(1, "", "web", "1.1.1.1")]
        [InlineData(1, "   ", "web", "1.1.1.1")]
        [InlineData(1, "d", "", "1.1.1.1")]
        [InlineData(1, "d", "web", "")]
        public async Task Invalid_command_data_is_rejected_before_any_side_effect(
            int partyRef, string description, string channel, string ip)
        {
            await Assert.ThrowsAsync<DomainException>(() =>
                CreateHandler().Handle(Command(partyRef, description: description, channel: channel, ip: ip)));

            Assert.Equal(0, _repository.InsertCalls);
            Assert.Equal(0, _gateway.RequestCalls);
        }

        [Fact]
        public async Task Over_long_description_is_rejected_before_any_side_effect()
        {
            await Assert.ThrowsAsync<DomainException>(() =>
                CreateHandler().Handle(Command(description: new string('d', 501))));

            Assert.Equal(0, _repository.InsertCalls);
        }

        [Fact]
        public async Task Long_gateway_error_text_is_truncated_when_stored()
        {
            _gateway.RequestResult = new GatewayRequestResult(false, null, null, null, new string('9', 50), new string('m', 900));

            await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command()));

            var stored = Assert.Single(_repository.All);
            Assert.Equal(20, stored.ErrorCode!.Length);
            Assert.Equal(500, stored.ErrorMessage!.Length);
        }

        [Fact]
        public async Task Fee_may_be_absent()
        {
            _gateway.RequestResult = new GatewayRequestResult(true, "A", "https://pay/A", null, null, null);

            await CreateHandler().Handle(Command());

            Assert.Null(Assert.Single(_repository.All).Fee);
        }

        [Fact]
        public async Task Table_and_target_are_stored_when_supplied_by_a_trusted_caller()
        {
            await CreateHandler().Handle(Command(tableName: "Course", targetRef: 8));

            var stored = Assert.Single(_repository.All);
            Assert.Equal("Course", stored.TableName);
            Assert.Equal(8, stored.TargetRef);
        }

        [Fact]
        public async Task Channel_and_ip_are_stored_as_given()
        {
            await CreateHandler().Handle(Command(channel: "mobile", ip: "2001:db8::1"));

            var stored = Assert.Single(_repository.All);
            Assert.Equal("mobile", stored.Channel);
            Assert.Equal("2001:db8::1", stored.IpAddress);
        }

        [Fact]
        public async Task Gateway_receives_the_description_and_the_cancellation_token()
        {
            using var cts = new CancellationTokenSource();

            await CreateHandler().Handle(Command(description: "پرداخت"), cts.Token);

            Assert.Equal("پرداخت", _gateway.LastRequestDescription);
            Assert.Equal(cts.Token, _gateway.LastRequestToken);
        }

        [Fact]
        public async Task Returned_payment_id_matches_the_stored_record_and_callback_url()
        {
            var result = await CreateHandler().Handle(Command());

            var stored = Assert.Single(_repository.All);
            Assert.Equal(stored.Id, result.PaymentId);
            Assert.Contains(stored.Id.ToString(), _gateway.LastCallbackUrl);
        }

        [Fact]
        public async Task Sequential_requests_create_independent_payments()
        {
            var handler = CreateHandler();

            var first = await handler.Handle(Command());
            var second = await handler.Handle(Command());

            Assert.NotEqual(first.PaymentId, second.PaymentId);
            Assert.Equal(2, _repository.All.Count);
        }

        [Fact]
        public async Task Gateway_name_comes_from_the_gateway_not_from_the_caller()
        {
            await CreateHandler().Handle(Command());

            Assert.Equal(_gateway.Name, Assert.Single(_repository.All).Gateway);
        }
    }

    public class PayInvoiceEdgeCaseTests
    {
        private const int Owner = 7823;
        private const int Other = 9001;

        private readonly FakePaymentRepository _payments = new();
        private readonly FakePaymentGateway _gateway = new();
        private readonly FakeInvoiceRepository _invoices = new();

        private PayInvoiceHandler CreateHandler() =>
            new(
                _invoices,
                _payments,
                new RequestPaymentHandler(
                    _payments, _gateway, new FakeCallbackUrlProvider(), NullLogger<RequestPaymentHandler>.Instance));

        private static PayInvoiceCommand Command(int invoiceId = 100, int party = Owner, string channel = "web", string ip = "1.1.1.1") =>
            new(invoiceId, party, channel, ip);

        private static Payment VerifiedFor(int party, string table, int target) =>
            Payment.Create(party, table, target, "Any", 10_000, "d", "web", "1.1.1.1")
                .MarkSentToGateway("A", null)
                .MarkVerified(1, null, null);

        [Fact]
        public async Task Missing_invoice_is_reported_even_if_a_verified_payment_exists_for_that_id()
        {
            _payments.Seed(VerifiedFor(Owner, "Invoice", 100));

            var ex = await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command()));

            Assert.Equal("فاکتور پیدا نشد", ex.Message);
        }

        [Fact]
        public async Task Verified_payment_for_another_table_with_the_same_target_does_not_block()
        {
            _invoices.Add(new PayableInvoice(100, 5, 1000), Owner);
            _payments.Seed(VerifiedFor(Owner, "Course", 100));

            await CreateHandler().Handle(Command());

            Assert.Equal(1, _gateway.RequestCalls);
        }

        [Fact]
        public async Task Verified_payment_through_a_different_gateway_still_blocks()
        {
            _invoices.Add(new PayableInvoice(100, 5, 1000), Owner);
            _payments.Seed(VerifiedFor(Owner, "Invoice", 100));

            await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command()));
        }

        [Fact]
        public async Task Channel_and_ip_flow_into_the_stored_payment()
        {
            _invoices.Add(new PayableInvoice(100, 5, 1000), Owner);

            await CreateHandler().Handle(Command(channel: "mobile", ip: "9.9.9.9"));

            var stored = Assert.Single(_payments.All);
            Assert.Equal("mobile", stored.Channel);
            Assert.Equal("9.9.9.9", stored.IpAddress);
        }

        [Fact]
        public async Task Minimum_net_price_of_one_is_payable()
        {
            _invoices.Add(new PayableInvoice(100, 5, 1), Owner);

            await CreateHandler().Handle(Command());

            Assert.Equal(1, _gateway.LastRequestAmount);
        }

        [Fact]
        public async Task Very_large_net_price_is_sent_without_overflow()
        {
            _invoices.Add(new PayableInvoice(100, 5, 9_000_000_000_000), Owner);

            await CreateHandler().Handle(Command());

            Assert.Equal(9_000_000_000_000, _gateway.LastRequestAmount);
        }

        [Fact]
        public async Task Two_open_attempts_are_allowed_but_the_first_success_blocks_further_ones()
        {
            _invoices.Add(new PayableInvoice(100, 5, 1000), Owner);
            var handler = CreateHandler();

            var first = await handler.Handle(Command());
            var second = await handler.Handle(Command());
            Assert.NotEqual(first.PaymentId, second.PaymentId);

            var paid = _payments.All.Single(p => p.Id == first.PaymentId);
            _payments.Seed(paid.MarkVerified(1, null, null));

            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(Command()));
        }

        [Fact]
        public async Task Each_customer_only_sees_their_own_invoice_even_with_the_same_invoice_number()
        {
            _invoices.Add(new PayableInvoice(100, 5, 1000), Owner);
            _invoices.Add(new PayableInvoice(200, 5, 2000), Other);
            var handler = CreateHandler();

            await handler.Handle(Command(invoiceId: 100, party: Owner));
            await handler.Handle(Command(invoiceId: 200, party: Other));

            Assert.Equal(1000, _payments.All.Single(p => p.PartyRef == Owner).Amount);
            Assert.Equal(2000, _payments.All.Single(p => p.PartyRef == Other).Amount);
            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(Command(invoiceId: 200, party: Owner)));
        }

        [Fact]
        public async Task Gateway_failure_records_a_request_failed_payment_that_does_not_block_retry()
        {
            _invoices.Add(new PayableInvoice(100, 5, 1000), Owner);
            _gateway.RequestResult = new GatewayRequestResult(false, null, null, null, "NETWORK", "x");
            var handler = CreateHandler();

            await Assert.ThrowsAsync<DomainException>(() => handler.Handle(Command()));
            Assert.Equal(PaymentState.RequestFailed, Assert.Single(_payments.All).State);

            _gateway.RequestResult = new GatewayRequestResult(true, "A", "https://pay/A", 1, null, null);
            var retry = await handler.Handle(Command());

            Assert.Equal("https://pay/A", retry.PaymentUrl);
        }

        [Fact]
        public async Task Persian_description_contains_the_invoice_number_not_its_internal_id()
        {
            _invoices.Add(new PayableInvoice(100, 3833, 1000), Owner);

            await CreateHandler().Handle(Command());

            var description = Assert.Single(_payments.All).Description;
            Assert.Contains("3833", description);
            Assert.DoesNotContain("100", description);
            Assert.StartsWith("پرداخت فاکتور", description);
        }

        [Fact]
        public async Task Cancellation_token_reaches_the_gateway()
        {
            _invoices.Add(new PayableInvoice(100, 5, 1000), Owner);
            using var cts = new CancellationTokenSource();

            await CreateHandler().Handle(Command(), cts.Token);

            Assert.Equal(cts.Token, _gateway.LastRequestToken);
        }
    }
}

namespace SalesManagement.Tests.Features.Payments
{
    public class PaymentCultureIndependenceTests
    {
        [Theory]
        [InlineData("en-US")]
        [InlineData("fa-IR")]
        [InlineData("ar-SA")]
        [InlineData("de-DE")]
        public async Task Invoice_description_and_callback_url_do_not_depend_on_the_current_culture(string cultureName)
        {
            var original = System.Globalization.CultureInfo.CurrentCulture;
            var payments = new FakePaymentRepository();
            var gateway = new FakePaymentGateway();
            var invoices = new FakeInvoiceRepository();
            invoices.Add(new PayableInvoice(1, 1234567, 248_200_000), 7823);
            var handler = new PayInvoiceHandler(
                invoices,
                payments,
                new RequestPaymentHandler(
                    payments, gateway, new FakeCallbackUrlProvider(), NullLogger<RequestPaymentHandler>.Instance));

            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(cultureName);

                var result = await handler.Handle(new PayInvoiceCommand(1, 7823, "web", "1.1.1.1"));

                var stored = Assert.Single(payments.All);
                Assert.Equal("پرداخت فاکتور شماره 1234567", stored.Description);
                Assert.Equal(248_200_000, stored.Amount);
                Assert.EndsWith($"paymentId={result.PaymentId:D}", gateway.LastCallbackUrl);
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = original;
            }
        }
    }
}
