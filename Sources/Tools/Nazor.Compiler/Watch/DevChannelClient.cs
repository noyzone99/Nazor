using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Text.Json.Nodes;
using Nazor.Development;
using Nazor.JsonRpc;

namespace Nazor.Compiler.Watch;

internal sealed record StyleDelivery(string Source, int Applied, IReadOnlyList<int> NotInUse, IReadOnlyList<string> Errors, TimeSpan Elapsed)
{
    public int Unchanged { get; init; }
}

internal sealed record ApplicationInfo(int Pid, string Title, bool HotReloadSupported, string NazorVersion);

internal sealed record OpenResult(bool Connected, NamedPipeClientStream? Stream, JsonRpcPeer? Peer, JsonObject? Initialize)
{
    public static readonly OpenResult Refused = new(false, null, null, null);
    public static readonly OpenResult Silent = new(true, null, null, null);
}

internal sealed class DevChannelClient : IDisposable
{
    private const string Prefix = "nazorc watch: ";
    private const int StaleAfterTimeouts = 3;
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ShutdownWait = TimeSpan.FromSeconds(2);

    private readonly string projectDirectory;
    private readonly string discoveryDirectory;
    private readonly TextWriter output;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly List<Application> applications = [];
    private readonly Dictionary<int, int> timeouts = [];
    private readonly CancellationTokenSource stopping = new();
    private Task? scanning;
    private bool warnedVersion;
    private bool warnedNoApplication;
    private bool disposed;

    public string? DiscoveryIssue { get; private set; }

    public DevChannelClient(string projectDirectory, string? discoveryDirectory, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(projectDirectory);
        ArgumentNullException.ThrowIfNull(output);
        this.projectDirectory = DevChannelProtocol.NormalizeDirectory(projectDirectory);
        this.discoveryDirectory = discoveryDirectory ?? DevChannelProtocol.DiscoveryDirectory;
        this.output = output;
    }

    public int ConnectionCount
    {
        get
        {
            lock (applications) return applications.Count;
        }
    }

    public IReadOnlyList<ApplicationInfo> Applications
    {
        get
        {
            lock (applications) return applications.Select(a => a.Info).ToArray();
        }
    }

    public bool AnyHotReloadSupported => Applications.Any(a => a.HotReloadSupported);

