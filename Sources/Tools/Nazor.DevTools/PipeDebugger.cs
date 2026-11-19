using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

public sealed class PipeDebugger(string descriptorPath) : IInspectorDebugger
{
    private sealed record Descriptor(int Version, int ProcessId, string ProcessStartToken, int HostProcessId, string PipeName, string Token);
    private readonly SemaphoreSlim gate = new(1);
    internal static string? Find(int processId)
    {
        var directory = LocalDevTools.DiscoveryDirectory;
        if (!Directory.Exists(directory)) return null;
        LocalDevTools.PrepareDirectory(directory);
        foreach (var file in Directory.EnumerateFiles(directory, "*.ide").Take(64))
        {
            try
            {
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0 || new FileInfo(file).Length > 16384) continue;
                var descriptor = JsonSerializer.Deserialize<Descriptor>(File.ReadAllText(file), DevToolsJson.Options);
                if (descriptor?.ProcessId == processId && descriptor.ProcessStartToken == LocalDevTools.ProcessStartToken(processId)) return file;
            }
            catch (Exception error) when (error is IOException or JsonException or ArgumentException or UnauthorizedAccessException) { }
        }
        return null;
    }
    public Task<DebuggerStatus> StateAsync(int processId, CancellationToken token) => File.Exists(descriptorPath)
        ? CallAsync<DebuggerStatus>("state", new { processId }, token)
        : Task.FromResult(new DebuggerStatus(processId, "VS Code / C# (coreclr)", "exited", null, false, "The IDE debugger session has ended."));
    public Task<TreeSnapshot> TreeAsync(int processId, string sessionId, string stopId, CancellationToken token) => CallAsync<TreeSnapshot>("tree", new { processId, sessionId, stopId }, token);
    public Task<DebuggerFields> FieldsAsync(DebuggerReadRequest request, CancellationToken token) => CallAsync<DebuggerFields>("fields", request, token);
    private async Task<T> CallAsync<T>(string method, object parameters, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        await gate.WaitAsync(timeout.Token).ConfigureAwait(false);
        try
        {
            var directory = LocalDevTools.DiscoveryDirectory;
            var file = Path.GetFullPath(descriptorPath);
            if (Path.GetDirectoryName(file) != directory || Path.GetExtension(file) != ".ide") throw new InvalidDataException("Invalid IDE discovery path.");
            LocalDevTools.PrepareDirectory(directory);
            if (!File.Exists(file)) throw new IOException("The VS Code debugger session has ended.");
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0 || new FileInfo(file).Length > 16384 ||
                (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(file) & ~(UnixFileMode.UserRead | UnixFileMode.UserWrite)) != 0))
                throw new UnauthorizedAccessException("The IDE descriptor must be private to the current user.");
            var descriptor = JsonSerializer.Deserialize<Descriptor>(await File.ReadAllTextAsync(file, timeout.Token), DevToolsJson.Options) ?? throw new InvalidDataException("Missing IDE descriptor.");
            var id = Path.GetFileNameWithoutExtension(file);
            var expectedPipe = OperatingSystem.IsWindows() ? "nazor-ide-" + id : Path.Combine(directory, id + ".ide.sock");
            if (descriptor.Version != 1 || !Guid.TryParseExact(id, "N", out _) || descriptor.PipeName != expectedPipe || descriptor.Token.Length != 64 ||
                !descriptor.Token.All(Uri.IsHexDigit) || descriptor.ProcessId <= 0 || descriptor.HostProcessId <= 0 ||
                LocalDevTools.ProcessStartToken(descriptor.ProcessId) != descriptor.ProcessStartToken) throw new InvalidDataException("The IDE target or bridge version is stale.");
            using var stream = new NamedPipeClientStream(".", descriptor.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await stream.ConnectAsync(timeout.Token).ConfigureAwait(false);
            await IdeWire.WriteAsync(stream, JsonSerializer.Serialize(new { version = 1, token = descriptor.Token, method, @params = parameters }, DevToolsJson.Options), timeout.Token).ConfigureAwait(false);
            var json = JsonNode.Parse(await IdeWire.ReadAsync(stream, timeout.Token).ConfigureAwait(false) ?? throw new EndOfStreamException())!;
            if (json["error"] is { } error) throw new IOException(error.GetValue<string>());
            return DevToolsJson.Read<T>(json["result"]);
        }
        finally { gate.Release(); }
    }
}
