
using SalesManagement.Domain.Features.Auth.PartyLogin;

namespace SalesManagement.Tests.Features.Auth
{
    internal sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    public class LoginAttemptTrackerTests
    {
        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero));

        private LoginAttemptTracker Make() => new(_time);

        [Fact]
        public void Four_failures_do_not_lock()
        {
            var t = Make();
            for (var i = 0; i < 4; i++) t.RegisterFailure("0911");
            Assert.Null(t.GetLockRemaining("0911"));
        }

        [Fact]
        public void Fifth_failure_locks_for_two_minutes()
        {
            var t = Make();
            for (var i = 0; i < 5; i++) t.RegisterFailure("0911");

            var remaining = t.GetLockRemaining("0911");
            Assert.NotNull(remaining);
            Assert.Equal(TimeSpan.FromMinutes(2), remaining);
        }

        [Fact]
        public void Lock_expires_after_two_minutes()
        {
            var t = Make();
            for (var i = 0; i < 5; i++) t.RegisterFailure("0911");

            _time.Advance(TimeSpan.FromMinutes(1));
            Assert.Equal(TimeSpan.FromMinutes(1), t.GetLockRemaining("0911"));

            _time.Advance(TimeSpan.FromMinutes(1));
            Assert.Null(t.GetLockRemaining("0911"));
        }

        [Fact]
        public void After_lock_ends_counter_starts_over()
        {
            var t = Make();
            for (var i = 0; i < 5; i++) t.RegisterFailure("0911");
            _time.Advance(TimeSpan.FromMinutes(3));

            for (var i = 0; i < 4; i++) t.RegisterFailure("0911");
            Assert.Null(t.GetLockRemaining("0911"));

            t.RegisterFailure("0911");
            Assert.NotNull(t.GetLockRemaining("0911"));
        }

        [Fact]
        public void Success_reset_clears_failures()
        {
            var t = Make();
            for (var i = 0; i < 4; i++) t.RegisterFailure("0911");
            t.Reset("0911");
            t.RegisterFailure("0911");
            Assert.Null(t.GetLockRemaining("0911"));
        }

        [Fact]
        public void Old_failures_are_forgotten()
        {
            var t = Make();
            for (var i = 0; i < 4; i++) t.RegisterFailure("0911");
            _time.Advance(TimeSpan.FromMinutes(16));
            t.RegisterFailure("0911");
            Assert.Null(t.GetLockRemaining("0911"));
        }

        [Fact]
        public void Keys_are_independent()
        {
            var t = Make();
            for (var i = 0; i < 5; i++) t.RegisterFailure("0911");
            Assert.Null(t.GetLockRemaining("0922"));
        }
    }
}
