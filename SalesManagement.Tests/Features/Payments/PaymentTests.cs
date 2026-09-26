using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    public class PaymentTests
    {
        private static Payment Make(
            int partyRef = 7823,
            string? tableName = "Invoice",
            int? targetRef = 100,
            string gateway = "ZarinPal",
            long amount = 250_000,
            string description = "پرداخت فاکتور",
            string channel = "web",
            string ipAddress = "10.0.0.1")
            => Payment.Create(partyRef, tableName, targetRef, gateway, amount, description, channel, ipAddress);

        [Fact]
        public void Create_sets_initial_state()
        {
            var payment = Make();

            Assert.NotEqual(Guid.Empty, payment.Id);
            Assert.Equal(PaymentState.Created, payment.State);
            Assert.False(payment.IsPay);
            Assert.False(payment.IsOk);
            Assert.Equal(0, payment.Modifier);
            Assert.Null(payment.Authority);
            Assert.Null(payment.RefId);
            Assert.Null(payment.VerifiedDate);
        }

        [Fact]
        public void Create_preserves_inputs()
        {
            var payment = Make(
                partyRef: 42, tableName: "Invoice", targetRef: 9, gateway: "ZarinPal", amount: 1234,
                description: "desc", channel: "mobile", ipAddress: "1.2.3.4");

            Assert.Equal(42, payment.PartyRef);
            Assert.Equal("Invoice", payment.TableName);
            Assert.Equal(9, payment.TargetRef);
            Assert.Equal("ZarinPal", payment.Gateway);
            Assert.Equal(1234, payment.Amount);
            Assert.Equal("desc", payment.Description);
            Assert.Equal("mobile", payment.Channel);
            Assert.Equal("1.2.3.4", payment.IpAddress);
        }

        [Fact]
        public void Create_allows_null_table_and_target()
        {
            var payment = Make(tableName: null, targetRef: null);

            Assert.Null(payment.TableName);
            Assert.Null(payment.TargetRef);
        }

        [Fact]
        public void Create_generates_tracking_code_of_fifteen_digits()
        {
            var payment = Make();

            Assert.NotNull(payment.TrackingCode);
            Assert.Equal(15, payment.TrackingCode!.Length);
            Assert.All(payment.TrackingCode, c => Assert.True(char.IsAsciiDigit(c)));
        }

        [Fact]
        public void Create_generates_unique_ids()
        {
            Assert.NotEqual(Make().Id, Make().Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Invalid_party_throws(int partyRef)
        {
            Assert.Throws<DomainException>(() => Make(partyRef: partyRef));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-500)]
        public void Non_positive_amount_throws(long amount)
        {
            Assert.Throws<DomainException>(() => Make(amount: amount));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_gateway_throws(string gateway)
        {
            Assert.Throws<DomainException>(() => Make(gateway: gateway));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_description_throws(string description)
        {
            Assert.Throws<DomainException>(() => Make(description: description));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_channel_throws(string channel)
        {
            Assert.Throws<DomainException>(() => Make(channel: channel));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_ip_throws(string ipAddress)
        {
            Assert.Throws<DomainException>(() => Make(ipAddress: ipAddress));
        }

        [Fact]
        public void Fields_at_max_length_are_accepted()
        {
            var payment = Make(
                gateway: new string('g', 50),
                description: new string('d', 500),
                tableName: new string('t', 200),
                channel: new string('c', 30),
                ipAddress: new string('1', 45));

            Assert.Equal(500, payment.Description.Length);
        }

        [Fact]
        public void Gateway_over_max_length_throws()
        {
            Assert.Throws<DomainException>(() => Make(gateway: new string('g', 51)));
        }

        [Fact]
        public void Description_over_max_length_throws()
        {
            Assert.Throws<DomainException>(() => Make(description: new string('d', 501)));
        }

        [Fact]
        public void TableName_over_max_length_throws()
        {
            Assert.Throws<DomainException>(() => Make(tableName: new string('t', 201)));
        }

        [Fact]
        public void Channel_over_max_length_throws()
        {
            Assert.Throws<DomainException>(() => Make(channel: new string('c', 31)));
        }

        [Fact]
        public void Ip_over_max_length_throws()
        {
            Assert.Throws<DomainException>(() => Make(ipAddress: new string('1', 46)));
        }

        [Fact]
        public void MarkSentToGateway_sets_authority_fee_and_state()
        {
            var sent = Make().MarkSentToGateway("AUTH-1", 1000);

            Assert.Equal(PaymentState.SentToGateway, sent.State);
            Assert.Equal("AUTH-1", sent.Authority);
            Assert.Equal(1000, sent.Fee);
            Assert.False(sent.IsPay);
        }

        [Fact]
        public void Transitions_do_not_mutate_the_original()
        {
            var original = Make();

            var sent = original.MarkSentToGateway("AUTH-1", 1000);

            Assert.Equal(PaymentState.Created, original.State);
            Assert.Null(original.Authority);
            Assert.NotSame(original, sent);
        }

        [Fact]
        public void MarkRequestFailed_sets_state_and_error()
        {
            var failed = Make().MarkRequestFailed("-9", "validation error");

            Assert.Equal(PaymentState.RequestFailed, failed.State);
            Assert.Equal("-9", failed.ErrorCode);
            Assert.Equal("validation error", failed.ErrorMessage);
            Assert.False(failed.IsPay);
        }

        [Fact]
        public void MarkRequestFailed_truncates_long_error_fields()
        {
            var failed = Make().MarkRequestFailed(new string('9', 50), new string('m', 900));

            Assert.Equal(20, failed.ErrorCode!.Length);
            Assert.Equal(500, failed.ErrorMessage!.Length);
        }

        [Fact]
        public void MarkRequestFailed_accepts_null_error_fields()
        {
            var failed = Make().MarkRequestFailed(null, null);

            Assert.Null(failed.ErrorCode);
            Assert.Null(failed.ErrorMessage);
        }

        [Fact]
        public void MarkCanceledByUser_sets_state()
        {
            var canceled = Make().MarkSentToGateway("AUTH-1", null).MarkCanceledByUser();

            Assert.Equal(PaymentState.CanceledByUser, canceled.State);
            Assert.False(canceled.IsPay);
        }

        [Fact]
        public void MarkVerified_sets_paid_fields()
        {
            var verified = Make().MarkSentToGateway("AUTH-1", 1000).MarkVerified(555, "603799******1234", "hash");

            Assert.Equal(PaymentState.Verified, verified.State);
            Assert.True(verified.IsPay);
            Assert.False(verified.IsOk);
            Assert.Equal(555, verified.RefId);
            Assert.Equal("603799******1234", verified.CardPan);
            Assert.Equal("hash", verified.CardHash);
            Assert.NotNull(verified.VerifiedDate);
        }

        [Fact]
        public void MarkVerifyFailed_keeps_IsPay_false_and_truncates_errors()
        {
            var failed = Make().MarkSentToGateway("AUTH-1", 1000)
                .MarkVerifyFailed(new string('1', 40), new string('x', 700));

            Assert.Equal(PaymentState.VerifyFailed, failed.State);
            Assert.False(failed.IsPay);
            Assert.Equal(20, failed.ErrorCode!.Length);
            Assert.Equal(500, failed.ErrorMessage!.Length);
        }

        [Fact]
        public void MarkApproved_sets_IsOk_and_modifier_without_touching_IsPay()
        {
            var verified = Make().MarkSentToGateway("AUTH-1", 1000).MarkVerified(555, null, null);

            var approved = verified.MarkApproved(12);

            Assert.True(approved.IsOk);
            Assert.Equal(12, approved.Modifier);
            Assert.True(approved.IsPay);
            Assert.Equal(PaymentState.Verified, approved.State);
        }

        [Fact]
        public void Transitions_keep_immutable_fields()
        {
            var created = Make();

            var verified = created.MarkSentToGateway("AUTH-1", 1000).MarkVerified(555, null, null).MarkApproved(3);

            Assert.Equal(created.Id, verified.Id);
            Assert.Equal(created.PartyRef, verified.PartyRef);
            Assert.Equal(created.Amount, verified.Amount);
            Assert.Equal(created.TrackingCode, verified.TrackingCode);
            Assert.Equal(created.CreationDate, verified.CreationDate);
            Assert.Equal(created.Gateway, verified.Gateway);
        }

        [Fact]
        public void Reconstitute_restores_every_field()
        {
            var id = Guid.NewGuid();
            var created = new DateTime(2026, 1, 2, 3, 4, 5);
            var verifiedAt = new DateTime(2026, 1, 2, 3, 9, 5);

            var payment = Payment.Reconstitute(
                id, 7, "Invoice", 100, "ZarinPal", 5000, "desc", "AUTH-1", PaymentState.Verified, true, true,
                3, created, 999, "TRK", "pan", "hash", 50, "100", "ok", "web", "1.1.1.1", created, verifiedAt,
                verifiedAt);

            Assert.Equal(id, payment.Id);
            Assert.Equal(7, payment.PartyRef);
            Assert.Equal("Invoice", payment.TableName);
            Assert.Equal(100, payment.TargetRef);
            Assert.Equal(5000, payment.Amount);
            Assert.Equal("AUTH-1", payment.Authority);
            Assert.Equal(PaymentState.Verified, payment.State);
            Assert.True(payment.IsPay);
            Assert.True(payment.IsOk);
            Assert.Equal(3, payment.Modifier);
            Assert.Equal(999, payment.RefId);
            Assert.Equal("TRK", payment.TrackingCode);
            Assert.Equal("pan", payment.CardPan);
            Assert.Equal("hash", payment.CardHash);
            Assert.Equal(50, payment.Fee);
            Assert.Equal("100", payment.ErrorCode);
            Assert.Equal("ok", payment.ErrorMessage);
            Assert.Equal(created, payment.CreationDate);
            Assert.Equal(verifiedAt, payment.VerifiedDate);
            Assert.Equal(verifiedAt, payment.LastModifiedDate);
        }
    }
}
