using System.Collections.Concurrent;

namespace SchoolsManagement.Api.Services;

public static class LoginAttemptTracker
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
    private static readonly ConcurrentDictionary<string, AttemptState> States = new();

    public static bool IsLocked(string key, out TimeSpan? wait)
    {
        wait = null;
        if (!States.TryGetValue(key, out var state))
        {
            return false;
        }

        if (state.LockedUntil is { } until && until > DateTimeOffset.UtcNow)
        {
            wait = until - DateTimeOffset.UtcNow;
            return true;
        }

        if (state.LockedUntil is not null)
        {
            state.Failures = 0;
            state.LockedUntil = null;
        }

        return false;
    }

    public static void RecordFailure(string key)
    {
        var state = States.GetOrAdd(key, _ => new AttemptState());
        state.Failures++;
        if (state.Failures >= MaxAttempts)
        {
            state.LockedUntil = DateTimeOffset.UtcNow.Add(LockDuration);
        }
    }

    public static void Clear(string key) => States.TryRemove(key, out _);

    private sealed class AttemptState
    {
        public int Failures;
        public DateTimeOffset? LockedUntil;
    }
}
