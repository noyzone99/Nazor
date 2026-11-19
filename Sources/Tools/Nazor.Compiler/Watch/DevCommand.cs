using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nazor.Development;

namespace Nazor.Compiler.Watch;

internal static class DevCommand
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(2);
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static async Task<int> RunAsync(IReadOnlyList<string> arguments)
    {
        if (arguments.Count < 2) return Usage();
        var target = arguments[0];
        string method;
        JsonObject? parameters = null;
        switch (arguments[1])
        {
            case "info": method = DevChannelProtocol.AppInfo; break;
            case "render": method = DevChannelProtocol.AppRender; break;
            case "dump":
                method = DevChannelProtocol.AppDump;
                parameters = new JsonObject { ["styles"] = arguments.Skip(2).Contains("--styles") };
                break;
            default: return Usage();
        }
        if (arguments.Count > 2 && (arguments[1] != "dump" || arguments.Skip(2).Any(a => a != "--styles"))) return Usage();

        var descriptors = Find(target);
        if (descriptors is null)
        {
            Console.Error.WriteLine($"nazorc dev: '{target}' is neither a process id nor a project directory");
            return 2;
        }
        if (descriptors.Count == 0)
        {
            Console.Error.WriteLine($"nazorc dev: no running application for '{target}' (start it with NAZOR_DEVCHANNEL=1)");
            return 1;
        }
        var failed = 0;
        foreach (var descriptor in descriptors)
        {
            var opened = await DevChannelClient.OpenAsync(descriptor, ConnectTimeout, notifications: null, CancellationToken.None);
            if (opened is not { Stream: { } stream, Peer: { } peer })
            {
                Console.Error.WriteLine(opened.Connected
                    ? $"nazorc dev: application {descriptor.Pid} does not answer (stopped in the debugger?)"
                    : $"nazorc dev: application {descriptor.Pid} does not accept connections");
                failed++;
                continue;
            }
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var result = await peer.RequestAsync(method, parameters, timeout.Token);
                Console.WriteLine(result is null ? "null" : result.ToJsonString(Indented));
            }
            catch (Exception exception) when (exception is JsonRpc.JsonRpcException or OperationCanceledException or IOException)
            {
                Console.Error.WriteLine($"nazorc dev: application {descriptor.Pid}: {exception.Message}");
                failed++;
            }
            finally
            {
                peer.Dispose();
                stream.Dispose();
            }
        }
        return failed == 0 ? 0 : 1;
    }

    private static IReadOnlyList<DevChannelDescriptor>? Find(string target)
    {
        var descriptors = DevChannelClient.ReadDescriptors(DevChannelProtocol.DiscoveryDirectory).Select(d => d.Descriptor).Where(d => d.IsAlive);
        if (int.TryParse(target, NumberStyles.None, CultureInfo.InvariantCulture, out var pid)) return descriptors.Where(d => d.Pid == pid).ToArray();
        if (!Directory.Exists(target)) return null;
        var directory = DevChannelProtocol.NormalizeDirectory(target);
        return descriptors.Where(d => d.ServesProject(directory)).ToArray();
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: nazorc dev <pid | project directory> info | dump [--styles] | render");
        return 2;
    }
}
