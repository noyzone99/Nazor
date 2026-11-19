namespace Nazor;

internal static class RenderWorker
{
    private static readonly Queue<Action> work = new();
    private static readonly AutoResetEvent wake = new(false);

    static RenderWorker()
    {
        new Thread(() =>
        {
            while (true)
            {
                wake.WaitOne();
                while (true)
                {
                    Action next;
                    lock (work) { if (!work.TryDequeue(out next!)) break; }
                    next();
                }
            }
        }) { IsBackground = true, Name = "Nazor render" }.Start();
    }

    internal static void Post(Action action)
    {
        lock (work) work.Enqueue(action);
        wake.Set();
    }

    internal static T Invoke<T>(Func<T> action)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try { completion.SetResult(action()); }
            catch (Exception exception) { completion.SetException(exception); }
        });
        return completion.Task.GetAwaiter().GetResult();
    }
}