    public void StartPeriodicScan(TimeSpan interval)
    {
        if (scanning is not null) return;
        scanning = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(interval);
            try
            {
                while (await timer.WaitForNextTickAsync(stopping.Token)) await ScanAsync();
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    public async Task ScanAsync()
    {
        if (disposed) return;
        await gate.WaitAsync();
        try
        {
            if (disposed) return;
            DropClosed();
            var matchingDescriptor = false;
            DiscoveryIssue = $"No live development-channel descriptor was found in '{discoveryDirectory}' for project '{projectDirectory}'. Verify the running application's NAZOR_DEVCHANNEL=1 and NAZOR_DEV_DIRECTORY, then restart it if needed.";
            foreach (var (path, descriptor) in ReadDescriptors(discoveryDirectory))
            {
                if (!descriptor.IsAlive)
                {
                    Delete(path);
                    DropPid(descriptor.Pid);
                    continue;
                }
                if (!descriptor.ServesProject(projectDirectory))
                {
                    if (!matchingDescriptor)
                        DiscoveryIssue = $"Live development-channel descriptors in '{discoveryDirectory}' belong to another project. Start the application built from '{projectDirectory}' or rebuild its project references.";
                    continue;
                }
                matchingDescriptor = true;
                if (IsConnected(descriptor.Pid)) continue;
                var (application, connected) = await ConnectAsync(descriptor);
                if (application is null)
                {
                    DiscoveryIssue = connected
                        ? $"The development channel of process {descriptor.Pid} accepted the connection but initialization did not complete. Resume the debugger or wait for the running Hot Reload operation, then retry."
                        : $"The development-channel descriptor of process {descriptor.Pid} matches '{projectDirectory}', but its pipe refused or timed out. Check that the application is responsive and runs in this user session, then retry.";
                    if (connected)
                    {
                        timeouts.Remove(descriptor.Pid);
                        continue;
                    }
                    timeouts[descriptor.Pid] = timeouts.GetValueOrDefault(descriptor.Pid) + 1;
                    if (timeouts[descriptor.Pid] < StaleAfterTimeouts) continue;
                    timeouts.Remove(descriptor.Pid);
                    Delete(path);
                    continue;
                }
                timeouts.Remove(descriptor.Pid);
                lock (applications) applications.Add(application);
                Print($"{Prefix}connected to application {application.Info.Pid} ({application.Info.Title})");
                if (!warnedVersion && application.Info.NazorVersion != DevChannelProtocol.NazorVersion)
                {
                    warnedVersion = true;
                    Print($"{Prefix}warning: application {application.Info.Pid} runs Nazor {application.Info.NazorVersion} while nazorc is {DevChannelProtocol.NazorVersion}");
                }
                if (!application.Info.HotReloadSupported)
                    Print($"{Prefix}warning NZW002: application {application.Info.Pid} was started without .NET Hot Reload support: only styles are reloaded; run it with 'dotnet watch run' or from Visual Studio to apply markup and code changes");
            }
            if (ConnectionCount > 0) DiscoveryIssue = null;
        }
        finally
        {
            gate.Release();
        }
        CheckNoApplication();
    }

    public async Task<IReadOnlyList<StyleDelivery>> SendStylesAsync(IReadOnlyList<StyleUpdate> updates)
    {
        ArgumentNullException.ThrowIfNull(updates);
        var deliveries = new List<StyleDelivery>();
        if (disposed || updates.Count == 0) return deliveries;
        await gate.WaitAsync();
        try
        {
            if (disposed) return deliveries;
            DropClosed();
            Application[] targets;
            lock (applications) targets = applications.ToArray();
            foreach (var update in updates)
            {
                var stopwatch = Stopwatch.StartNew();
                var applied = 0;
                var unchanged = 0;
                var notInUse = new List<int>();
                var errors = new List<string>();
                JsonNode? sheet;
                try
                {
                    sheet = JsonNode.Parse(update.SheetJson);
                }
                catch (System.Text.Json.JsonException exception)
                {
                    deliveries.Add(new(update.Source, 0, [], [$"the sheet is not valid JSON: {exception.Message}"], stopwatch.Elapsed));
                    continue;
                }
                foreach (var application in targets)
                {
                    var parameters = new JsonObject { ["source"] = update.Source, ["sheet"] = sheet?.DeepClone() };
                    if (update.Scope is not null) parameters["scope"] = update.Scope;
                    try
                    {
                        var result = await RequestAsync(application, DevChannelProtocol.StylesUpdate, parameters) as JsonObject;
                        if (result?["applied"]?.GetValue<bool>() != true) notInUse.Add(application.Info.Pid);
                        else if (result["message"]?.GetValue<string>() == "unchanged") unchanged++;
                        else applied++;
                    }
                    catch (JsonRpcException exception)
                    {
                        errors.Add($"application {application.Info.Pid} rejected the styles: {exception.Message}");
                    }
                    catch (OperationCanceledException)
                    {
                        errors.Add($"application {application.Info.Pid} did not answer within {RequestTimeout.TotalSeconds:0} s (stopped in the debugger?)");
                    }
                    catch (IOException exception)
                    {
                        errors.Add($"application {application.Info.Pid}: {exception.Message}");
                    }
                }
                deliveries.Add(new(update.Source, applied, notInUse, errors, stopwatch.Elapsed) { Unchanged = unchanged });
            }
            DropClosed();
        }
        finally
        {
            gate.Release();
        }
        return deliveries;
    }

    public static IReadOnlyList<(string Path, DevChannelDescriptor Descriptor)> ReadDescriptors(string directory)
    {
        var descriptors = new List<(string, DevChannelDescriptor)>();
        if (!Directory.Exists(directory)) return descriptors;
        foreach (var path in Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                descriptors.Add((path, DevChannelDescriptor.Load(path)));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
            {
            }
        }
        return descriptors;
    }

    public static async Task<OpenResult> OpenAsync(
        DevChannelDescriptor descriptor, TimeSpan connectTimeout, Action<string, JsonNode?>? notifications, CancellationToken cancellationToken)
    {
        var stream = new NamedPipeClientStream(".", descriptor.Pipe, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        try
        {
            await stream.ConnectAsync((int)connectTimeout.TotalMilliseconds, cancellationToken);
        }
        catch (Exception exception) when (exception is TimeoutException or IOException or UnauthorizedAccessException)
        {
            stream.Dispose();
            return OpenResult.Refused;
        }
        var peer = new JsonRpcPeer(stream, stream) { NotificationHandler = notifications };
        peer.Start();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(RequestTimeout);
            var parameters = new JsonObject
            {
                ["client"] = "nazorc",
                ["version"] = DevChannelProtocol.NazorVersion,
                ["protocolVersion"] = DevChannelProtocol.ProtocolVersion,
            };
            if (await peer.RequestAsync(DevChannelProtocol.Initialize, parameters, timeout.Token) is JsonObject initialize)
                return new OpenResult(true, stream, peer, initialize);
        }
        catch (Exception exception) when (exception is JsonRpcException or OperationCanceledException or IOException)
        {
        }
        peer.Dispose();
        stream.Dispose();
        return OpenResult.Silent;
    }

    private async Task<(Application? Application, bool Connected)> ConnectAsync(DevChannelDescriptor descriptor)
    {
        var pid = descriptor.Pid;
        var opened = await OpenAsync(descriptor, ConnectTimeout, (method, parameters) => OnNotification(pid, method, parameters), stopping.Token);
        if (opened is not { Stream: { } stream, Peer: { } peer, Initialize: { } initialize }) return (null, opened.Connected);
        var windows = initialize["windows"] as JsonArray;
        var title = windows?.Count > 0 ? windows[0]?["title"]?.GetValue<string>() ?? "" : "";
        var info = new ApplicationInfo(
            initialize["pid"]?.GetValue<int>() ?? pid,
            title,
            initialize["hotReloadSupported"]?.GetValue<bool>() ?? false,
            initialize["nazorVersion"]?.GetValue<string>() ?? "");
        return (new Application(stream, peer, info), true);
    }

    private async Task<JsonNode?> RequestAsync(Application application, string method, JsonNode? parameters)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
        timeout.CancelAfter(RequestTimeout);
        return await application.Peer.RequestAsync(method, parameters, timeout.Token);
    }

    private void OnNotification(int pid, string method, JsonNode? parameters)
    {
        if (method != DevChannelProtocol.AppLog || parameters is not JsonObject log) return;
        var level = log["level"]?.GetValue<string>() ?? "info";
        var message = log["message"]?.GetValue<string>() ?? "";
        Print(level == "info"
            ? $"nazorc watch [{pid.ToString(CultureInfo.InvariantCulture)}]: {message}"
            : $"nazorc watch [{pid.ToString(CultureInfo.InvariantCulture)}]: {level}: {message}");
    }

    private void DropClosed()
    {
        Application[] closed;
        lock (applications)
        {
            closed = applications.Where(a => a.Peer.Closed.IsCompleted).ToArray();
            foreach (var application in closed) applications.Remove(application);
        }
        foreach (var application in closed)
        {
            application.Dispose();
            Print($"{Prefix}application {application.Info.Pid} exited");
        }
    }

    private void DropPid(int pid)
    {
        Application? gone;
        lock (applications)
        {
            gone = applications.FirstOrDefault(a => a.Info.Pid == pid);
            if (gone is not null) applications.Remove(gone);
        }
        if (gone is null) return;
        gone.Dispose();
        Print($"{Prefix}application {pid} exited");
    }

    private bool IsConnected(int pid)
    {
        lock (applications) return applications.Any(a => a.Info.Pid == pid);
    }

    private void CheckNoApplication()
    {
        if (ConnectionCount > 0)
        {
            warnedNoApplication = false;
            return;
        }
        if (warnedNoApplication) return;
        warnedNoApplication = true;
        Print($"{Prefix}warning NZW003: no running application for this project: start it with NAZOR_DEVCHANNEL=1 (dotnet watch run reads launchSettings.json)");
    }

    private static void Delete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private void Print(string line)
    {
        lock (output) output.WriteLine(line);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        stopping.Cancel();
        try
        {
            scanning?.Wait(ShutdownWait);
        }
        catch (AggregateException)
        {
        }
        Application[] connected;
        lock (applications)
        {
            connected = applications.ToArray();
            applications.Clear();
        }
        foreach (var application in connected) application.Dispose();
    }

    private sealed class Application(NamedPipeClientStream stream, JsonRpcPeer peer, ApplicationInfo info) : IDisposable
    {
        public JsonRpcPeer Peer { get; } = peer;
        public ApplicationInfo Info { get; } = info;

        public void Dispose()
        {
            Peer.Dispose();
            try
            {
                stream.Dispose();
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException)
            {
            }
        }
    }
}
