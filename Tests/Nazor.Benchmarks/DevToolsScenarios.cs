using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json.Nodes;
using Nazor.DevTools;
using Nazor.DevTools.Fixtures;
using Nazor.DevTools.Protocol;
using Nazor.Diagnostics;
using Nazor.Graphics;

namespace Nazor.Benchmarks;

internal static class DevToolsScenarios
{
    internal static IEnumerable<(string Name, List<FrameSample> Samples)> All(int samples)
    {
        foreach (var mode in new[] { "baseline", "detached", "idle", "active", "slow" })
        {
            using var bench = new BenchHost(new Size(800, 600));
            var page = (CardView)bench.Host.Render<CardView>().Component;
            bench.Host.Render<LargeTree>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Count"] = 700 }));
            bench.Settle();
            using var session = mode == "baseline" ? null : new DevToolsSession(bench.Host.Renderer, bench.Scene, bench.Input);
            using var agent = session is null ? null : new DevToolsAgent(session);
            void Pump(Task task)
            {
                var watch = Stopwatch.StartNew();
                while (!task.IsCompleted && watch.ElapsedMilliseconds < 15000) { bench.Host.RunPending(); Thread.Sleep(1); }
                task.WaitAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
            }
            if (agent is not null)
            {
                var ready = Task.Run(async () => { while (DebuggerRoots.Windows.Single(w => w.SessionId == session!.SessionId).TreeJson.Length == 0) await Task.Delay(1); });
                Pump(ready);
            }
            DevToolsPeer? peer = null;
            NamedPipeClientStream? slow = null;
            var target = agent is null ? null : new TargetState(agent.Descriptor.Identity, 1, "running", null);
            try
            {
                if (mode is "idle" or "active")
                {
                    var connect = LocalDevTools.ConnectAsync(agent!.Descriptor, "Benchmark", [DevToolsCapabilities.Tree]);
                    Pump(connect); peer = connect.Result; target = peer.RemoteTarget;
                    Pump(peer.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, DevToolsJson.Write(new ContextRequest(new(target!.Identity.SessionId, target.Generation, null)))));
                }
                if (mode == "slow")
                {
                    slow = new(".", agent!.Descriptor.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    Pump(slow.ConnectAsync());
                    Pump(IdeWire.WriteAsync(slow, new JsonObject { ["jsonrpc"] = "2.0", ["id"] = "init", ["method"] = "initialize",
                        ["params"] = DevToolsJson.Write(new InitializeRequest(new(DevToolsProtocol.Major, DevToolsProtocol.Minor), "Slow benchmark", [DevToolsCapabilities.Tree])) }.ToJsonString(), default));
                    var initialized = IdeWire.ReadAsync(slow, default); Pump(initialized);
                    target = DevToolsJson.Read<InitializeResult>(JsonNode.Parse(initialized.Result! )!["result"]).Target;
                    Pump(IdeWire.WriteAsync(slow, new JsonObject { ["jsonrpc"] = "2.0", ["id"] = "tree", ["method"] = DevToolsProtocol.GetTree,
                        ["params"] = new JsonObject { ["context"] = DevToolsJson.Write(new RequestContext(target!.Identity.SessionId, target.Generation, null)), ["deadlineUtc"] = DateTimeOffset.UtcNow.AddSeconds(10).ToString("O") } }.ToJsonString(), default));
                    for (var i = 0; i < 10; i++) { bench.Host.RunPending(); Thread.Sleep(2); }
                }
                bench.Settle();
                var idle = new List<FrameSample>();
                for (var i = 0; i < samples; i++) { Thread.Sleep(16); idle.Add(bench.Frame()); }
                yield return ("devtools." + mode + ".idle", idle);
                var active = new List<FrameSample>();
                for (var i = 0; i < samples + 5; i++)
                {
                    Thread.Sleep(16);
                    page.SetValue(i.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    var frame = bench.Frame();
                    if (mode == "active")
                    {
                        var watch = Stopwatch.StartNew();
                        Pump(peer!.RequestAsync<TreeSnapshot>(DevToolsProtocol.GetTree, DevToolsJson.Write(new ContextRequest(new(target!.Identity.SessionId, target.Generation, null)))));
                        frame = frame with { ToolRoundTripMs = watch.Elapsed.TotalMilliseconds };
                    }
                    if (i >= 5) active.Add(frame);
                }
                yield return ("devtools." + mode + ".change", active);
            }
            finally { peer?.Dispose(); slow?.Dispose(); }
        }
    }
}
