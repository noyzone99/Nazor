namespace Nazor.Rendering;

internal sealed class PresenceLifetime(Func<Func<Task>, Task> dispatch, Action changed, Func<Task> exited) : IDisposable
{
    private Timer? timer;
    private int generation;

    public bool Rendered { get; private set; }
    public bool Exiting { get; private set; }

    public async Task UpdateAsync(bool present, TimeSpan duration, bool restartExit = false)
    {
        if (duration < TimeSpan.Zero || duration.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(duration));
        if (present)
        {
            Cancel();
            Rendered = true;
            return;
        }
        if (!Rendered) return;
        if (duration == TimeSpan.Zero)
        {
            Cancel();
            Rendered = false;
            await exited();
            return;
        }
        if (Exiting)
        {
            if (!restartExit) return;
            Cancel();
        }
        Exiting = true;
        var current = ++generation;
        timer = new Timer(_ => _ = dispatch(() => FinishAsync(current)), null, duration, Timeout.InfiniteTimeSpan);
    }

    private async Task FinishAsync(int current)
    {
        if (current != generation || !Exiting) return;
        Cancel();
        Rendered = false;
        changed();
        await exited();
    }

    private void Cancel()
    {
        generation++;
        Exiting = false;
        timer?.Dispose();
        timer = null;
    }

    public void Dispose() => Cancel();
}
