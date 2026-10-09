namespace SiigoSharp.Tests.Infrastructure;

/// <summary>
/// A clock that tests move by hand. Delays complete at once: the requested duration is recorded and
/// added to the clock, as if that time had passed. That keeps retry tests fast and deterministic.
/// </summary>
internal sealed class TestTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private readonly List<TimeSpan> _delays = [];
    private DateTimeOffset _now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public IReadOnlyList<TimeSpan> Delays
    {
        get
        {
            lock (_gate)
            {
                return [.. _delays];
            }
        }
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _now;
        }
    }

    public void Advance(TimeSpan duration)
    {
        lock (_gate)
        {
            _now += duration;
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        lock (_gate)
        {
            _delays.Add(dueTime);
            _now += dueTime;
        }

        ThreadPool.QueueUserWorkItem(_ => callback(state));
        return new FiredTimer();
    }

    private sealed class FiredTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => false;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
