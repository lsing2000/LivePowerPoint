using System;

namespace LivePowerPoint.Core
{
    /// <summary>Exponential backoff with jitter; resets on success.</summary>
    public sealed class BackoffPolicy
    {
        readonly TimeSpan _baseInterval; readonly TimeSpan _max; readonly Random _rng; int _failures;
        public BackoffPolicy(TimeSpan baseInterval, TimeSpan max, Random rng = null)
        { _baseInterval = baseInterval; _max = max; _rng = rng ?? new Random(); }
        public int Failures => _failures;
        public void Success() => _failures = 0;
        public TimeSpan Failure()
        {
            _failures++;
            var factor = Math.Pow(2, Math.Min(_failures, 10));
            var ms = Math.Min(_baseInterval.TotalMilliseconds * factor, _max.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(ms * (0.75 + _rng.NextDouble() * 0.5)); // +/-25% jitter
        }
        public TimeSpan Next => _failures == 0 ? _baseInterval : TimeSpan.FromMilliseconds(Math.Min(_baseInterval.TotalMilliseconds * Math.Pow(2, Math.Min(_failures, 10)), _max.TotalMilliseconds));
    }

    /// <summary>Skips unchanged versions and rejects stale (older) versions.</summary>
    public sealed class VersionTracker
    {
        long _last = -1;
        public long Last => _last;
        public bool ShouldApply(TableSnapshot s)
        {
            if (s.Version == _last || s.Version < _last) return false;
            return true;
        }
        public void MarkApplied(TableSnapshot s) => _last = s.Version;
        public void Reset() => _last = -1;
    }
}
