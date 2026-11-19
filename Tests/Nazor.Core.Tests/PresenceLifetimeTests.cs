using Nazor.Rendering;

namespace Nazor.Core.Tests;

public sealed class PresenceLifetimeTests
{
    [Fact]
    public async Task Reopening_cancels_retention_and_zero_duration_completes_the_new_exit_once()
    {
        var completions = 0;
        using var lifetime = new PresenceLifetime(action => action(), () => { }, () => { completions++; return Task.CompletedTask; });
        await lifetime.UpdateAsync(true, TimeSpan.Zero);
        await lifetime.UpdateAsync(false, TimeSpan.FromSeconds(5));
        Assert.True(lifetime.Rendered);
        Assert.True(lifetime.Exiting);
        await lifetime.UpdateAsync(true, TimeSpan.Zero);
        Assert.True(lifetime.Rendered);
        Assert.False(lifetime.Exiting);
        Assert.Equal(0, completions);
        await lifetime.UpdateAsync(false, TimeSpan.Zero);
        await lifetime.UpdateAsync(false, TimeSpan.Zero);
        Assert.False(lifetime.Rendered);
        Assert.False(lifetime.Exiting);
        Assert.Equal(1, completions);
    }

    [Fact]
    public async Task Timer_completion_invalidates_after_releasing_the_retained_content()
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var invalidations = 0;
        using var lifetime = new PresenceLifetime(action => action(), () => invalidations++, () =>
        {
            completed.SetResult();
            return Task.CompletedTask;
        });
        await lifetime.UpdateAsync(true, TimeSpan.Zero);
        await lifetime.UpdateAsync(false, TimeSpan.FromMilliseconds(20));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.False(lifetime.Rendered);
        Assert.False(lifetime.Exiting);
        Assert.Equal(1, invalidations);
    }
}
