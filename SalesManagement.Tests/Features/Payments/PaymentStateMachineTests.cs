using System.Globalization;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.Payments;

namespace SalesManagement.Tests.Features.Payments
{
    public class PaymentStateMachineTests
    {
        private static Payment Fresh() =>
            Payment.Create(7823, "Invoice", 100, "ZarinPal", 250_000, "desc", "web", "10.0.0.1");

        private static Payment InState(PaymentState state)
        {
            var created = Fresh();
            return state switch
            {
                PaymentState.Created => created,
                PaymentState.SentToGateway => created.MarkSentToGateway("AUTH-1", 1000),
                PaymentState.RequestFailed => created.MarkRequestFailed("-9", "x"),
                PaymentState.CanceledByUser => created.MarkSentToGateway("AUTH-1", 1000).MarkCanceledByUser(),
                PaymentState.Verified => created.MarkSentToGateway("AUTH-1", 1000).MarkVerified(1, "p", "h"),
                PaymentState.VerifyFailed => created.MarkSentToGateway("AUTH-1", 1000).MarkVerifyFailed("-51", "x"),
                _ => throw new ArgumentOutOfRangeException(nameof(state)),
            };
        }

        public static TheoryData<PaymentState> States()
        {
            var data = new TheoryData<PaymentState>();
            foreach (var state in Enum.GetValues<PaymentState>()) data.Add(state);
            return data;
        }

        [Fact]
        public void State_values_match_the_numeric_codes_stored_in_the_database()
        {
            Assert.Equal(0, (byte)PaymentState.Created);
            Assert.Equal(1, (byte)PaymentState.SentToGateway);
            Assert.Equal(2, (byte)PaymentState.RequestFailed);
            Assert.Equal(3, (byte)PaymentState.CanceledByUser);
            Assert.Equal(4, (byte)PaymentState.Verified);
            Assert.Equal(5, (byte)PaymentState.VerifyFailed);
            Assert.Equal(6, Enum.GetValues<PaymentState>().Length);
        }

