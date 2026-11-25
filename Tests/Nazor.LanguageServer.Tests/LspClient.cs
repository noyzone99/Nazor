using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Text.Json.Nodes;
using Nazor.JsonRpc;

namespace Nazor.LanguageServer.Tests;

internal sealed class LspClient : IAsyncDisposable
{
    private readonly JsonRpcConnection connection;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonObject>> responses = new();
    private readonly List<JsonObject> notifications = [];
    private readonly SemaphoreSlim notified = new(0);
    private readonly CancellationTokenSource stopping = new();
    private readonly Task reading;
    private int nextId;

    public LspClient(Stream fromServer, Stream toServer)
    {
        connection = new JsonRpcConnection(fromServer, toServer);
        reading = Task.Run(ReadAsync);
    }

    public Task<int>? Server { get; private init; }

    public static LspClient InProcess()
    {
        var toServer = new Pipe();
        var toClient = new Pipe();
        var server = new NazorLanguageServer(toServer.Reader.AsStream(), toClient.Writer.AsStream(), TextWriter.Null, TimeSpan.Zero);
        return new LspClient(toClient.Reader.AsStream(), toServer.Writer.AsStream()) { Server = Task.Run(server.RunAsync) };
    }

    public async Task<JsonNode?> RequestAsync(string method, JsonObject parameters)
    {
        var id = Interlocked.Increment(ref nextId);
        var response = new TaskCompletionSource<JsonObject>(TaskCreationOptions.RunContinuationsAsynchronously);
        responses[id] = response;
        await connection.WriteAsync(new JsonObject { ["id"] = id, ["method"] = method, ["params"] = parameters }, stopping.Token);
        var message = await response.Task.WaitAsync(TimeSpan.FromSeconds(30));
        if (message["error"] is { } error) throw new InvalidOperationException(error.ToJsonString());
        return message["result"];
    }

    public Task NotifyAsync(string method, JsonObject? parameters = null) =>
        connection.WriteAsync(parameters is null ? new JsonObject { ["method"] = method } : new JsonObject { ["method"] = method, ["params"] = parameters }, stopping.Token);

    public async Task<JsonNode?> InitializeAsync(JsonObject? capabilities = null, JsonObject? initializationOptions = null)
    {
        capabilities ??= new JsonObject();
        if (!capabilities.ContainsKey("workspace")) capabilities["workspace"] = new JsonObject
        {
            ["workspaceEdit"] = new JsonObject { ["documentChanges"] = true, ["resourceOperations"] = new JsonArray("create", "delete", "rename") },
        };
        var result = await RequestAsync("initialize", new JsonObject { ["processId"] = null, ["rootUri"] = null, ["capabilities"] = capabilities, ["initializationOptions"] = initializationOptions });
        await NotifyAsync("initialized", []);
        return result;
    }

    public Task<JsonNode?> InitializeAsVisualStudioAsync() => InitializeAsync(new JsonObject { ["_vs_supportsVisualStudioExtensions"] = true });

    public Task OpenAsync(string path, string text, int version = 1) => NotifyAsync("textDocument/didOpen", new JsonObject
    {
        ["textDocument"] = new JsonObject { ["uri"] = Uri(path), ["languageId"] = "nazor", ["version"] = version, ["text"] = text },
    });

    public Task ChangeAsync(string path, string text, int version) => NotifyAsync("textDocument/didChange", new JsonObject
    {
        ["textDocument"] = new JsonObject { ["uri"] = Uri(path), ["version"] = version },
        ["contentChanges"] = new JsonArray(new JsonObject { ["text"] = text }),
    });

    public Task CloseAsync(string path) => NotifyAsync("textDocument/didClose", new JsonObject { ["textDocument"] = new JsonObject { ["uri"] = Uri(path) } });

    public Task<JsonNode?> AtAsync(string method, string path, int line, int character) => RequestAsync(method, new JsonObject
    {
        ["textDocument"] = new JsonObject { ["uri"] = Uri(path) },
        ["position"] = new JsonObject { ["line"] = line, ["character"] = character },
    });

    public async Task<JsonArray> DiagnosticsAsync(string path, Func<JsonArray, bool>? condition = null, int? version = null)
    {
        var uri = Uri(path);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            lock (notifications)
            {
                foreach (var notification in notifications)
                {
                    if (notification["method"]?.GetValue<string>() != "textDocument/publishDiagnostics") continue;
                    var parameters = notification["params"]!;
                    if (!string.Equals(parameters["uri"]!.GetValue<string>(), uri, StringComparison.OrdinalIgnoreCase)) continue;
                    if (version is int expectedVersion && parameters["version"]?.GetValue<int>() != expectedVersion) continue;
                    var diagnostics = (JsonArray)parameters["diagnostics"]!;
                    if (condition is null || condition(diagnostics)) return diagnostics;
                }
            }
            await notified.WaitAsync(TimeSpan.FromSeconds(1));
        }
        throw new TimeoutException(version is int requestedVersion
            ? $"No matching diagnostics for {path} at version {requestedVersion}."
            : $"No matching diagnostics for {path}.");
    }

    public void ForgetDiagnostics(string path)
    {
        var uri = Uri(path);
        lock (notifications)
            notifications.RemoveAll(n => n["method"]?.GetValue<string>() == "textDocument/publishDiagnostics" &&
                string.Equals(n["params"]!["uri"]!.GetValue<string>(), uri, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<JsonObject> MessageAsync(string method)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            lock (notifications)
                if (notifications.FirstOrDefault(n => n["method"]?.GetValue<string>() == method) is { } message) return message;
            await notified.WaitAsync(TimeSpan.FromSeconds(1));
        }
        throw new TimeoutException($"No {method} from the server.");
    }

    public static string Uri(string path) => new Uri(path).AbsoluteUri;

    public static async Task DeleteDirectoryAsync(string directory)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 20)
            {
                await Task.Delay(100);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return;
            }
        }
    }

    private async Task ReadAsync()
    {
        try
        {
            while (await connection.ReadAsync(stopping.Token) is { } message)
            {
                if (message["id"] is { } id && message["method"] is null)
                {
                    if (responses.TryRemove(id.GetValue<int>(), out var response)) response.SetResult(message);
                    continue;
                }
                lock (notifications) notifications.Add(message);
                notified.Release();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        await stopping.CancelAsync();
        try
        {
            await reading.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
        }
    }
}
