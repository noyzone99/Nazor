using System.Text;
using System.Text.Json.Nodes;
using Nazor.JsonRpc;

namespace Nazor.DevTools.Protocol.Tests;

public sealed class FramingTests
{
    [Theory]
    [InlineData("Content-Length: -1\r\n\r\n")]
    [InlineData("Content-Length: 0\r\n\r\n")]
    [InlineData("Content-Length: 1048577\r\n\r\n")]
    [InlineData("Content-Length: 9999999999999999999999\r\n\r\n")]
    [InlineData("Content-Length: 2\r\nContent-Length: 2\r\n\r\n{}")]
    [InlineData("Content-Length: 2\n\n{}")]
    [InlineData("Content-Length: 2\rX\r\n\r\n{}")]
    [InlineData("Content-Type: application/json; charset=latin1\r\nContent-Length: 2\r\n\r\n{}")]
    [InlineData("Unknown: header\r\nContent-Length: 2\r\n\r\n{}")]
    [InlineData("Content-Type: application/json\r\nContent-Type: application/json\r\nContent-Length: 2\r\n\r\n{}")]
    [InlineData("\r\n")]
    [InlineData("Content-Length: 2")]
    [InlineData("Content-Length: 2\r\n\r\n[]")]
    public async Task Invalid_headers_and_non_object_payloads_are_rejected(string wire)
    {
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(wire));
        var rpc = new JsonRpcConnection(input, Stream.Null, DevToolsProtocol.MaxMessageBytes, DevToolsProtocol.MaxDepth);
        await Assert.ThrowsAsync<InvalidDataException>(() => rpc.ReadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Partial_streams_unicode_byte_lengths_and_exact_message_limits_are_supported()
    {
        using var output = new MemoryStream();
        var rpc = new JsonRpcConnection(Stream.Null, output);
        await rpc.WriteAsync(new() { ["result"] = "Caffè 漢字 🙂" }, TestContext.Current.CancellationToken);
        using var input = new OneByteStream(output.ToArray());
        Assert.Equal("Caffè 漢字 🙂", (await new JsonRpcConnection(input, Stream.Null).ReadAsync(TestContext.Current.CancellationToken))!["result"]!.GetValue<string>());
        using var exact = new MemoryStream(Encoding.UTF8.GetBytes("Content-Length: 2\r\n\r\n{}"));
        Assert.NotNull(await new JsonRpcConnection(exact, Stream.Null, 2).ReadAsync(TestContext.Current.CancellationToken));
        using var large = new MemoryStream();
        await new JsonRpcConnection(Stream.Null, large, 17).WriteAsync(new() , TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidDataException>(() => new JsonRpcConnection(Stream.Null, Stream.Null, 16).WriteAsync(new() { ["result"] = new string('x', 100) }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Header_depth_utf8_and_truncation_limits_fail_closed()
    {
        using var header = new MemoryStream(Encoding.ASCII.GetBytes(new string('a', 8193)));
        await Assert.ThrowsAsync<InvalidDataException>(() => new JsonRpcConnection(header, Stream.Null).ReadAsync(TestContext.Current.CancellationToken));
        using var truncated = new MemoryStream(Encoding.ASCII.GetBytes("Content-Length: 10\r\n\r\n{}"));
        await Assert.ThrowsAsync<EndOfStreamException>(() => new JsonRpcConnection(truncated, Stream.Null).ReadAsync(TestContext.Current.CancellationToken));
        using var invalidUtf8 = new MemoryStream([.. Encoding.ASCII.GetBytes("Content-Length: 3\r\n\r\n"), 0x22, 0xff, 0x22]);
        await Assert.ThrowsAsync<InvalidDataException>(() => new JsonRpcConnection(invalidUtf8, Stream.Null).ReadAsync(TestContext.Current.CancellationToken));
        var body = "{\"n\":" + new string('[', 32) + "0" + new string(']', 32) + "}";
        using var deep = new MemoryStream(Encoding.UTF8.GetBytes($"Content-Length: {body.Length}\r\n\r\n{body}"));
        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(() => new JsonRpcConnection(deep, Stream.Null, maxDepth: 32).ReadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Full_message_and_header_byte_boundaries_are_inclusive()
    {
        var body = "{\"x\":\"" + new string('x', DevToolsProtocol.MaxMessageBytes - 8) + "\"}";
        using var input = new MemoryStream(Encoding.UTF8.GetBytes($"Content-Length: {body.Length}\r\n\r\n{body}"));
        Assert.NotNull(await new JsonRpcConnection(input, Stream.Null, DevToolsProtocol.MaxMessageBytes).ReadAsync(TestContext.Current.CancellationToken));
        var header = "Content-Length:" + new string(' ', 8192 - "Content-Length:2\r\n\r\n".Length) + "2\r\n\r\n{}";
        using var padded = new MemoryStream(Encoding.ASCII.GetBytes(header));
        Assert.NotNull(await new JsonRpcConnection(padded, Stream.Null).ReadAsync(TestContext.Current.CancellationToken));
        using var compatible = new MemoryStream(Encoding.ASCII.GetBytes("Content-Type: application/vscode-jsonrpc;charset=utf8\r\nContent-Length: 2\r\n\r\n{}"));
        Assert.NotNull(await new JsonRpcConnection(compatible, Stream.Null).ReadAsync(TestContext.Current.CancellationToken));
    }

    private sealed class OneByteStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => base.ReadAsync(buffer[..Math.Min(buffer.Length, 1)], cancellationToken);
    }
}
