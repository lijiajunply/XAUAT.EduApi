using System.Collections.Concurrent;

namespace XAUAT.EduApi.Services;

public class StudentRateLimitState : IStudentRateLimitState
{
    private static readonly TimeSpan[] CooldownSteps =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(4)
    ];

    private readonly ConcurrentDictionary<string, StudentRateLimitEntry> _states = new();

    public bool TryGetBlockedUntil(string identityKey, out DateTimeOffset blockedUntil)
    {
        blockedUntil = default;

        if (string.IsNullOrWhiteSpace(identityKey))
        {
            return false;
        }

        if (!_states.TryGetValue(identityKey, out var entry))
        {
            return false;
        }

        if (entry.BlockedUntil <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        blockedUntil = entry.BlockedUntil;
        return true;
    }

    public TimeSpan MarkRateLimited(string identityKey)
    {
        if (string.IsNullOrWhiteSpace(identityKey))
        {
            return TimeSpan.Zero;
        }

        var now = DateTimeOffset.UtcNow;
        var entry = _states.AddOrUpdate(
            identityKey,
            _ => new StudentRateLimitEntry
            {
                HitCount = 1,
                LastRateLimitedAt = now,
                BlockedUntil = now.Add(CooldownSteps[0])
            },
            (_, current) =>
            {
                var nextHitCount = Math.Min(current.HitCount + 1, CooldownSteps.Length);
                var cooldown = CooldownSteps[nextHitCount - 1];

                return new StudentRateLimitEntry
                {
                    HitCount = nextHitCount,
                    LastRateLimitedAt = now,
                    BlockedUntil = now.Add(cooldown)
                };
            });

        return entry.BlockedUntil - now;
    }

    public void MarkSuccess(string identityKey)
    {
        if (string.IsNullOrWhiteSpace(identityKey))
        {
            return;
        }

        _states.TryRemove(identityKey, out _);
    }

    private sealed class StudentRateLimitEntry
    {
        public int HitCount { get; init; }
        public DateTimeOffset LastRateLimitedAt { get; init; }
        public DateTimeOffset BlockedUntil { get; init; }
    }
}