        [Fact]
        public void State_enum_fits_a_tinyint_column()
        {
            Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(PaymentState)));
        }

        [Theory]
        [MemberData(nameof(States))]
        public void Only_a_verified_payment_is_marked_paid(PaymentState state)
        {
            Assert.Equal(state == PaymentState.Verified, InState(state).IsPay);
        }

        [Theory]
        [MemberData(nameof(States))]
        public void No_state_is_approved_without_an_admin(PaymentState state)
        {
            var payment = InState(state);

            Assert.False(payment.IsOk);
            Assert.Equal(0, payment.Modifier);
        }

        [Theory]
        [MemberData(nameof(States))]
        public void Every_transition_keeps_the_immutable_identity_fields(PaymentState state)
        {
            var created = Fresh();
            var payment = InState(state);

            Assert.Equal(created.PartyRef, payment.PartyRef);
            Assert.Equal(created.TableName, payment.TableName);
            Assert.Equal(created.TargetRef, payment.TargetRef);
            Assert.Equal(created.Gateway, payment.Gateway);
            Assert.Equal(created.Amount, payment.Amount);
            Assert.Equal(created.Description, payment.Description);
            Assert.Equal(created.Channel, payment.Channel);
            Assert.Equal(created.IpAddress, payment.IpAddress);
        }

        [Theory]
        [MemberData(nameof(States))]
        public void Only_a_verified_payment_has_ref_id_card_and_verified_date(PaymentState state)
        {
            var payment = InState(state);
            var verified = state == PaymentState.Verified;

            Assert.Equal(verified, payment.RefId is not null);
            Assert.Equal(verified, payment.CardPan is not null);
            Assert.Equal(verified, payment.VerifiedDate is not null);
        }

        [Theory]
        [MemberData(nameof(States))]
        public void Only_failed_states_carry_an_error(PaymentState state)
        {
            var payment = InState(state);
            var failed = state is PaymentState.RequestFailed or PaymentState.VerifyFailed;

            Assert.Equal(failed, payment.ErrorCode is not null);
            Assert.Equal(failed, payment.ErrorMessage is not null);
        }

        [Theory]
        [MemberData(nameof(States))]
        public void Only_states_after_the_gateway_call_have_an_authority(PaymentState state)
        {
            var hasAuthority = state is PaymentState.SentToGateway or PaymentState.CanceledByUser
                or PaymentState.Verified or PaymentState.VerifyFailed;

            Assert.Equal(hasAuthority, InState(state).Authority is not null);
        }

        [Fact]
        public void Last_modified_date_never_goes_backwards_across_transitions()
        {
            var created = Fresh();
            var sent = created.MarkSentToGateway("A", null);
            var verified = sent.MarkVerified(1, null, null);

            Assert.True(sent.LastModifiedDate >= created.LastModifiedDate);
            Assert.True(verified.LastModifiedDate >= sent.LastModifiedDate);
        }

        [Fact]
        public void Verified_date_is_set_at_verification_time()
        {
            var sent = Fresh().MarkSentToGateway("A", null);
            var before = DateTime.Now;

            var verified = sent.MarkVerified(1, null, null);

            Assert.InRange(verified.VerifiedDate!.Value, before, DateTime.Now);
        }

        [Fact]
        public void Approval_updates_modification_fields_but_not_the_verification_date()
        {
            var verified = Fresh().MarkSentToGateway("A", null).MarkVerified(1, null, null);

            var approved = verified.MarkApproved(3);

            Assert.Equal(verified.VerifiedDate, approved.VerifiedDate);
            Assert.True(approved.LastModificationDate >= verified.LastModificationDate);
        }

        [Fact]
        public void Approval_can_be_recorded_by_different_admins_and_keeps_the_latest()
        {
            var verified = Fresh().MarkSentToGateway("A", null).MarkVerified(1, null, null);

            var approved = verified.MarkApproved(3).MarkApproved(9);

            Assert.Equal(9, approved.Modifier);
        }

        [Fact]
        public void Every_transition_returns_a_new_instance()
        {
            var created = Fresh();

            Assert.NotSame(created, created.MarkSentToGateway("A", null));
            Assert.NotSame(created, created.MarkRequestFailed(null, null));
            Assert.NotSame(created, created.MarkCanceledByUser());
            Assert.NotSame(created, created.MarkVerified(1, null, null));
            Assert.NotSame(created, created.MarkVerifyFailed(null, null));
            Assert.NotSame(created, created.MarkApproved(1));
            Assert.Equal(PaymentState.Created, created.State);
        }

        [Fact]
        public void Records_with_the_same_values_are_equal_and_different_states_are_not()
        {
            var created = Fresh();
            var sameAgain = created with { };

            Assert.Equal(created, sameAgain);
            Assert.NotEqual(created, created.MarkCanceledByUser());
        }

        [Fact]
        public void Tracking_codes_of_many_new_payments_are_well_formed()
        {
            for (var i = 0; i < 200; i++)
            {
                var code = Fresh().TrackingCode!;
                Assert.Equal(15, code.Length);
                Assert.All(code, c => Assert.True(char.IsAsciiDigit(c)));
            }
        }

        [Fact]
        public void Tracking_code_stays_the_same_across_every_transition()
        {
            var created = Fresh();

            var final = created.MarkSentToGateway("A", 1).MarkVerified(1, null, null).MarkApproved(1);

            Assert.Equal(created.TrackingCode, final.TrackingCode);
        }

        [Fact]
        public void Tracking_code_does_not_depend_on_the_current_culture()
        {
            var original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fa-IR");
                var code = Fresh().TrackingCode!;

                Assert.Equal(15, code.Length);
                Assert.All(code, c => Assert.True(char.IsAsciiDigit(c)));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }
    }
}

namespace SalesManagement.Tests.Features.Payments
{
    public class TrackingCodeCultureTests
    {
        private static Payment Fresh() =>
            Payment.Create(7823, "Invoice", 100, "ZarinPal", 250_000, "desc", "web", "10.0.0.1");

        private static CultureInfo PersianCalendarCulture()
        {
            var culture = new CultureInfo("fa-IR");
            culture.DateTimeFormat.Calendar = new PersianCalendar();
            return culture;
        }

        private static string CreateUnder(CultureInfo culture)
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = culture;
                return Fresh().TrackingCode!;
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void Tracking_code_uses_the_gregorian_date_even_when_the_culture_uses_the_persian_calendar()
        {
            var gregorianYear = DateTime.Now.ToString("yy", CultureInfo.InvariantCulture);

            var code = CreateUnder(PersianCalendarCulture());

            Assert.StartsWith(gregorianYear, code);
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("fa-IR")]
        [InlineData("ar-SA")]
        [InlineData("th-TH")]
        public void Tracking_code_year_is_the_same_under_every_culture(string cultureName)
        {
            var gregorianYear = DateTime.Now.ToString("yy", CultureInfo.InvariantCulture);

            var code = CreateUnder(new CultureInfo(cultureName));

            Assert.StartsWith(gregorianYear, code);
            Assert.Equal(15, code.Length);
            Assert.All(code, c => Assert.True(char.IsAsciiDigit(c)));
        }
    }
}
