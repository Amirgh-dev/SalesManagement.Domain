using Microsoft.Extensions.Logging.Abstractions;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Payments.VerifyPayment;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    public class VerifyPaymentStateMatrixTests
    {
        private const string Authority = "AUTH-1";

        private readonly FakePaymentRepository _repository = new();
        private readonly FakePaymentGateway _gateway = new();

        private VerifyPaymentHandler CreateHandler() =>
            new(_repository, _gateway, NullLogger<VerifyPaymentHandler>.Instance);

        private static Payment Build(PaymentState state)
        {
            var created = Payment.Create(7823, "Invoice", 100, "FakeGateway", 250_000, "d", "web", "1.1.1.1");
            return state switch
            {
                PaymentState.Created => created,
                PaymentState.SentToGateway => created.MarkSentToGateway(Authority, 1000),
                PaymentState.RequestFailed => created.MarkRequestFailed("-9", "x"),
                PaymentState.CanceledByUser => created.MarkSentToGateway(Authority, 1000).MarkCanceledByUser(),
                PaymentState.Verified => created.MarkSentToGateway(Authority, 1000).MarkVerified(1, "p", "h"),
                PaymentState.VerifyFailed => created.MarkSentToGateway(Authority, 1000).MarkVerifyFailed("-51", "x"),
                _ => throw new ArgumentOutOfRangeException(nameof(state)),
            };
        }

        public static TheoryData<PaymentState, string, bool> Matrix()
        {
            var data = new TheoryData<PaymentState, string, bool>();
            foreach (var state in Enum.GetValues<PaymentState>())
                foreach (var status in new[] { "OK", "NOK" })
                    foreach (var authorityMatches in new[] { true, false })
                        data.Add(state, status, authorityMatches);
            return data;
        }

        [Fact]
        public void Matrix_covers_every_state_status_and_authority_combination()
        {
            Assert.Equal(6 * 2 * 2, Matrix().Count());
            Assert.Equal(6, Enum.GetValues<PaymentState>().Length);
        }

        [Theory]
        [MemberData(nameof(Matrix))]
        public async Task Callback_outcome_for_every_state_status_and_authority(
            PaymentState state, string status, bool authorityMatches)
        {
            var payment = _repository.Seed(Build(state));
            var authority = authorityMatches ? Authority : "OTHER";
            var command = new VerifyPaymentCommand(payment.Id, status, authority);
            var isTerminal = state is PaymentState.RequestFailed or PaymentState.CanceledByUser
                or PaymentState.Verified or PaymentState.VerifyFailed;

            if (isTerminal)
            {
                var result = await CreateHandler().Handle(command);

                Assert.Equal(state == PaymentState.Verified, result.IsSuccess);
                Assert.Equal(payment.TrackingCode, result.TrackingCode);
                Assert.Equal(payment, await _repository.GetByIdAsync(payment.Id));
                Assert.Equal(0, _gateway.VerifyCalls);
                Assert.Equal(0, _repository.UpdateCalls);
                return;
            }

            if (state == PaymentState.Created || !authorityMatches)
            {
                await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(command));

                Assert.Equal(payment, await _repository.GetByIdAsync(payment.Id));
                Assert.Equal(0, _gateway.VerifyCalls);
                Assert.Equal(0, _repository.UpdateCalls);
                return;
            }

            var outcome = await CreateHandler().Handle(command);
            var stored = (await _repository.GetByIdAsync(payment.Id))!;

            if (status == "OK")
            {
                Assert.True(outcome.IsSuccess);
                Assert.Equal(PaymentState.Verified, stored.State);
                Assert.True(stored.IsPay);
                Assert.Equal(1, _gateway.VerifyCalls);
            }
            else
            {
                Assert.False(outcome.IsSuccess);
                Assert.Equal(PaymentState.CanceledByUser, stored.State);
                Assert.False(stored.IsPay);
                Assert.Equal(0, _gateway.VerifyCalls);
            }

            Assert.Equal(1, _repository.UpdateCalls);
            Assert.Equal(payment.TrackingCode, outcome.TrackingCode);
        }

        [Theory]
        [InlineData(PaymentState.Verified)]
        [InlineData(PaymentState.CanceledByUser)]
        [InlineData(PaymentState.VerifyFailed)]
        [InlineData(PaymentState.RequestFailed)]
        public async Task Terminal_states_never_change_no_matter_how_many_callbacks_arrive(PaymentState state)
        {
            var payment = _repository.Seed(Build(state));
            var handler = CreateHandler();

            for (var i = 0; i < 5; i++)
            {
                await handler.Handle(new VerifyPaymentCommand(payment.Id, i % 2 == 0 ? "OK" : "NOK", Authority));
            }

            Assert.Equal(payment, await _repository.GetByIdAsync(payment.Id));
            Assert.Equal(0, _gateway.VerifyCalls);
        }

        [Fact]
        public async Task Verify_error_message_from_the_gateway_is_truncated_to_column_size()
        {
            var payment = _repository.Seed(Build(PaymentState.SentToGateway));
            _gateway.VerifyResult = new GatewayVerifyResult(false, null, null, null, new string('9', 60), new string('m', 900));

            await CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            var stored = (await _repository.GetByIdAsync(payment.Id))!;
            Assert.Equal(20, stored.ErrorCode!.Length);
            Assert.Equal(500, stored.ErrorMessage!.Length);
        }

        [Fact]
        public async Task Gateway_exception_leaves_the_payment_untouched_and_retryable()
        {
            var payment = _repository.Seed(Build(PaymentState.SentToGateway));
            _gateway.VerifyException = new HttpRequestException("down");

            await Assert.ThrowsAsync<HttpRequestException>(() =>
                CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority)));

            Assert.Equal(payment, await _repository.GetByIdAsync(payment.Id));
            Assert.Equal(0, _repository.UpdateCalls);

            _gateway.VerifyException = null;
            var retry = await CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            Assert.True(retry.IsSuccess);
        }

        [Fact]
        public async Task Gateway_success_without_card_details_still_verifies()
        {
            var payment = _repository.Seed(Build(PaymentState.SentToGateway));
            _gateway.VerifyResult = new GatewayVerifyResult(true, 42, null, null, null, null);

            var result = await CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            Assert.True(result.IsSuccess);
            var stored = (await _repository.GetByIdAsync(payment.Id))!;
            Assert.Equal(42, stored.RefId);
            Assert.Null(stored.CardPan);
            Assert.Null(stored.CardHash);
        }

        [Fact]
        public async Task Verified_payment_is_not_auto_approved()
        {
            var payment = _repository.Seed(Build(PaymentState.SentToGateway));

            await CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority));

            var stored = (await _repository.GetByIdAsync(payment.Id))!;
            Assert.False(stored.IsOk);
            Assert.Equal(0, stored.Modifier);
        }

        [Fact]
        public async Task Verify_only_touches_the_addressed_payment()
        {
            var target = _repository.Seed(Build(PaymentState.SentToGateway));
            var bystander = _repository.Seed(Build(PaymentState.SentToGateway));

            await CreateHandler().Handle(new VerifyPaymentCommand(target.Id, "OK", Authority));

            Assert.Equal(PaymentState.Verified, (await _repository.GetByIdAsync(target.Id))!.State);
            Assert.Equal(bystander, await _repository.GetByIdAsync(bystander.Id));
        }

        [Fact]
        public async Task Verify_forwards_the_cancellation_token_to_the_gateway()
        {
            var payment = _repository.Seed(Build(PaymentState.SentToGateway));
            using var cts = new CancellationTokenSource();

            await CreateHandler().Handle(new VerifyPaymentCommand(payment.Id, "OK", Authority), cts.Token);

            Assert.Equal(cts.Token, _gateway.LastVerifyToken);
        }

        [Fact]
        public async Task Empty_guid_is_reported_as_not_found()
        {
            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                CreateHandler().Handle(new VerifyPaymentCommand(Guid.Empty, "OK", Authority)));

            Assert.Equal("تراکنش پیدا نشد", ex.Message);
        }
    }
}
