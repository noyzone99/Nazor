using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace Nazor.JsonRpc;

internal sealed class JsonRpcException(int code, string message) : Exception(message)
{
    public const int MethodNotFound = -32601;

    public const int InvalidParams = -32602;

    public const int ApplicationError = -32000;

    public const int ConnectionClosed = -32001;

    public int Code { get; } = code;
}

internal sealed class JsonRpcPeer : IDisposable
{
    private readonly JsonRpcConnection connection;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonObject>> pending = new();
    private readonly CancellationTokenSource stopping = new();
    private readonly TaskCompletionSource closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Lock gate = new();
    private Task? reading;
    private int nextId;

    public JsonRpcPeer(Stream input, Stream output) : this(new JsonRpcConnection(input, output))
    {
    }

    public JsonRpcPeer(JsonRpcConnection connection) => this.connection = connection;

    public Func<string, JsonNode?, CancellationToken, Task<JsonNode?>>? RequestHandler { get; set; }

    public Action<string, JsonNode?>? NotificationHandler { get; set; }

    public Task Closed => closed.Task;

    public void Start()
    {
        if (reading is not null) return;
        lock (gate) reading ??= Task.Run(ReadAsync);
    }

    public async Task<JsonNode?> RequestAsync(string method, JsonNode? parameters, CancellationToken cancellationToken = default)
    {
        Start();
        var id = Interlocked.Increment(ref nextId);
        var response = new TaskCompletionSource<JsonObject>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending[id] = response;
        try
        {
            var message = new JsonObject { ["id"] = id, ["method"] = method };
            if (parameters is not null) message["params"] = parameters;
            await connection.WriteAsync(message, cancellationToken);
            var reply = await response.Task.WaitAsync(cancellationToken);
            if (reply["error"] is JsonObject error)
                throw new JsonRpcException(error["code"]?.GetValue<int>() ?? JsonRpcException.ApplicationError, error["message"]?.GetValue<string>() ?? "unknown error");
            return reply["result"];
        }
        finally
        {
            pending.TryRemove(id, out _);
        }
    }

    public Task NotifyAsync(string method, JsonNode? parameters, CancellationToken cancellationToken = default)
    {
        Start();
        var message = new JsonObject { ["method"] = method };
        if (parameters is not null) message["params"] = parameters;
        return connection.WriteAsync(message, cancellationToken);
    }

    private async Task ReadAsync()
    {
        try
        {
            while (await connection.ReadAsync(stopping.Token) is { } message)
            {
                var method = message["method"]?.GetValue<string>();
                var id = message["id"];
                if (method is null)
                {
                    if (id is JsonValue value && value.TryGetValue<int>(out var number) && pending.TryRemove(number, out var response)) response.TrySetResult(message);
                    continue;
                }
                if (id is null)
                {
                    try
                    {
                        NotificationHandler?.Invoke(method, message["params"]);
                    }
                    catch
                    {
                    }
                    continue;
                }
                _ = AnswerAsync(id.DeepClone(), method, message["params"]);
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or ObjectDisposedException or InvalidDataException or System.Text.Json.JsonException)
        {
        }
        finally
        {
            FailPending();
            closed.TrySetResult();
        }
    }

    private async Task AnswerAsync(JsonNode id, string method, JsonNode? parameters)
    {
        var reply = new JsonObject { ["id"] = id };
        try
        {
            if (RequestHandler is not { } handler) throw new JsonRpcException(JsonRpcException.MethodNotFound, $"method '{method}' is not supported");
            reply["result"] = await handler(method, parameters, stopping.Token);
        }
        catch (JsonRpcException exception)
        {
            reply["error"] = new JsonObject { ["code"] = exception.Code, ["message"] = exception.Message };
        }
        catch (Exception exception)
        {
            reply["error"] = new JsonObject { ["code"] = JsonRpcException.ApplicationError, ["message"] = exception.Message };
        }
        try
        {
            await connection.WriteAsync(reply, stopping.Token);
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or ObjectDisposedException)
        {
        }
    }

    private void FailPending()
    {
        foreach (var (id, response) in pending)
            if (pending.TryRemove(id, out _)) response.TrySetException(new JsonRpcException(JsonRpcException.ConnectionClosed, "the connection was closed"));
    }

    public void Dispose()
    {
        if (stopping.IsCancellationRequested) return;
        stopping.Cancel();
        FailPending();
        if (reading is null) closed.TrySetResult();
    }
}
