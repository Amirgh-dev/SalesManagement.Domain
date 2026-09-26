using Microsoft.Extensions.Logging.Abstractions;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Payments.RequestPayment;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Enums;

namespace SalesManagement.Tests.Features.Payments
{
    public class RequestPaymentHandlerTests
    {
        private readonly FakePaymentRepository _repository = new();
        private readonly FakePaymentGateway _gateway = new();

        private RequestPaymentHandler CreateHandler() =>
            new(_repository, _gateway, new FakeCallbackUrlProvider(), NullLogger<RequestPaymentHandler>.Instance);

        private static RequestPaymentCommand Command(long amount = 250_000) =>
            new(7823, "Invoice", 100, amount, "پرداخت فاکتور", "web", "10.0.0.1");

        [Fact]
        public async Task Success_returns_payment_id_and_gateway_url()
        {
            var result = await CreateHandler().Handle(Command());

            Assert.Equal("https://pay.example/AUTH-1", result.PaymentUrl);
            Assert.Contains(_repository.All, p => p.Id == result.PaymentId);
        }

        [Fact]
        public async Task Success_stores_payment_as_sent_to_gateway_with_authority_and_fee()
        {
            var result = await CreateHandler().Handle(Command());

            var stored = Assert.Single(_repository.All);
            Assert.Equal(result.PaymentId, stored.Id);
            Assert.Equal(PaymentState.SentToGateway, stored.State);
            Assert.Equal("AUTH-1", stored.Authority);
            Assert.Equal(1000, stored.Fee);
            Assert.Equal("FakeGateway", stored.Gateway);
            Assert.Equal(250_000, stored.Amount);
            Assert.Equal("Invoice", stored.TableName);
            Assert.Equal(100, stored.TargetRef);
        }

        [Fact]
        public async Task Success_sends_amount_and_callback_url_with_payment_id_to_gateway()
        {
            var result = await CreateHandler().Handle(Command(amount: 777_000));

            Assert.Equal(1, _gateway.RequestCalls);
            Assert.Equal(777_000, _gateway.LastRequestAmount);
            Assert.Equal("پرداخت فاکتور", _gateway.LastRequestDescription);
            Assert.EndsWith($"paymentId={result.PaymentId}", _gateway.LastCallbackUrl);
        }

        [Fact]
        public async Task Gateway_failure_throws_and_marks_payment_request_failed()
        {
            _gateway.RequestResult = new GatewayRequestResult(false, null, null, null, "-9", "invalid");

            await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command()));

            var stored = Assert.Single(_repository.All);
            Assert.Equal(PaymentState.RequestFailed, stored.State);
            Assert.Equal("-9", stored.ErrorCode);
            Assert.Equal("invalid", stored.ErrorMessage);
            Assert.Null(stored.Authority);
        }

        [Fact]
        public async Task Gateway_success_without_authority_is_treated_as_failure()
        {
            _gateway.RequestResult = new GatewayRequestResult(true, null, "https://pay.example/x", null, null, null);

            await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command()));

            Assert.Equal(PaymentState.RequestFailed, Assert.Single(_repository.All).State);
        }

        [Fact]
        public async Task Network_style_failure_result_is_persisted_as_request_failed()
        {
            _gateway.RequestResult = new GatewayRequestResult(false, null, null, null, "NETWORK", "no connection");

            await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command()));

            Assert.Equal("NETWORK", Assert.Single(_repository.All).ErrorCode);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public async Task Invalid_amount_throws_before_touching_repository_or_gateway(long amount)
        {
            await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command(amount)));

            Assert.Equal(0, _repository.InsertCalls);
            Assert.Equal(0, _gateway.RequestCalls);
        }
    }
}
