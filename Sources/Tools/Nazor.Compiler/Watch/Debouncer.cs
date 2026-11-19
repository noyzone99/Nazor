namespace Nazor.Compiler.Watch;

internal sealed class Debouncer : IDisposable
{
    private readonly TimeSpan quietPeriod;
    private readonly Action<IReadOnlyCollection<string>> refresh;
    private readonly Action<Exception>? failed;
    private readonly Lock gate = new();
    private readonly HashSet<string> pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly Timer timer;
    private readonly ManualResetEventSlim idle = new(true);
    private DateTime due;
    private bool running;
    private bool disposed;

    public Debouncer(TimeSpan quietPeriod, Action<IReadOnlyCollection<string>> refresh, Action<Exception>? failed = null)
    {
        ArgumentNullException.ThrowIfNull(refresh);
        this.quietPeriod = quietPeriod;
        this.refresh = refresh;
        this.failed = failed;
        timer = new Timer(_ => Fire(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public int RefreshCount { get; private set; }

    public void Notify(string path)
    {
        lock (gate)
        {
            if (disposed) return;
            pending.Add(path);
            due = DateTime.UtcNow + quietPeriod;
            idle.Reset();
            if (!running) timer.Change(quietPeriod, Timeout.InfiniteTimeSpan);
        }
    }

    public bool WaitIdle(TimeSpan timeout) => idle.Wait(timeout);

    private void Fire()
    {
        string[] paths;
        lock (gate)
        {
            if (disposed || running) return;
            var remaining = due - DateTime.UtcNow;
            if (remaining > TimeSpan.Zero)
            {
                timer.Change(remaining, Timeout.InfiniteTimeSpan);
                return;
            }
            if (pending.Count == 0)
            {
                idle.Set();
                return;
            }
            paths = [.. pending];
            pending.Clear();
            running = true;
        }
        try
        {
            refresh(paths);
        }
        catch (Exception exception)
        {
            failed?.Invoke(exception);
        }
        finally
        {
            lock (gate)
            {
                running = false;
                RefreshCount++;
                if (disposed || pending.Count == 0) idle.Set();
                else
                {
                    var remaining = due - DateTime.UtcNow;
                    timer.Change(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, Timeout.InfiniteTimeSpan);
                }
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            pending.Clear();
            timer.Dispose();
            if (!running) idle.Set();
        }
    }
}
