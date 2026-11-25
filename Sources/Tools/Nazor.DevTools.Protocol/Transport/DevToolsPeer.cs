using System.Text.Json;
using System.Text.Json.Nodes;
using Nazor.JsonRpc;

namespace Nazor.DevTools.Protocol;

public sealed class DevToolsRequest
{
    private readonly Action<Action> apply;
    internal DevToolsRequest(string method, JsonNode parameters, RequestContext context, Action<Action> apply)
        => (Method, Parameters, Context, this.apply) = (method, parameters, context, apply);
    public string Method { get; }
    public JsonNode Parameters { get; }
    public RequestContext Context { get; }
    public void ApplyMutation(Action action) => apply(action);
}

public sealed class DevToolsPeer : IDisposable
{
    private readonly Stream input;
    private readonly Stream output;
    private readonly JsonRpcConnection connection;
    private readonly string name;
    private readonly string[] capabilities;
    private readonly object gate = new();
    private readonly CancellationTokenSource stopping = new();
    private readonly Dictionary<string, TaskCompletionSource<JsonNode?>> pending = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CancellationTokenSource> active = new(StringComparer.Ordinal);
    private readonly TaskCompletionSource closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SemaphoreSlim writes = new(64, 64);
    private Task? reading;
    private TargetState? target;
    private string[] negotiated = [];
    private bool initialized;
    private int stopped;
    private int notifications;

    public DevToolsPeer(Stream input, Stream output, string name, string[] capabilities, TargetState? target = null)
    {
        DevToolsJson.Write(new InitializeResult(new(DevToolsProtocol.Major, DevToolsProtocol.Minor), name, capabilities, target));
        this.input = input;
        this.output = output;
        this.name = name;
        this.capabilities = capabilities.ToArray();
        this.target = target;
        connection = new(input, output, DevToolsProtocol.MaxMessageBytes, DevToolsProtocol.MaxDepth);
    }

    public Func<DevToolsRequest, CancellationToken, Task<JsonNode?>>? RequestHandler { get; set; }
    public Action<string, JsonNode>? NotificationHandler { get; set; }
    public Task Closed => closed.Task;
    public bool IsInitialized { get { lock (gate) return initialized; } }
    public bool Supports(string capability) { lock (gate) return initialized && negotiated.Contains(capability, StringComparer.Ordinal); }
    public TargetState? RemoteTarget { get; private set; }

    public void Start()
    {
        lock (gate)
        {
            ThrowIfClosed();
            reading ??= Task.Run(ReadAsync);
        }
    }

