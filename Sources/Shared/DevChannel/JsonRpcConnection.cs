using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Nazor.JsonRpc;

internal sealed class JsonRpcConnection
{
    private readonly Stream input;
    private readonly Stream output;
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private readonly byte[] one = new byte[1];
    private readonly int maxContentLength;
    private readonly int maxDepth;
    private int headerBytes;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public JsonRpcConnection(Stream input, Stream output, int maxContentLength = 16 * 1024 * 1024, int maxDepth = 64)
    {
        this.input = input is BufferedStream ? input : new BufferedStream(input);
        this.output = output;
        this.maxContentLength = maxContentLength;
        this.maxDepth = maxDepth;
    }

    public async Task<JsonObject?> ReadAsync(CancellationToken cancellationToken)
    {
        int? length = null;
        headerBytes = 0;
        var headers = 0;
        var contentTypeSeen = false;
        while (true)
        {
            var line = await ReadHeaderLineAsync(cancellationToken);
            if (line is null)
            {
                if (headerBytes != 0) throw new InvalidDataException("Truncated JSON-RPC header.");
                return null;
            }
            if (line.Length == 0)
            {
                if (length is null) throw new InvalidDataException("Missing Content-Length.");
                break;
            }
            var colon = line.IndexOf(':');
            if (++headers > 16 || colon <= 0) throw new InvalidDataException("Invalid JSON-RPC header.");
            var name = line[..colon];
            var value = line[(colon + 1)..].Trim();
            if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                if (length is not null || !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var count) || count <= 0 || count > maxContentLength)
                    throw new InvalidDataException("Invalid or oversized Content-Length.");
                length = count;
            }
            else if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                var parts = value.Split(';', StringSplitOptions.TrimEntries);
                if (contentTypeSeen || parts.Length > 2 || !parts[0].Equals("application/vscode-jsonrpc", StringComparison.OrdinalIgnoreCase) && !parts[0].Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
                    parts.Length == 2 && !parts[1].Equals("charset=utf-8", StringComparison.OrdinalIgnoreCase) && !parts[1].Equals("charset=utf8", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Unsupported JSON-RPC content type.");
                contentTypeSeen = true;
            }
            else throw new InvalidDataException("Unsupported JSON-RPC header.");
        }
        var content = new byte[length.Value];
        await input.ReadExactlyAsync(content, cancellationToken);
        try
        {
            return JsonNode.Parse(Utf8.GetString(content), documentOptions: new() { MaxDepth = maxDepth }) as JsonObject
                ?? throw new InvalidDataException("A JSON-RPC message must be an object.");
        }
        catch (DecoderFallbackException exception) { throw new InvalidDataException("Invalid UTF-8 JSON-RPC body.", exception); }
    }

    public async Task WriteAsync(JsonObject message, CancellationToken cancellationToken)
    {
        message["jsonrpc"] = "2.0";
        using var buffer = new BoundedBuffer(maxContentLength);
        await System.Text.Json.JsonSerializer.SerializeAsync(buffer, message, JsonRpcJson.Default.JsonObject, cancellationToken);
        var content = buffer.ToArray();
        var header = Encoding.ASCII.GetBytes($"Content-Length: {content.Length}\r\n\r\n");
        await writeLock.WaitAsync(cancellationToken);
        try
        {
            await output.WriteAsync(header, cancellationToken);
            await output.WriteAsync(content, cancellationToken);
            await output.FlushAsync(cancellationToken);
        }
        finally
        {
            writeLock.Release();
        }
    }

    private async Task<string?> ReadHeaderLineAsync(CancellationToken cancellationToken)
    {
        var line = new StringBuilder();
        while (true)
        {
            if (await input.ReadAsync(one, cancellationToken) == 0) return line.Length == 0 ? null : line.ToString();
            if (++headerBytes > 8192 || one[0] > 127) throw new InvalidDataException("Invalid or oversized JSON-RPC header.");
            var c = (char)one[0];
            if (line.Length > 0 && line[^1] == '\r' && c != '\n') throw new InvalidDataException("Invalid JSON-RPC CRLF.");
            if (c == '\n')
            {
                if (line.Length == 0 || line[^1] != '\r') throw new InvalidDataException("JSON-RPC headers require CRLF.");
                return line.ToString(0, line.Length - 1);
            }
            line.Append(c);
        }
    }

    private sealed class BoundedBuffer(int limit) : MemoryStream
    {
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (Length + buffer.Length > limit) throw new InvalidDataException("The JSON-RPC message is too large.");
            return base.WriteAsync(buffer, cancellationToken);
        }
    }
}

[System.Text.Json.Serialization.JsonSerializable(typeof(JsonObject))]
internal partial class JsonRpcJson : System.Text.Json.Serialization.JsonSerializerContext;
