using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json.Nodes;
using Nazor.Compiler.Watch;
using Nazor.Development;
using Nazor.JsonRpc;

namespace Nazor.Compiler.Tests;

public sealed class DevChannelClientTests : IDisposable
{
    private readonly string discovery = Directory.CreateTempSubdirectory("nazor-discovery-").FullName;
    private readonly string project = Directory.CreateTempSubdirectory("nazor-project-").FullName;
    private readonly StringWriter output = new();

    public void Dispose()
    {
        Directory.Delete(discovery, recursive: true);
        Directory.Delete(project, recursive: true);
    }

    private string Output
    {
        get
        {
            lock (output) return output.ToString();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public async Task Ide_refresh_delivers_styles_and_returns_only_json_with_actionable_session_warnings(bool? hotReloadSupported)
    {
        using var application = hotReloadSupported.HasValue ? new FakeApplication(hotReloadSupported.Value) : null;
        application?.Descriptor(project).Save(discovery);
        File.WriteAllText(Path.Combine(project, "_Imports.nazor"), "@using Nazor.Controls\n");
        File.WriteAllText(Path.Combine(project, "App.nazor"), "<Container />\n@style { Container { padding: 8px; } }\n");
        File.WriteAllText(Path.Combine(project, "Theme.nss"), "Container { padding: 10px; }\n");
        var generated = Path.Combine(project, "obj", "Nazor");
        var session = new WatchSession(new() { RootNamespace = "TestApp", References = [Build.Core, Build.Controls, Build.Style] },
            generated, project, null);
        var replyText = new StringWriter();

        Assert.Equal(0, await IdeRefreshCommand.RunAsync(session, replyText, discovery));
        var reply = JsonNode.Parse(replyText.ToString())!;
        Assert.Equal(2, reply["generated"]!.AsArray().Count);
        var diagnostics = reply["diagnostics"]!.AsArray();
        Assert.True(reply["timings"]!["generationMs"]!.GetValue<double>() >= 0);
        Assert.True(reply["timings"]!["totalMs"]!.GetValue<double>() >= reply["timings"]!["generationMs"]!.GetValue<double>());
        if (hotReloadSupported == true) Assert.Empty(diagnostics);
        else Assert.Equal(hotReloadSupported is null ? "NZVS003" : "NZVS002", Assert.Single(diagnostics)!["Id"]!.GetValue<string>());
        if (hotReloadSupported is null)
        {
            var message = diagnostics[0]!["Message"]!.GetValue<string>();
            Assert.Contains("No live development-channel descriptor", message);
            Assert.DoesNotContain("Enable NAZOR_DEVCHANNEL", message);
            Assert.Contains("Hot Reload result is reported separately", message);
        }
        if (application is not null)
        {
            var updates = application.Requests.Where(r => r.Method == DevChannelProtocol.StylesUpdate).ToArray();
            Assert.Equal(2, updates.Length);
            Assert.Contains(updates, r => r.Parameters!["source"]!.GetValue<string>() == "App.nazor");
            Assert.Contains(updates, r => r.Parameters!["source"]!.GetValue<string>() == "Theme.nss");
        }
    }

    [Fact]
    public async Task Discovery_distinguishes_a_different_project_from_a_matching_unreachable_pipe()
    {
        using var application = new FakeApplication(hotReloadSupported: true);
        application.Descriptor(Path.Combine(project, "Other")).Save(discovery);
        using var client = new DevChannelClient(project, discovery, output);
        await client.ScanAsync();
        Assert.Contains("another project", client.DiscoveryIssue);
        var descriptor = application.Descriptor(project) with { Pipe = "nazor-no-pipe-" + Guid.NewGuid().ToString("N") };
        descriptor.Save(discovery);
        await client.ScanAsync();
        Assert.Contains("pipe refused or timed out", client.DiscoveryIssue);
        Assert.Equal(0, client.ConnectionCount);
        application.Descriptor(project).Save(discovery);
        await client.ScanAsync();
        Assert.Equal(1, client.ConnectionCount);
        Assert.Null(client.DiscoveryIssue);
    }

    [Fact]
    public async Task Initialize_and_style_updates_reach_the_application_and_its_log_is_printed()
    {
        using var application = new FakeApplication(hotReloadSupported: true);
        application.Descriptor(project).Save(discovery);
        using var client = new DevChannelClient(project, discovery, output);

        await client.ScanAsync();
        Assert.Equal(1, client.ConnectionCount);
        var info = Assert.Single(client.Applications);
        Assert.Equal((Environment.ProcessId, "Fake window", true), (info.Pid, info.Title, info.HotReloadSupported));
        Assert.True(client.AnyHotReloadSupported);
        var (method, parameters) = Assert.Single(application.Requests);
        Assert.Equal(DevChannelProtocol.Initialize, method);
        Assert.Equal("nazorc", parameters!["client"]!.GetValue<string>());
        Assert.Equal(DevChannelProtocol.ProtocolVersion, parameters["protocolVersion"]!.GetValue<int>());
        Assert.Contains($"nazorc watch: connected to application {Environment.ProcessId} (Fake window)", Output);
        Assert.DoesNotContain("NZW002", Output);
        Assert.DoesNotContain("NZW003", Output);

        var sheet = Nss.NssCompiler.Compile("Components/Badge.nazor", ".badge { padding: 6px; }").StyleSheet;
        var json = Style.StyleSheetJson.Serialize(sheet);
        var deliveries = await client.SendStylesAsync([
            new StyleUpdate("Components/Badge.nazor", "ProjectSample.Components.Badge", json),
            new StyleUpdate("Styles/Theme.nss", null, json),
            new StyleUpdate("Components/Same.nazor", "ProjectSample.Components.Same", json)]);

        Assert.Equal(3, deliveries.Count);
        Assert.Equal(("Components/Badge.nazor", 1, 0), (deliveries[0].Source, deliveries[0].Applied, deliveries[0].Unchanged));
        Assert.Empty(deliveries[0].Errors);
        Assert.Equal(("Styles/Theme.nss", 0), (deliveries[1].Source, deliveries[1].Applied));
        Assert.Equal([Environment.ProcessId], deliveries[1].NotInUse);
        Assert.Equal(("Components/Same.nazor", 0, 1), (deliveries[2].Source, deliveries[2].Applied, deliveries[2].Unchanged));
        Assert.Empty(deliveries[2].NotInUse);
        Assert.Equal(4, application.Requests.Count);
        var (_, scoped) = application.Requests[1];
        Assert.Equal("Components/Badge.nazor", scoped!["source"]!.GetValue<string>());
        Assert.Equal("ProjectSample.Components.Badge", scoped["scope"]!.GetValue<string>());
        Assert.Equal(json, scoped["sheet"]!.ToJsonString());
        Assert.Null(application.Requests[2].Parameters!["scope"]);
        var lines = Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains($"nazorc watch [{Environment.ProcessId}]: styles of Components/Badge.nazor applied", lines);
    }

    [Fact]
    public async Task An_application_without_hot_reload_gets_one_warning_and_no_application_another()
    {
        using (var client = new DevChannelClient(project, discovery, output))
        {
            await client.ScanAsync();
            Assert.Equal(0, client.ConnectionCount);
            await client.ScanAsync();
        }
        Assert.Single(Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries), l => l.Contains("warning NZW003"));

        using var application = new FakeApplication(hotReloadSupported: false);
        application.Descriptor(project).Save(discovery);
        using var connected = new DevChannelClient(project, discovery, output);
        await connected.ScanAsync();
        await connected.ScanAsync();
        Assert.Equal(1, connected.ConnectionCount);
        Assert.False(connected.AnyHotReloadSupported);
        Assert.Single(Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries), l => l.Contains("warning NZW002") && l.Contains("without .NET Hot Reload support"));
    }