    public async Task<InitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new InitializeRequest(new(DevToolsProtocol.Major, DevToolsProtocol.Minor), name, capabilities);
            var result = DevToolsJson.Read<InitializeResult>(await SendAsync(DevToolsProtocol.Initialize, DevToolsJson.Write(request), cancellationToken));
            if (result.Version.Major != DevToolsProtocol.Major || result.Version.Minor > DevToolsProtocol.Minor || result.Capabilities.Except(capabilities, StringComparer.Ordinal).Any())
                throw new DevToolsException(DevToolsErrors.VersionMismatch, "Incompatible initialization response.");
            lock (gate) { initialized = true; negotiated = result.Capabilities; RemoteTarget = result.Target; }
            return result;
        }
        catch { Dispose(); throw; }
    }

    public Task<T> RequestAsync<T>(string method, JsonNode parameters, CancellationToken cancellationToken = default) =>
        RequestAsync<T>(method, parameters, DateTimeOffset.UtcNow.AddSeconds(DevToolsProtocol.RequestTimeoutSeconds), cancellationToken);

    public async Task<T> RequestAsync<T>(string method, JsonNode parameters, DateTimeOffset deadlineUtc, CancellationToken cancellationToken = default)
    {
        var remaining = deadlineUtc - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero) throw new DevToolsException(DevToolsErrors.Cancelled, "The request expired.");
        if (remaining > TimeSpan.FromSeconds(DevToolsProtocol.RequestTimeoutSeconds)) throw new ArgumentOutOfRangeException(nameof(deadlineUtc));
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lifetime.CancelAfter(remaining);
        var descriptor = Describe(method);
        if (descriptor.Result != typeof(T)) throw new ArgumentException("The method requires a different result contract.", nameof(method));
        var context = ReadContext(method, parameters);
        lock (gate) RequireReady(descriptor.Capability);
        var payload = parameters.DeepClone();
        payload["deadlineUtc"] = deadlineUtc.ToUniversalTime().ToString("O");
        var result = await SendAsync(method, payload, lifetime.Token);
        ValidateResult(method, result, context);
        return DevToolsJson.Read<T>(result);
    }

    public void SetTargetState(TargetState state)
    {
        DevToolsJson.Write(state);
        CancellationTokenSource[] requests;
        lock (gate)
        {
            ThrowIfClosed();
            if (target is { } previous && previous.Identity.SessionId == state.Identity.SessionId && state.Generation <= previous.Generation)
                throw new ArgumentException("A target transition requires a newer generation.", nameof(state));
            if (target is { } bound && bound.Identity.SessionId == state.Identity.SessionId && bound.Identity != state.Identity)
                throw new ArgumentException("A session cannot change its target identity.", nameof(state));
            target = state;
            requests = active.Values.ToArray();
        }
        foreach (var request in requests) Cancel(request);
    }

    public async Task NotifyAsync(string method, JsonNode parameters, CancellationToken cancellationToken = default)
    {
        var capture = ValidateNotification(method, parameters);
        lock (gate)
        {
            RequireReady(NotificationCapability(method));
            if (capture is not null) ValidateContext(new(capture.SessionId, capture.Generation, capture.StopId), false);
            if (method == DevToolsProtocol.SessionChanged && DevToolsJson.Read<TargetState>(parameters) != target)
                throw new DevToolsException(DevToolsErrors.StaleSession, "Publish the authoritative target state.");
            if (notifications >= DevToolsProtocol.MaxPendingRequests) throw new DevToolsException(DevToolsErrors.Busy, "Too many pending notifications.");
            notifications++;
        }
        try { await WriteAsync(new() { ["method"] = method, ["params"] = parameters.DeepClone() }, cancellationToken); }
        finally { lock (gate) notifications--; }
    }

    private async Task<JsonNode?> SendAsync(string method, JsonNode parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Start();
        var id = Guid.NewGuid().ToString("N");
        var completion = new TaskCompletionSource<JsonNode?>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (gate)
        {
            ThrowIfClosed();
            if (pending.Count >= DevToolsProtocol.MaxPendingRequests) throw new DevToolsException(DevToolsErrors.Busy, "Too many pending requests.");
            pending.Add(id, completion);
        }
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stopping.Token);
        lifetime.CancelAfter(TimeSpan.FromSeconds(DevToolsProtocol.RequestTimeoutSeconds));
        try
        {
            // The caller's cancellation and deadline apply to the reply, never to the frame being written: an interrupted
            // write would leave a partial frame on the shared stream and close every request of the connection.
            await WriteAsync(new() { ["id"] = id, ["method"] = method, ["params"] = parameters }, stopping.Token);
            return await completion.Task.WaitAsync(lifetime.Token);
        }
        catch (OperationCanceledException)
        {
            if (completion.Task.IsCompleted) return await completion.Task;
            if (!stopping.IsCancellationRequested) _ = SendCancellationAsync(id);
            if (!cancellationToken.IsCancellationRequested)
                throw new DevToolsException(stopping.IsCancellationRequested ? DevToolsErrors.ConnectionClosed : DevToolsErrors.Cancelled,
                    stopping.IsCancellationRequested ? "The connection was closed." : "The request deadline elapsed.");
            throw;
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            Dispose();
            throw new DevToolsException(DevToolsErrors.ConnectionClosed, "The connection was closed.");
        }
        finally { lock (gate) pending.Remove(id); }
    }

    private async Task SendCancellationAsync(string id)
    {
        try { await WriteAsync(new() { ["method"] = DevToolsProtocol.CancelRequest, ["params"] = new JsonObject { ["id"] = id } }, stopping.Token); }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or OperationCanceledException or DevToolsException) { Dispose(); }
    }

    private async Task ReadAsync()
    {
        try
        {
            while (await connection.ReadAsync(stopping.Token) is { } message)
            {
                ValidateEnvelope(message);
                if (message["method"] is not null)
                {
                    var method = message["method"]!.GetValue<string>();
                    if (message["id"] is null) { ReceiveNotification(method, message["params"]!); continue; }
                    var id = message["id"]!.GetValue<string>();
                    CancellationTokenSource? lifetime = null;
                    lock (gate)
                    {
                        if (active.ContainsKey(id)) throw new InvalidDataException("Duplicate request ID.");
                        if (active.Count < DevToolsProtocol.MaxConcurrentRequests)
                        {
                            lifetime = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
                            active.Add(id, lifetime);
                        }
                    }
                    if (lifetime is null) await ReplyAsync(id, null, new(DevToolsErrors.Busy, "Too many active requests."));
                    else _ = Task.Run(() => AnswerAsync(id, method, message["params"]!, lifetime));
                }
                else
                {
                    TaskCompletionSource<JsonNode?>? completion;
                    lock (gate) pending.TryGetValue(message["id"]!.GetValue<string>(), out completion);
                    if (message["error"] is JsonObject error)
                        completion?.TrySetException(new DevToolsException(error["code"]!.GetValue<int>(), error["message"]!.GetValue<string>()));
                    else completion?.TrySetResult(message["result"]?.DeepClone());
                }
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException or OperationCanceledException or ObjectDisposedException or ArgumentException or InvalidOperationException or DevToolsException) { }
        finally { Dispose(); }
    }

    private async Task AnswerAsync(string id, string method, JsonNode parameters, CancellationTokenSource lifetime)
    {
        JsonNode? result = null;
        DevToolsException? error = null;
        Task<JsonNode?>? work = null;
        var token = lifetime.Token;
        try
        {
            if (method == DevToolsProtocol.Initialize)
            {
                var request = DevToolsJson.Read<InitializeRequest>(parameters);
                if (request.Version.Major != DevToolsProtocol.Major) throw new DevToolsException(DevToolsErrors.VersionMismatch, "Incompatible protocol major version.");
                lock (gate)
                {
                    if (initialized) throw new DevToolsException(DevToolsErrors.InvalidRequest, "Already initialized.");
                    negotiated = capabilities.Intersect(request.Capabilities, StringComparer.Ordinal).ToArray();
                    result = DevToolsJson.Write(new InitializeResult(new(DevToolsProtocol.Major, Math.Min(DevToolsProtocol.Minor, request.Version.Minor)), name, negotiated, target));
                    initialized = true;
                }
            }
            else
            {
                var descriptor = Describe(method);
                var context = ReadContext(method, parameters);
                if (!DevToolsJson.TryUtc(parameters["deadlineUtc"]?.GetValue<string>(), out var deadline) ||
                    deadline > DateTimeOffset.UtcNow.AddSeconds(DevToolsProtocol.RequestTimeoutSeconds + 1))
                    throw new DevToolsException(DevToolsErrors.InvalidParams, "A bounded UTC deadline is required.");
                var remaining = deadline - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero) throw new DevToolsException(DevToolsErrors.Cancelled, "The request expired.");
                lifetime.CancelAfter(remaining);
                lock (gate) { RequireReady(descriptor.Capability); ValidateContext(context, descriptor.Mutation); }
                var completed = false;
                var request = new DevToolsRequest(method, parameters, context, action =>
                {
                    lock (gate)
                    {
                        if (!descriptor.Mutation) throw new InvalidOperationException("This is a read request.");
                        token.ThrowIfCancellationRequested();
                        if (completed || DateTimeOffset.UtcNow >= deadline) throw new DevToolsException(DevToolsErrors.Cancelled, "The mutation lease expired.");
                        ValidateContext(context, true);
                        action();
                    }
                });
                try
                {
                    if (RequestHandler is not { } handler) throw new DevToolsException(DevToolsErrors.Unavailable, "No target handler is available.");
                    work = handler(request, token);
                    result = await work.WaitAsync(token);
                    lock (gate) { token.ThrowIfCancellationRequested(); ValidateContext(context, descriptor.Mutation); ValidateResult(method, result, context); }
                }
                finally { lock (gate) completed = true; }
            }
        }
        catch (DevToolsException exception) { error = exception; }
        catch (OperationCanceledException) { error = new(DevToolsErrors.Cancelled, "The request was cancelled."); }
        catch (Exception) { error = new(DevToolsErrors.InternalError, "The request failed."); }
        await ReplyAsync(id, result, error);
        if (error?.Code == DevToolsErrors.VersionMismatch) Dispose();
        if (work is not null) { try { await work; } catch (Exception) { } }
        lock (gate) active.Remove(id);
        lifetime.Dispose();
    }

    private async Task ReplyAsync(string id, JsonNode? result, DevToolsException? error)
    {
        var reply = new JsonObject { ["id"] = id };
        if (error is null) reply["result"] = result;
        else reply["error"] = new JsonObject { ["code"] = error.Code, ["message"] = error.Message };
        try { await WriteAsync(reply, stopping.Token); }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ObjectDisposedException or OperationCanceledException or JsonException or DevToolsException) { Dispose(); }
    }

    private async Task WriteAsync(JsonObject message, CancellationToken cancellationToken)
    {
        if (!writes.Wait(0)) throw new DevToolsException(DevToolsErrors.Busy, "Too many concurrent writes.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stopping.Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(DevToolsProtocol.RequestTimeoutSeconds));
        try { await connection.WriteAsync(message, deadline.Token); }
        catch (OperationCanceledException) { Dispose(); throw; }
        finally { writes.Release(); }
    }

    private void ReceiveNotification(string method, JsonNode parameters)
    {
        if (method == DevToolsProtocol.CancelRequest)
        {
            var id = parameters["id"]?.GetValue<string>() ?? throw new InvalidDataException("Missing cancellation ID.");
            if (id.Length > 128) throw new InvalidDataException("Invalid cancellation ID.");
            CancellationTokenSource? request;
            lock (gate) active.TryGetValue(id, out request);
            if (request is not null) Cancel(request);
            return;
        }
        lock (gate) RequireReady(NotificationCapability(method));
        var capture = ValidateNotification(method, parameters);
        lock (gate)
        {
            if (capture is not null && (RemoteTarget is null || capture.SessionId != RemoteTarget.Identity.SessionId || capture.Generation != RemoteTarget.Generation || capture.StopId != RemoteTarget.StopId)) return;
        }
        if (method == DevToolsProtocol.SessionChanged)
        {
            var state = DevToolsJson.Read<TargetState>(parameters);
            lock (gate)
            {
                if (RemoteTarget is { } previous && previous.Identity.SessionId == state.Identity.SessionId && state.Generation <= previous.Generation) return;
                if (RemoteTarget is { } bound && bound.Identity.SessionId == state.Identity.SessionId && bound.Identity != state.Identity)
                    throw new InvalidDataException("A session changed its target identity.");
                RemoteTarget = state;
                foreach (var response in pending.Values) response.TrySetException(new DevToolsException(DevToolsErrors.StaleSession, "The target generation changed."));
            }
        }
        NotificationHandler?.Invoke(method, parameters);
    }

    private void ValidateContext(RequestContext context, bool mutation)
    {
        if (target is null || target.Identity.SessionId != context.SessionId || target.Generation != context.Generation || target.StopId != context.StopId)
            throw new DevToolsException(DevToolsErrors.StaleSession, "The session, generation or debugger stop changed.");
        if (target.State == "exited") throw new DevToolsException(DevToolsErrors.Unavailable, "The target exited.");
        if (mutation && target.State == "paused") throw new DevToolsException(DevToolsErrors.TargetPaused, "Target mutations are unavailable while paused.");
    }

    private void RequireReady(string? capability)
    {
        ThrowIfClosed();
        if (!initialized) throw new DevToolsException(DevToolsErrors.NotInitialized, "Initialize the connection first.");
        if (capability is not null && !negotiated.Contains(capability, StringComparer.Ordinal)) throw new DevToolsException(DevToolsErrors.Unsupported, "The capability was not negotiated.");
    }

    private void ThrowIfClosed()
    {
        if (stopped != 0) throw new DevToolsException(DevToolsErrors.ConnectionClosed, "The connection was closed.");
    }

    private static void ValidateEnvelope(JsonObject message)
    {
        DevToolsJson.CheckShape(message);
        if (message["jsonrpc"]?.GetValue<string>() != "2.0") throw new InvalidDataException("Expected JSON-RPC 2.0.");
        if (message["id"] is { } id && (id is not JsonValue value || !value.TryGetValue<string>(out var text) || string.IsNullOrWhiteSpace(text) || text.Length > 128))
            throw new InvalidDataException("Request IDs must be bounded nonempty strings.");
        if (message.ContainsKey("method"))
        {
            if (message["method"] is not JsonValue method || !method.TryGetValue<string>(out var name) || string.IsNullOrWhiteSpace(name) || name.Length > 64 ||
                message["params"] is not JsonObject || message.ContainsKey("result") || message.ContainsKey("error") || message.ContainsKey("id") && message["id"] is null)
                throw new InvalidDataException("Invalid request/notification envelope.");
        }
        else
        {
            if (message["id"] is null || message.ContainsKey("params") || message.ContainsKey("result") == message.ContainsKey("error") || message.ContainsKey("error") && message["error"] is not JsonObject)
                throw new InvalidDataException("Invalid response envelope.");
            if (message["error"] is { } error && (error is not JsonObject || error["code"] is not JsonValue code || !code.TryGetValue<int>(out _) ||
                error["message"] is not JsonValue errorText || !errorText.TryGetValue<string>(out _))) throw new InvalidDataException("Invalid error envelope.");
        }
        if (message.Any(pair => pair.Key is not ("jsonrpc" or "id" or "method" or "params" or "result" or "error"))) throw new InvalidDataException("Unknown envelope field.");
    }

    private static (string Capability, Type Result, bool Mutation) Describe(string method) => method switch
    {
        DevToolsProtocol.GetTree => (DevToolsCapabilities.Tree, typeof(TreeSnapshot), false),
        DevToolsProtocol.BrowseTree => (DevToolsCapabilities.PagedTree, typeof(TreeBrowsePage), false),
        DevToolsProtocol.GetStyles => (DevToolsCapabilities.Styles, typeof(StyleDetails), false),
        DevToolsProtocol.GetGeometry => (DevToolsCapabilities.Geometry, typeof(GeometryDetails), false),
        DevToolsProtocol.GetFrames => (DevToolsCapabilities.Frames, typeof(FrameBatch), false),
        DevToolsProtocol.GetSelection => (DevToolsCapabilities.Selection, typeof(SelectionResult), false),
        DevToolsProtocol.Select => (DevToolsCapabilities.Selection, typeof(SelectionResult), true),
        DevToolsProtocol.Inspect => (DevToolsCapabilities.Inspection, typeof(SelectionResult), true),
        _ => throw new DevToolsException(DevToolsErrors.MethodNotFound, "Unknown DevTools method."),
    };

    private static RequestContext ReadContext(string method, JsonNode parameters) => method switch
    {
        DevToolsProtocol.GetTree or DevToolsProtocol.GetFrames or DevToolsProtocol.GetSelection => DevToolsJson.Read<ContextRequest>(parameters).Context,
        DevToolsProtocol.BrowseTree => DevToolsJson.Read<TreeBrowseRequest>(parameters).Context,
        DevToolsProtocol.GetStyles or DevToolsProtocol.GetGeometry => DevToolsJson.Read<NodeRequest>(parameters).Context,
        DevToolsProtocol.Select => DevToolsJson.Read<SelectionRequest>(parameters).Context,
        DevToolsProtocol.Inspect => DevToolsJson.Read<InspectionRequest>(parameters).Context,
        _ => throw new DevToolsException(DevToolsErrors.MethodNotFound, "Unknown DevTools method."),
    };

    private static void ValidateResult(string method, JsonNode? result, RequestContext context)
    {
        var capture = method switch
        {
            DevToolsProtocol.GetTree => DevToolsJson.Read<TreeSnapshot>(result).Capture,
            DevToolsProtocol.BrowseTree => DevToolsJson.Read<TreeBrowsePage>(result).Capture,
            DevToolsProtocol.GetStyles => DevToolsJson.Read<StyleDetails>(result).Capture,
            DevToolsProtocol.GetGeometry => DevToolsJson.Read<GeometryDetails>(result).Capture,
            DevToolsProtocol.GetFrames => DevToolsJson.Read<FrameBatch>(result).Capture,
            DevToolsProtocol.Select or DevToolsProtocol.Inspect or DevToolsProtocol.GetSelection => DevToolsJson.Read<SelectionResult>(result).Capture,
            _ => throw new DevToolsException(DevToolsErrors.MethodNotFound, "Unknown DevTools method."),
        };
        if (capture.SessionId != context.SessionId || capture.Generation != context.Generation || capture.StopId != context.StopId)
            throw new DevToolsException(DevToolsErrors.StaleSession, "The result belongs to a different capture context.");
    }

    private static string? NotificationCapability(string method) => method switch
    {
        DevToolsProtocol.TreeChanged => DevToolsCapabilities.Tree,
        DevToolsProtocol.TreeInvalidated => DevToolsCapabilities.PagedTree,
        DevToolsProtocol.FramesAdded => DevToolsCapabilities.Frames,
        DevToolsProtocol.SelectionChanged => DevToolsCapabilities.Selection,
        DevToolsProtocol.SessionChanged => null,
        _ => throw new DevToolsException(DevToolsErrors.MethodNotFound, "Unknown DevTools event."),
    };

    private static CaptureStamp? ValidateNotification(string method, JsonNode parameters)
    {
        switch (method)
        {
            case DevToolsProtocol.TreeChanged: return DevToolsJson.Read<TreeDelta>(parameters).Capture;
            case DevToolsProtocol.TreeInvalidated: return DevToolsJson.Read<TreeInvalidation>(parameters).Capture;
            case DevToolsProtocol.FramesAdded: return DevToolsJson.Read<FrameBatch>(parameters).Capture;
            case DevToolsProtocol.SelectionChanged: return DevToolsJson.Read<SelectionResult>(parameters).Capture;
            case DevToolsProtocol.SessionChanged: DevToolsJson.Read<TargetState>(parameters); return null;
            default: throw new DevToolsException(DevToolsErrors.MethodNotFound, "Unknown DevTools event.");
        }
    }

    private static void Cancel(CancellationTokenSource source)
    {
        try { source.Cancel(); }
        catch (Exception exception) when (exception is ObjectDisposedException or AggregateException) { }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref stopped, 1) != 0) return;
        Cancel(stopping);
        CancellationTokenSource[] requests;
        lock (gate)
        {
            foreach (var response in pending.Values) response.TrySetException(new DevToolsException(DevToolsErrors.ConnectionClosed, "The connection was closed."));
            pending.Clear();
            requests = active.Values.ToArray();
        }
        foreach (var request in requests) Cancel(request);
        try { input.Dispose(); } catch (IOException) { }
        if (!ReferenceEquals(input, output)) { try { output.Dispose(); } catch (IOException) { } }
        closed.TrySetResult();
    }
}
