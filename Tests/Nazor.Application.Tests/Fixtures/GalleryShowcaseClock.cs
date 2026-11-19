namespace Nazor.Application.Tests.Fixtures;

internal sealed class GalleryShowcaseClock : TimeProvider
{
    private readonly List<ShowcaseTimer> timers = [];
    private DateTimeOffset now = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => now;
    public int ActiveTimers => timers.Count(timer => timer.Due is not null);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ShowcaseTimer(this, callback, state);
        timers.Add(timer);
        timer.Change(dueTime, period);
        return timer;
    }

    public void Advance(TimeSpan duration)
    {
        var end = now + duration;
        while (timers.Where(timer => timer.Due is { } due && due <= end).OrderBy(timer => timer.Due).FirstOrDefault() is { } timer)
        {
            now = timer.Due!.Value;
            timer.Fire();
        }
        now = end;
    }

    private sealed class ShowcaseTimer(GalleryShowcaseClock clock, TimerCallback callback, object? state) : ITimer
    {
        private TimeSpan period;
        private bool disposed;
        public DateTimeOffset? Due { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan interval)
        {
            if (disposed) return false;
            period = interval;
            Due = dueTime == Timeout.InfiniteTimeSpan ? null : clock.now + dueTime;
            return true;
        }

        public void Fire()
        {
            Due = period <= TimeSpan.Zero ? null : clock.now + period;
            callback(state);
        }

        public void Dispose()
        {
            disposed = true;
            Due = null;
            clock.timers.Remove(this);
        }

        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
