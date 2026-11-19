using Nazor.Compiler.Watch;

namespace Nazor.Compiler.Tests;

public sealed class DebouncerTests
{
    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(60);
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

    [Fact]
    public void A_burst_of_changes_is_one_refresh_with_every_path()
    {
        var refreshes = new List<string[]>();
        using var debouncer = new Debouncer(TimeSpan.FromMilliseconds(250), paths => { lock (refreshes) refreshes.Add([.. paths.Order()]); });

        debouncer.Notify("a.nazor");
        debouncer.Notify("b.nss");
        Thread.Sleep(20);
        debouncer.Notify("a.nazor");
        Assert.True(debouncer.WaitIdle(Wait), "the debouncer did not become idle");

        Assert.Equal(["a.nazor", "b.nss"], Assert.Single(refreshes));
        Assert.Equal(1, debouncer.RefreshCount);
        Assert.True(debouncer.WaitIdle(TimeSpan.Zero));
    }

    [Fact]
    public void A_change_during_a_refresh_runs_another_one_after_it()
    {
        using var firstStarted = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var overlaps = 0;
        var running = 0;
        var refreshes = new List<string[]>();
        using var debouncer = new Debouncer(Quiet, paths =>
        {
            if (Interlocked.Increment(ref running) != 1) Interlocked.Increment(ref overlaps);
            lock (refreshes) refreshes.Add([.. paths]);
            if (refreshes.Count == 1)
            {
                firstStarted.Set();
                release.Wait(Wait, TestContext.Current.CancellationToken);
            }
            Interlocked.Decrement(ref running);
        });

        debouncer.Notify("first.nazor");
        Assert.True(firstStarted.Wait(Wait, TestContext.Current.CancellationToken), "the first refresh did not start");
        debouncer.Notify("second.nazor");
        Thread.Sleep(Quiet * 3);
        lock (refreshes) Assert.Single(refreshes);
        Assert.Equal(0, debouncer.RefreshCount);
        release.Set();
        Assert.True(debouncer.WaitIdle(Wait), "the debouncer did not become idle");

        Assert.Equal(2, refreshes.Count);
        Assert.Equal(["first.nazor"], refreshes[0]);
        Assert.Equal(["second.nazor"], refreshes[1]);
        Assert.Equal(0, overlaps);
    }

    [Fact]
    public void A_failing_refresh_is_reported_and_the_next_one_still_runs()
    {
        var failures = new List<Exception>();
        var count = 0;
        using var debouncer = new Debouncer(Quiet, _ =>
        {
            if (Interlocked.Increment(ref count) == 1) throw new InvalidOperationException("boom");
        }, failures.Add);

        debouncer.Notify("x.cs");
        Assert.True(debouncer.WaitIdle(Wait));
        debouncer.Notify("y.cs");
        Assert.True(debouncer.WaitIdle(Wait));

        Assert.Equal(2, count);
        Assert.Equal("boom", Assert.Single(failures).Message);
    }

    [Fact]
    public void Disposing_stops_pending_refreshes()
    {
        var count = 0;
        var debouncer = new Debouncer(Quiet, _ => Interlocked.Increment(ref count));
        debouncer.Notify("x.cs");
        debouncer.Dispose();
        Thread.Sleep(Quiet * 4);
        Assert.Equal(0, count);
        debouncer.Notify("y.cs");
        Assert.True(debouncer.WaitIdle(TimeSpan.Zero));
    }
}
