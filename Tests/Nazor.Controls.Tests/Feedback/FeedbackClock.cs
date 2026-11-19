namespace Nazor.Controls.Tests.Feedback;

// A manual clock whose timers fire only when the test advances it.
public sealed class FeedbackClock : TimeProvider
{
    private readonly List<ManualTimer> timers = [];
    private DateTimeOffset now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => now;

    public int ActiveTimers => timers.Count(t => t.Due is not null);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        timers.Add(timer);
        return timer;
    }

    public void Advance(TimeSpan by)
    {
        var end = now + by;
        while (true)
        {
            var next = timers.Where(t => t.Due is { } due && due <= end).OrderBy(t => t.Due).FirstOrDefault();
            if (next is null) break;
            now = next.Due!.Value;
            next.Fire();
        }
        now = end;
    }

    private sealed class ManualTimer(FeedbackClock clock, TimerCallback callback, object? state) : ITimer
    {
        private TimeSpan period;

        public DateTimeOffset? Due { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            this.period = period;
            Due = dueTime == Timeout.InfiniteTimeSpan ? null : clock.now + dueTime;
            return true;
        }

        public void Fire()
        {
            Due = period == Timeout.InfiniteTimeSpan || period == TimeSpan.Zero ? null : clock.now + period;
            callback(state);
        }

        public void Dispose()
        {
            Due = null;
            clock.timers.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
