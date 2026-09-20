using System.Collections.Concurrent;


namespace SalesManagement.Domain.Features.Auth.PartyLogin
{
    public sealed class LoginAttemptTracker(TimeProvider timeProvider) : ILoginAttemptTracker
    {
        public const int MaxFailures = 5;
        public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan FailureMemory = TimeSpan.FromMinutes(15);

        private const int PurgeThreshold = 1000;

        private sealed class Entry
        {
            public int Failures;
            public DateTimeOffset LastFailure;
            public DateTimeOffset? LockedUntil;
        }

        private readonly ConcurrentDictionary<string, Entry> _entries = new();

        public TimeSpan? GetLockRemaining(string key)
        {
            if (!_entries.TryGetValue(key, out var entry)) return null;

            lock (entry)
            {
                var remaining = entry.LockedUntil - timeProvider.GetUtcNow();
                return remaining > TimeSpan.Zero ? remaining : null;
            }
        }

        public void RegisterFailure(string key)
        {
            var now = timeProvider.GetUtcNow();
            if (_entries.Count > PurgeThreshold) Purge(now);

            var entry = _entries.GetOrAdd(key, _ => new Entry());
            lock (entry)
            {
                if (entry.Failures > 0 && now - entry.LastFailure > FailureMemory)
                    entry.Failures = 0;

                entry.Failures++;
                entry.LastFailure = now;

                if (entry.Failures >= MaxFailures)
                {
                    entry.LockedUntil = now + LockDuration;
                    entry.Failures = 0;
                }
            }
        }

        public void Reset(string key) => _entries.TryRemove(key, out _);

        private void Purge(DateTimeOffset now)
        {
            foreach (var (key, entry) in _entries)
            {
                lock (entry)
                {
                    var lockActive = entry.LockedUntil > now;
                    if (!lockActive && now - entry.LastFailure > FailureMemory)
                        _entries.TryRemove(key, out _);
                }
            }
        }
    }
}
