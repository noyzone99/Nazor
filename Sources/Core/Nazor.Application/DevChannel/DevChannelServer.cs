using System.Globalization;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Nazor.JsonRpc;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Development;

/// <summary>
/// The development channel of a running application: a named pipe server, one JSON-RPC peer per connected
/// tool, that applies recompiled stylesheets in place (<c>styles/update</c>), re-renders (<c>app/render</c>) and
/// describes the windows (<c>app/info</c>, <c>app/dump</c>), and forwards hot reload events as <c>app/log</c>
/// notifications. The process is found through a descriptor file in the discovery directory, written on
/// <see cref="Start" /> and deleted on <see cref="Dispose" />. Everything that touches a window runs on its UI
/// dispatcher.
/// </summary>
public sealed class DevChannelServer : IDisposable
{
    /// <summary>The logger category of the channel.</summary>
    public const string LoggerCategory = "Nazor.DevChannel";

    private static readonly TimeSpan ShutdownWait = TimeSpan.FromSeconds(2);
    private readonly UIDispatcher dispatcher;
    private readonly Func<IReadOnlyList<NazorWindow>> windows;
    private readonly ILogger? logger;
    private readonly string discoveryDirectory;
    private readonly CancellationTokenSource stopping = new();
    private readonly List<Client> clients = [];
    private readonly HashSet<NazorWindow> observed = [];
    private readonly Lock gate = new();
    private readonly EventHandler processExit;
    private NamedPipeServerStream? listening;
    private Task? accepting;
    private DevChannelDescriptor descriptor = DevChannelDescriptor.ForCurrentProcess([]);
    private bool started;
    private bool disposed;

    /// <summary>Initializes a new instance of DevChannelServer.</summary>
    /// <param name="dispatcher">The UI dispatcher of the windows served: only windows on it are visible to the tools.</param>
    /// <param name="windows">The open windows, read on every request (for example
    /// <see cref="NazorWindow.OpenWindows" />).</param>
    /// <param name="logger">Where errors go; without one they go to <see cref="Console.Error" />.</param>
    /// <param name="discoveryDirectory">Where the descriptor is written;
    /// <see cref="DevChannelProtocol.DiscoveryDirectory" /> when null.</param>
    /// <exception cref="ArgumentNullException">dispatcher, windows is null.</exception>
    public DevChannelServer(UIDispatcher dispatcher, Func<IReadOnlyList<NazorWindow>> windows, ILogger? logger, string? discoveryDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(windows);
        this.dispatcher = dispatcher;
        this.windows = windows;
        this.logger = logger;
        this.discoveryDirectory = discoveryDirectory ?? DevChannelProtocol.DiscoveryDirectory;
        processExit = (_, _) => DeleteDiscoveryFile();
        PipeName = DevChannelProtocol.PipeName(Environment.ProcessId);
        DiscoveryFile = DevChannelDescriptor.PathIn(this.discoveryDirectory, Environment.ProcessId);
    }

    /// <summary>The name of the named pipe the tools connect to.</summary>
    public string PipeName { get; }

    /// <summary>The descriptor file: <c>&lt;discovery directory&gt;/&lt;pid&gt;.json</c>.</summary>
    public string DiscoveryFile { get; }

    /// <summary>The tools connected right now.</summary>
    public int ConnectionCount
    {
        get
        {
            lock (gate) return clients.Count;
        }
    }