    [Fact]
    public async Task Descriptors_of_dead_processes_and_of_other_projects_are_handled()
    {
        var dead = Enumerable.Range(1, 100_000).Select(i => 4_000_000 - 4 * i).First(pid => !Exists(pid));
        var deadFile = new DevChannelDescriptor(dead, DateTime.UtcNow, DevChannelProtocol.PipeName(dead), [project], "dead", DevChannelProtocol.NazorVersion, 1).Save(discovery);
        using var other = new FakeApplication(hotReloadSupported: true);
        other.Descriptor(Path.Combine(project, "elsewhere")).Save(discovery);
        Assert.True(File.Exists(deadFile));
        using var client = new DevChannelClient(project, discovery, output);

        await client.ScanAsync();

        Assert.False(File.Exists(deadFile));
        Assert.Equal(0, client.ConnectionCount);
        Assert.Empty(other.Requests);
        Assert.True(File.Exists(DevChannelDescriptor.PathIn(discovery, Environment.ProcessId)));
    }

    private static bool Exists(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private sealed class FakeApplication : IDisposable
    {
        private readonly NamedPipeServerStream server;
        private readonly bool hotReloadSupported;
        private readonly List<(string Method, JsonNode? Parameters)> requests = [];
        private JsonRpcPeer? peer;

        public FakeApplication(bool hotReloadSupported)
        {
            this.hotReloadSupported = hotReloadSupported;
            Pipe = "nazor-test-" + Guid.NewGuid().ToString("N");
            server = new NamedPipeServerStream(Pipe, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            _ = Task.Run(async () =>
            {
                await server.WaitForConnectionAsync();
                peer = new JsonRpcPeer(server, server) { RequestHandler = HandleAsync };
                peer.Start();
            });
        }

        public string Pipe { get; }

        public IReadOnlyList<(string Method, JsonNode? Parameters)> Requests
        {
            get
            {
                lock (requests) return [.. requests];
            }
        }

        public DevChannelDescriptor Descriptor(string projectDirectory)
        {
            using var process = Process.GetCurrentProcess();
            return new(Environment.ProcessId, process.StartTime.ToUniversalTime(), Pipe, [projectDirectory], "tests", DevChannelProtocol.NazorVersion, DevChannelProtocol.ProtocolVersion);
        }

        private async Task<JsonNode?> HandleAsync(string method, JsonNode? parameters, CancellationToken cancellationToken)
        {
            lock (requests) requests.Add((method, parameters?.DeepClone()));
            switch (method)
            {
                case DevChannelProtocol.Initialize:
                    return new JsonObject
                    {
                        ["protocolVersion"] = DevChannelProtocol.ProtocolVersion,
                        ["nazorVersion"] = DevChannelProtocol.NazorVersion,
                        ["pid"] = Environment.ProcessId,
                        ["hotReloadSupported"] = hotReloadSupported,
                        ["trackingEnabled"] = true,
                        ["windows"] = new JsonArray(new JsonObject { ["title"] = "Fake window" }),
                    };
                case DevChannelProtocol.StylesUpdate:
                    var source = parameters!["source"]!.GetValue<string>();
                    if (parameters["scope"] is null) return new JsonObject { ["applied"] = false, ["sheets"] = 0, ["message"] = "not in use yet" };
                    if (source.EndsWith("Same.nazor", StringComparison.Ordinal)) return new JsonObject { ["applied"] = true, ["sheets"] = 1, ["message"] = "unchanged" };
                    await peer!.NotifyAsync(DevChannelProtocol.AppLog, new JsonObject { ["level"] = "info", ["message"] = $"styles of {source} applied" }, cancellationToken);
                    return new JsonObject { ["applied"] = true, ["sheets"] = 1 };
                default:
                    throw new JsonRpcException(JsonRpcException.MethodNotFound, $"method '{method}' is not supported");
            }
        }

        public void Dispose()
        {
            peer?.Dispose();
            server.Dispose();
        }
    }
}
