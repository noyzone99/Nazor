using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

internal sealed class InspectorInstance : IDisposable
{
    internal sealed record Activation(int VisualStudioProcessId, string? IdeDescriptor, bool FindDebugger, bool Owned = false, string? OwnerHandle = null);
    private readonly Mutex mutex;
    private readonly CancellationTokenSource stopping = new();
    private readonly string pipeName;
    private readonly string discoveryFile;
    private Task? accepting;
    private bool owner;
    internal event Action<Activation>? Activated;
    internal InspectorInstance(string sessionId, string directory)
    {
        if (!Guid.TryParseExact(sessionId, "N", out _)) throw new ArgumentException("Invalid inspector session ID.");
        LocalDevTools.PrepareDirectory(directory);
        var user = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Environment.UserName)))[..12];
        mutex = new(false, "nazor-inspector-" + user + "-" + sessionId);
        pipeName = OperatingSystem.IsWindows() ? "nazor-inspector-" + user + "-" + sessionId : Path.Combine(directory, sessionId + ".ui.sock");
        discoveryFile = Path.Combine(directory, sessionId + ".ui");
    }
    internal bool TryOwn()
    {
        try { owner = mutex.WaitOne(0); } catch (AbandonedMutexException) { owner = true; }
        if (!owner) return false;
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, Share = FileShare.Read };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using (var stream = new FileStream(discoveryFile, options))
            JsonSerializer.Serialize(stream, new { processId = Environment.ProcessId, processStartToken = LocalDevTools.ProcessStartToken(Environment.ProcessId) });
        accepting = Task.Run(AcceptAsync);
        return true;
    }
    internal async Task ActivateAsync(Activation activation)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await pipe.ConnectAsync(timeout.Token).ConfigureAwait(false);
        await IdeWire.WriteAsync(pipe, JsonSerializer.Serialize(activation), timeout.Token).ConfigureAwait(false);
        await IdeWire.ReadAsync(pipe, timeout.Token).ConfigureAwait(false);
    }
    private async Task AcceptAsync()
    {
        while (!stopping.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(pipeName, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                await pipe.WaitForConnectionAsync(stopping.Token).ConfigureAwait(false);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                var json = await IdeWire.ReadAsync(pipe, timeout.Token).ConfigureAwait(false);
                if (json is null || json.Length > 16384) continue;
                var activation = JsonSerializer.Deserialize<Activation>(json) ?? throw new InvalidDataException("Missing inspector activation.");
                if (activation.VisualStudioProcessId < 0 || activation.IdeDescriptor?.Length > 4096 || activation.OwnerHandle?.Length > 4096) continue;
                Activated?.Invoke(activation);
                await IdeWire.WriteAsync(pipe, "{}", timeout.Token).ConfigureAwait(false);
            }
            catch (Exception error) when (error is IOException or OperationCanceledException or JsonException or UnauthorizedAccessException) { }
        }
    }
    public void Dispose()
    {
        stopping.Cancel();
        if (owner)
        {
            try { accepting?.Wait(TimeSpan.FromSeconds(1)); } catch (AggregateException) { }
            try { File.Delete(discoveryFile); } catch (IOException) { }
            mutex.ReleaseMutex();
        }
        mutex.Dispose();
    }
}
