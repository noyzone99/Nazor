using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace Nazor;

/// <summary>
/// Serialises work on the UI thread. Component state, rendering and events are only touched from the dispatcher;
/// other threads switch to it with <c>InvokeAsync</c>.
/// </summary>
public abstract class Dispatcher
{
    /// <summary>Creates a <see cref="UIDispatcher" /> owned by the calling thread.</summary>
    /// <returns>The resulting Dispatcher value.</returns>
    public static Dispatcher CreateDefault() => new UIDispatcher();

    /// <summary>Determines whether the current thread is the dispatcher thread.</summary>
    /// <returns>true if the current thread owns the UI dispatcher; otherwise, false.</returns>
    public abstract bool CheckAccess();

    /// <summary>
    /// The context installed while component code runs, so that <c>await</c> continuations return to the dispatcher.
    /// Null when the dispatcher relies on an ambient context.
    /// </summary>
    public virtual SynchronizationContext? SynchronizationContext => null;

    /// <summary>Throws InvalidOperationException when the current thread is not the dispatcher thread.</summary>
    public void AssertAccess()
    {
        if (!CheckAccess())
            throw new InvalidOperationException(
                "The current thread is not associated with the Dispatcher. Use InvokeAsync() to switch execution to the Dispatcher " +
                "when triggering rendering or changing component state.");
    }

    /// <summary>Schedules the supplied work on the UI dispatcher and observes its completion.</summary>
    /// <param name="workItem">The work to execute on the UI dispatcher.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public abstract Task InvokeAsync(Action workItem);

    /// <summary>Schedules the supplied work on the UI dispatcher and observes its completion.</summary>
    /// <param name="workItem">The work to execute on the UI dispatcher.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public abstract Task InvokeAsync(Func<Task> workItem);

    /// <summary>Schedules the supplied work on the UI dispatcher and observes its completion.</summary>
    /// <param name="workItem">The work to execute on the UI dispatcher.</param>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <returns>A task whose result is the value produced by the operation.</returns>
    public abstract Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem);

    /// <summary>Schedules the supplied work on the UI dispatcher and observes its completion.</summary>
    /// <param name="workItem">The work to execute on the UI dispatcher.</param>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <returns>A task whose result is the value produced by the operation.</returns>
    public abstract Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem);

    /// <summary>
    /// Raised for exceptions thrown by posted work that nobody observes (for example <c>async void</c>).
    /// </summary>
    public event EventHandler<Exception>? UnhandledException;

    /// <summary>Reports an unhandled exception to registered dispatcher exception handlers.</summary>
    /// <param name="exception">The unhandled exception to report.</param>
    /// <returns>true if an exception handler was registered; otherwise, false.</returns>
    protected bool OnUnhandledException(Exception exception)
    {
        var handler = UnhandledException;
        if (handler is null) return false;
        handler(this, exception);
        return true;
    }
}

