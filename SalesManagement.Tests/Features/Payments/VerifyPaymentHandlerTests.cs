using Microsoft.Extensions.Logging.Abstractions;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Payments.VerifyPayment;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    public class VerifyPaymentHandlerTests
    {
        private const string Authority = "AUTH-1";

        private readonly FakePaymentRepository _repository = new();
        private readonly FakePaymentGateway _gateway = new();

        private VerifyPaymentHandler CreateHandler() =>
            new(_repository, _gateway, NullLogger<VerifyPaymentHandler>.Instance);

        private Payment SeedSent(long amount = 250_000) =>
            _repository.Seed(
                Payment.Create(7823, "Invoice", 100, "FakeGateway", amount, "desc", "web", "10.0.0.1")
                    .MarkSentToGateway(Authority, 1000));

        [Fact]
        public async Task Unknown_payment_throws()
        {
            await Assert.ThrowsAsync<DomainException>(() =>
                CreateHandler().Handle(new VerifyPaymentCommand(Guid.NewGuid(), "OK", Authority)));
        }

        [Fact]
        public async Task Payment_not_sent_to_gateway_throws_and_skips_gateway()
        {
            var created = _repository.Seed(
                Payment.Create(7823, null, null, "FakeGateway", 1000, "desc", "web", "10.0.0.1"));

            await Assert.ThrowsAsync<DomainException>(() =>
                CreateHandler().Handle(new VerifyPaymentCommand(created.Id, "OK", null)));

            Assert.Equal(0, _gateway.VerifyCalls);
            Assert.Equal(PaymentState.Created, (await _repository.GetByIdAsync(created.Id))!.State);
        }

        [Theory]
        [InlineData("WRONG")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("auth-1")]
        public async Task Authority_mismatch_throws_and_skips_gateway(string? authority)
        {
            var payment = SeedSent();

            await Assert.ThrowsAsync<DomainException>(() =>
                CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, "OK", authority)));

            Assert.Equal(0, _gateway.VerifyCalls);
            Assert.Equal(PaymentState.SentToGateway, (await _repository.GetByIdAsync(payment.Id))!.State);
        }

        [Theory]
        [InlineData("NOK")]
        [InlineData("")]
        [InlineData("anything")]
        public async Task Non_ok_status_marks_canceled_and_skips_gateway(string status)
        {
            var payment = SeedSent();

            var result = await CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, status, Authority));

            Assert.False(result.IsSuccess);
            Assert.Equal(payment.TrackingCode, result.TrackingCode);
            Assert.Equal(0, _gateway.VerifyCalls);
            Assert.Equal(PaymentState.CanceledByUser, (await _repository.GetByIdAsync(payment.Id))!.State);
        }

        [Theory]
        [InlineData("OK")]
        [InlineData("ok")]
        [InlineData("Ok")]
        public async Task Ok_status_is_case_insensitive_and_verifies(string status)
        {
            var payment = SeedSent();

            var result = await CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, status, Authority));

            Assert.True(result.IsSuccess);
            Assert.Equal(1, _gateway.VerifyCalls);
        }

        [Fact]
        public async Task Successful_verify_marks_payment_paid_with_gateway_details()
        {
            var payment = SeedSent();

            var result = await CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            Assert.True(result.IsSuccess);
            Assert.Equal(payment.TrackingCode, result.TrackingCode);

            var stored = (await _repository.GetByIdAsync(payment.Id))!;
            Assert.Equal(PaymentState.Verified, stored.State);
            Assert.True(stored.IsPay);
            Assert.False(stored.IsOk);
            Assert.Equal(123456, stored.RefId);
            Assert.Equal("603799******1234", stored.CardPan);
            Assert.Equal("card-hash", stored.CardHash);
            Assert.NotNull(stored.VerifiedDate);
        }

        [Fact]
        public async Task Verify_uses_stored_amount_and_authority_not_caller_input()
        {
            var payment = SeedSent(amount: 248_200_000);

            await CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            Assert.Equal(248_200_000, _gateway.LastVerifyAmount);
            Assert.Equal(Authority, _gateway.LastVerifyAuthority);
        }

        [Fact]
        public async Task Gateway_verify_failure_marks_verify_failed_and_not_paid()
        {
            var payment = SeedSent();
            _gateway.VerifyResult = new GatewayVerifyResult(false, null, null, null, "-51", "not paid");

            var result = await CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            Assert.False(result.IsSuccess);
            var stored = (await _repository.GetByIdAsync(payment.Id))!;
            Assert.Equal(PaymentState.VerifyFailed, stored.State);
            Assert.False(stored.IsPay);
            Assert.Equal("-51", stored.ErrorCode);
            Assert.Equal("not paid", stored.ErrorMessage);
        }

        [Fact]
        public async Task Gateway_success_without_ref_id_is_treated_as_failure()
        {
            var payment = SeedSent();
            _gateway.VerifyResult = new GatewayVerifyResult(true, null, "pan", "hash", null, null);

            var result = await CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            Assert.False(result.IsSuccess);
            var stored = (await _repository.GetByIdAsync(payment.Id))!;
            Assert.Equal(PaymentState.VerifyFailed, stored.State);
            Assert.False(stored.IsPay);
        }

        [Fact]
        public async Task Repeated_callback_on_verified_payment_is_idempotent()
        {
            var payment = SeedSent();
            var handler = CreateHandler();

            await handler.Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));
            var updatesAfterFirst = _repository.UpdateCalls;

            var second = await handler.Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            Assert.True(second.IsSuccess);
            Assert.Equal(1, _gateway.VerifyCalls);
            Assert.Equal(updatesAfterFirst, _repository.UpdateCalls);
        }

        [Fact]
        public async Task Cancel_callback_cannot_downgrade_a_verified_payment()
        {
            var payment = SeedSent();
            var handler = CreateHandler();
            await handler.Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            var result = await handler.Handle(new VerifyPaymentCommand(payment.Id, "NOK", Authority));

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentState.Verified, (await _repository.GetByIdAsync(payment.Id))!.State);
        }

        [Fact]
        public async Task Repeated_callback_on_canceled_payment_stays_canceled_and_skips_gateway()
        {
            var payment = SeedSent();
            var handler = CreateHandler();
            await handler.Handle(new VerifyPaymentCommand(payment.Id, "NOK", Authority));

            var result = await handler.Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            Assert.False(result.IsSuccess);
            Assert.Equal(0, _gateway.VerifyCalls);
            Assert.Equal(PaymentState.CanceledByUser, (await _repository.GetByIdAsync(payment.Id))!.State);
        }

        [Fact]
        public async Task Repeated_callback_on_verify_failed_payment_does_not_call_gateway_again()
        {
            var payment = SeedSent();
            _gateway.VerifyResult = new GatewayVerifyResult(false, null, null, null, "-51", "not paid");
            var handler = CreateHandler();
            await handler.Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            var result = await handler.Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            Assert.False(result.IsSuccess);
            Assert.Equal(1, _gateway.VerifyCalls);
        }
    }
}