    /// <summary>
    /// Writes the descriptor and starts accepting connections; throws when the pipe or the descriptor cannot be
    /// created.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (started) return;
        started = true;
        var created = DevChannelDescriptor.ForCurrentProcess(FindProjectDirectories());
        lock (gate) descriptor = created;
        created.Save(discoveryDirectory);
        AppDomain.CurrentDomain.ProcessExit += processExit;
        listening = CreatePipe();
        foreach (var window in Served()) Observe(window);
        accepting = Task.Run(AcceptAsync);
    }

    private NamedPipeServerStream CreatePipe() => new(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
        PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

    private async Task AcceptAsync()
    {
        var token = stopping.Token;
        var creationFailed = false;
        while (!token.IsCancellationRequested)
        {
            NamedPipeServerStream? stream = null;
            try
            {
                stream = listening ?? CreatePipe();
                listening = stream;
                creationFailed = false;
                await stream.WaitForConnectionAsync(token);
            }
            catch (Exception exception) when (exception is OperationCanceledException or IOException or UnauthorizedAccessException or ObjectDisposedException)
            {
                stream?.Dispose();
                listening = null;
                if (token.IsCancellationRequested) return;
                if (stream is null)
                {

                    if (!creationFailed) LogError("The development channel could not create its pipe.", exception);
                    creationFailed = true;
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
                else if (exception is not OperationCanceledException) LogError("The development channel could not accept a connection.", exception);
                continue;
            }
            listening = null;
            var client = new Client(stream, new JsonRpcPeer(stream, stream));
            client.Peer.RequestHandler = (method, parameters, cancellation) => HandleAsync(client, method, parameters, cancellation);
            lock (gate) clients.Add(client);
            client.Peer.Start();
            _ = client.Peer.Closed.ContinueWith(_ => Drop(client), TaskScheduler.Default);
        }
    }

    private void Drop(Client client)
    {
        lock (gate) clients.Remove(client);
        client.Dispose();
    }

    private async Task<JsonNode?> HandleAsync(Client client, string method, JsonNode? parameters, CancellationToken cancellationToken)
    {
        if (parameters is not null and not JsonObject) throw new JsonRpcException(JsonRpcException.InvalidParams, "params must be an object");
        var request = parameters as JsonObject ?? [];
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stopping.Token);
        var token = linked.Token;
        return method switch
        {
            DevChannelProtocol.Initialize => await InitializeAsync(client, request, token),
            DevChannelProtocol.StylesUpdate => await OnUiAsync(() => UpdateStyles(request), token),
            DevChannelProtocol.AppRender => await OnUiAsync(Render, token),
            DevChannelProtocol.AppInfo => await OnUiAsync(Info, token),
            DevChannelProtocol.AppDump => await OnUiAsync(() => Dump(request["styles"]?.GetValue<bool>() ?? false), token),
            _ => throw new JsonRpcException(JsonRpcException.MethodNotFound, $"method '{method}' is not supported"),
        };
    }

    private async Task<JsonNode?> OnUiAsync(Func<JsonNode?> work, CancellationToken cancellationToken)
    {
        var task = dispatcher.InvokeAsync(work);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => cancelled.TrySetResult());
        if (await Task.WhenAny(task, cancelled.Task) != task) throw new JsonRpcException(JsonRpcException.ApplicationError, "application is shutting down");
        return await task;
    }

    private async Task<JsonNode?> InitializeAsync(Client client, JsonObject request, CancellationToken cancellationToken)
    {
        var directories = FindProjectDirectories();
        DevChannelDescriptor current;
        var changed = false;
        lock (gate)
        {
            if (!directories.SequenceEqual(descriptor.ProjectDirectories, StringComparer.OrdinalIgnoreCase))
            {
                descriptor = descriptor with { ProjectDirectories = directories };
                changed = true;
            }
            current = descriptor;
        }
        if (changed)
        {
            try
            {
                current.Save(discoveryDirectory);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                LogError("The development channel could not rewrite its descriptor.", exception);
            }
        }
        return await OnUiAsync(() => Initialize(client, request, current), cancellationToken);
    }

    private JsonNode Initialize(Client client, JsonObject request, DevChannelDescriptor current)
    {
        client.Name = request["client"]?.GetValue<string>() ?? "tool";
        client.Version = request["version"]?.GetValue<string>();
        client.ProtocolVersion = request["protocolVersion"]?.GetValue<int>() ?? 0;
        var served = Served();
        foreach (var window in served) Observe(window);
        logger?.LogInformation("{Client} {Version} connected to the development channel.", client.Name, client.Version ?? "");
        return new JsonObject
        {
            ["protocolVersion"] = DevChannelProtocol.ProtocolVersion,
            ["nazorVersion"] = DevChannelProtocol.NazorVersion,
            ["pid"] = Environment.ProcessId,
            ["startTimeUtc"] = current.StartTimeUtc.ToString("o", CultureInfo.InvariantCulture),
            ["projectDirectories"] = new JsonArray(current.ProjectDirectories.Select(d => (JsonNode)d).ToArray()),
            ["hotReloadSupported"] = HotReloadManager.IsSupported,
            ["trackingEnabled"] = StyleRegistry.TrackingEnabled,
            ["windows"] = new JsonArray(served.Select(w => (JsonNode)new JsonObject { ["title"] = Title(w) }).ToArray()),
        };
    }

    private JsonNode UpdateStyles(JsonObject request)
    {
        var source = request["source"]?.GetValue<string>() ?? throw new JsonRpcException(JsonRpcException.InvalidParams, "source is required");
        var scope = request["scope"]?.GetValue<string>();
        var sheetNode = request["sheet"] ?? throw new JsonRpcException(JsonRpcException.InvalidParams, "sheet is required");
        StyleSheet sheet;
        try
        {
            sheet = StyleSheetJson.FromJson(sheetNode);
        }
        catch (InvalidDataException exception)
        {
            throw new JsonRpcException(JsonRpcException.InvalidParams, $"sheet: {exception.Message}");
        }
        var result = new JsonObject();
        var changed = 0;
        if (scope is not null)
        {
            var type = FindType(scope) ?? throw new JsonRpcException(JsonRpcException.ApplicationError, $"component type '{scope}' is not loaded");

            var scoped = StyleRegistry.GetScoped(type);
            var version = scoped?.Version;
            StyleRegistry.ReplaceScoped(type, sheet);
            if (scoped is null || scoped.Version != version) changed++;
            result["applied"] = true;
            result["sheets"] = 1;
        }
        else
        {
            var live = LiveSheets(source);
            if (live.Count == 0)
            {
                StyleRegistry.SetPending(source, sheet);
                result["applied"] = false;
                result["sheets"] = 0;
                result["message"] = "not in use yet";
                return result;
            }
            foreach (var target in live)
                if (target.Replace(sheet)) changed++;
            result["applied"] = true;
            result["sheets"] = live.Count;
        }

        if (changed == 0)
        {
            result["message"] = "unchanged";
            return result;
        }
        foreach (var window in Served()) window.RequestFrame(WindowFrameCause.HotReload);
        Broadcast("info", $"styles of {source} applied");
        return result;
    }

    private List<StyleSheet> LiveSheets(string source)
    {
        var live = StyleRegistry.Find(source).ToList();
        var key = source.Replace('\\', '/');
        foreach (var window in Served())
        {
            var styles = window.Scene.Styles;
            foreach (var sheet in new[] { styles.Theme, styles.Application, styles.Window })
                if (sheet is not null && !ReferenceEquals(sheet, StyleSheet.Empty) && !live.Contains(sheet) &&
                    string.Equals(sheet.Source.Replace('\\', '/'), key, StringComparison.OrdinalIgnoreCase))
                    live.Add(sheet);
        }
        return live;
    }

    private JsonNode Render()
    {
        var served = Served();
        foreach (var window in served)
        {
            window.Renderer.RenderOnHotReload();
            window.RequestFrame(WindowFrameCause.HotReload);
        }
        return new JsonObject { ["windows"] = served.Count };
    }

    private JsonNode Info() => new JsonObject
    {
        ["pid"] = Environment.ProcessId,
        ["hotReloadSupported"] = HotReloadManager.IsSupported,
        ["windows"] = new JsonArray(Served().Select(w => (JsonNode)new JsonObject
        {
            ["title"] = Title(w),
            ["frameCount"] = w.FrameCount,
            ["hotReloadCount"] = w.HotReloadCount,
            ["exceptions"] = w.Exceptions.Count,
        }).ToArray()),
    };

    private JsonNode Dump(bool styles) => new JsonObject
    {
        ["windows"] = new JsonArray(Served().Select(w => (JsonNode)new JsonObject { ["title"] = Title(w), ["tree"] = Tree(w, styles) }).ToArray()),
    };

    private static string Tree(NazorWindow window, bool styles)
    {
        var scene = window.Scene;

        if (scene.Styles.IsDirty) scene.Update();
        var builder = new StringBuilder();
        foreach (var root in window.Renderer.RootComponents) Print(root.Nodes, 0);
        return builder.ToString();

        void Print(IEnumerable<Node> nodes, int depth)
        {
            foreach (var node in nodes)
            {
                builder.Append(' ', depth * 2).Append(node);
                if (node is ElementNode element)
                {
                    builder.Append("  ").Append(scene.GetBounds(element));
                    if (styles) builder.Append("  { ").Append(scene.GetStyle(element).Describe()).Append(" }");
                }
                builder.Append('\n');
                if (node is ElementNode parent) Print(parent.Children, depth + 1);
            }
        }
    }

    private IReadOnlyList<NazorWindow> Served() => windows().Where(w => ReferenceEquals(w.Dispatcher, dispatcher)).ToArray();

    private void Observe(NazorWindow window)
    {
        lock (gate)
        {
            if (disposed || !observed.Add(window)) return;
            window.HotReloadApplied += OnHotReloadApplied;
        }
    }

    private void OnHotReloadApplied(Type[]? types)
    {
        Broadcast("info", types is null ? "hot reload applied: all types" : $"hot reload applied: {types.Length} type(s)");
        if (StyleHotReload.LastError is { } styleError) Broadcast("warning", $"stylesheet not refreshed: {styleError.Message}");
        if (HotReloadManager.LastError is { } hostError) Broadcast("warning", $"hot reload subscriber failed: {hostError.Message}");
    }

    private void Broadcast(string level, string message)
    {
        Client[] targets;
        lock (gate) targets = clients.ToArray();
        foreach (var client in targets)
            _ = client.Peer.NotifyAsync(DevChannelProtocol.AppLog, new JsonObject { ["level"] = level, ["message"] = message }, stopping.Token)
                .ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
    }

    private static string Title(NazorWindow window)
    {
        try
        {
            return window.PlatformWindow.Title;
        }
        catch (InvalidOperationException)
        {
            return "";
        }
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Nazor.Sdk preserves all component types for development-channel component lookup.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Nazor.Sdk preserves all component types for development-channel component lookup.")]
    private static Type? FindType(string fullName)
    {
        if (Type.GetType(fullName, throwOnError: false) is { } direct) return direct;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic) continue;
            try
            {
                if (assembly.GetType(fullName, throwOnError: false) is { } type) return type;
            }
            catch (Exception exception) when (exception is FileNotFoundException or BadImageFormatException or TypeLoadException or ReflectionTypeLoadException)
            {
            }
        }
        return null;
    }

    private static string[] FindProjectDirectories()
    {
        var directories = new List<string>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        if (Assembly.GetEntryAssembly() is { } entry) assemblies.Insert(0, entry);
        foreach (var assembly in assemblies)
        {
            if (assembly.IsDynamic) continue;
            try
            {
                foreach (var metadata in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
                    if (metadata.Key == DevChannelProtocol.ProjectDirectoryMetadata && metadata.Value is { Length: > 0 } value)
                        directories.Add(DevChannelProtocol.NormalizeDirectory(value));
            }
            catch (Exception exception) when (exception is FileNotFoundException or TypeLoadException or ReflectionTypeLoadException)
            {
            }
        }
        return directories.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private void DeleteDiscoveryFile()
    {
        try
        {
            File.Delete(DiscoveryFile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {

        }
    }

    private void LogError(string message, Exception exception)
    {
        if (logger is not null) logger.LogError(exception, "{Message}", message);
        else Console.Error.WriteLine($"Nazor: {message} {exception.Message}");
    }

    /// <summary>
    /// Stops accepting, disconnects every tool, deletes the descriptor; waits at most two seconds for the loop.
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        stopping.Cancel();
        AppDomain.CurrentDomain.ProcessExit -= processExit;
        if (started) DeleteDiscoveryFile();
        Client[] connected;
        NazorWindow[] watched;
        lock (gate)
        {
            connected = clients.ToArray();
            clients.Clear();
            watched = observed.ToArray();
            observed.Clear();
        }
        foreach (var window in watched) window.HotReloadApplied -= OnHotReloadApplied;
        foreach (var client in connected) client.Dispose();
        try
        {
            listening?.Dispose();
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
        }
        try
        {
            accepting?.Wait(ShutdownWait);
        }
        catch (AggregateException)
        {
        }

    }

    private sealed class Client(NamedPipeServerStream stream, JsonRpcPeer peer) : IDisposable
    {
        private int disposed;

        public JsonRpcPeer Peer { get; } = peer;
        public string Name { get; set; } = "tool";
        public string? Version { get; set; }
        public int ProtocolVersion { get; set; }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
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