/// <summary>
/// A dispatcher bound to the thread that creates it. Work posted from other threads, and the continuations of <c>await</c>
/// started on the UI thread, wait in a FIFO queue until the owner runs <see cref="RunPending" />: a platform backend
/// does it from its message loop, a headless test does it explicitly. Calls made on the owner thread run inline.
/// </summary>
public sealed class UIDispatcher : Dispatcher
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(5);
    private readonly ConcurrentQueue<Action> queue = new();
    private readonly AutoResetEvent workPosted = new(false);
    private readonly int ownerThreadId = Environment.CurrentManagedThreadId;
    private readonly SynchronizationContext context;

    /// <summary>Initializes a new instance of UIDispatcher.</summary>
    public UIDispatcher() => context = new DispatcherSynchronizationContext(this);

    /// <summary>Raised on the posting thread after work is queued; backends use it to wake their loop.</summary>
    public event Action? WorkPosted;

    /// <summary>Gets whether work is waiting to execute on the UI dispatcher.</summary>
    public bool HasPendingWork => !queue.IsEmpty;

    /// <summary>Gets the synchronization context that schedules work on this UI dispatcher.</summary>
    public override SynchronizationContext SynchronizationContext => context;

    /// <summary>Determines whether the current thread is the dispatcher thread.</summary>
    /// <returns>true if the current thread owns the UI dispatcher; otherwise, false.</returns>
    public override bool CheckAccess() => Environment.CurrentManagedThreadId == ownerThreadId;

    /// <summary>Queues work for the UI thread and wakes the native event loop.</summary>
    /// <param name="workItem">The work to execute on the UI dispatcher.</param>
    /// <exception cref="ArgumentNullException">workItem is null.</exception>
    public void Post(Action workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        queue.Enqueue(workItem);
        workPosted.Set();
        WorkPosted?.Invoke();
    }

    /// <summary>
    /// Runs the queued work, including work queued meanwhile. Returns the number of items executed.
    /// </summary>
    /// <returns>The resulting int value.</returns>
    public int RunPending()
    {
        AssertAccess();
        var executed = 0;
        while (queue.TryDequeue(out var workItem))
        {
            executed++;
            try
            {
                RunWithContext(workItem);
            }
            catch (Exception exception) when (OnUnhandledException(exception))
            {
            }
        }
        return executed;
    }

    /// <summary>
    /// Runs queued work until <paramref name="condition" /> holds, waiting for posts from other threads.
    /// </summary>
    /// <param name="condition">The condition (Func&lt;bool&gt;).</param>
    /// <param name="timeout">The timeout (TimeSpan).</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool RunUntil(Func<bool> condition, TimeSpan timeout)
    {
        AssertAccess();
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            RunPending();
            if (condition()) return true;
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) return false;

            workPosted.WaitOne(remaining < PollInterval ? remaining : PollInterval);
        }
    }

    /// <summary>Schedules the supplied work on the UI dispatcher and observes its completion.</summary>
    /// <param name="workItem">The work to execute on the UI dispatcher.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public override Task InvokeAsync(Action workItem) => InvokeAsync(() =>
    {
        workItem();
        return true;
    });

    /// <summary>Schedules the supplied work on the UI dispatcher and observes its completion.</summary>
    /// <param name="workItem">The work to execute on the UI dispatcher.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public override Task InvokeAsync(Func<Task> workItem) => InvokeAsync<bool>(async () =>
    {
        await workItem();
        return true;
    });

    /// <summary>Schedules the supplied work on the UI dispatcher and observes its completion.</summary>
    /// <param name="workItem">The work to execute on the UI dispatcher.</param>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <returns>A task whose result is the value produced by the operation.</returns>
    public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
    {
        if (CheckAccess())
        {
            try
            {
                return Task.FromResult(RunWithContext(workItem));
            }
            catch (Exception exception)
            {
                return Task.FromException<TResult>(exception);
            }
        }
        var completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try
            {
                completion.SetResult(workItem());
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        return completion.Task;
    }

    /// <summary>Schedules the supplied work on the UI dispatcher and observes its completion.</summary>
    /// <param name="workItem">The work to execute on the UI dispatcher.</param>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <returns>A task whose result is the value produced by the operation.</returns>
    public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
    {
        if (CheckAccess())
        {
            try
            {
                return RunWithContext(workItem);
            }
            catch (Exception exception)
            {
                return Task.FromException<TResult>(exception);
            }
        }
        var completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(async () =>
        {
            try
            {
                completion.SetResult(await workItem());
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        return completion.Task;
    }

    private void RunWithContext(Action workItem) => RunWithContext(() =>
    {
        workItem();
        return true;
    });

    private T RunWithContext<T>(Func<T> workItem)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            return workItem();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class DispatcherSynchronizationContext(UIDispatcher dispatcher) : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => dispatcher.Post(() => d(state));

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (dispatcher.CheckAccess())
            {
                d(state);
                return;
            }
            ExceptionDispatchInfo? failure = null;
            using var done = new ManualResetEventSlim();
            dispatcher.Post(() =>
            {
                try { d(state); }
                catch (Exception exception) { failure = ExceptionDispatchInfo.Capture(exception); }
                finally { done.Set(); }
            });
            done.Wait();
            failure?.Throw();
        }

        public override SynchronizationContext CreateCopy() => this;
    }
}
