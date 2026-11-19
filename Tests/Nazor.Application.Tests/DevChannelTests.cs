using System.IO.Pipes;
using System.Text.Json.Nodes;
using System.Runtime.InteropServices;
using Nazor.Development;
using Nazor.JsonRpc;
using Nazor.Nss;
using Nazor.Samples.Counter;
using Nazor.Style;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class DevChannelTests
{
    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(POINT point);

    [Fact]
    public async Task Styles_update_refreshes_resize_radius_and_shadow_without_recreating_the_window()
    {
        using var channel = await Channel.OpenAsync(CounterOptions with
        {
            Rendering = Nazor.Graphics.RenderingMode.Cpu, IsAlwaysOnTop = true,
            Decorations = WindowDecorationPolicy.Require(WindowDecorations.Nazor(WindowTitleBarContent.None, WindowCaptionButtons.None)),
        });
        await channel.RequestAsync(DevChannelProtocol.Initialize, Channel.InitializeParams());
        const string source = "FrameLive.nss";
        var original = NssCompiler.Compile(source, ":root { -window-frame-resize-inset: 0; -window-frame-resize-outset: 4px; -window-frame-border-radius: 8px; -window-frame-box-shadow: 0 2px 4px #00000088; }");
        Assert.True(original.Success);
        channel.Harness.Read(window =>
        {
            window.StyleSheet = original.StyleSheet;
            Assert.True(SetWindowPos(window.PlatformWindow.Handle, 0, 100, 100, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE));
            return true;
        });
        channel.Harness.WaitUntil(window => window.PlatformWindow.ClientFrameRadius.TopLeft.X == 8, "initial frame stylesheet");
        var before = channel.Harness.Read(window =>
        {
            var handle = window.PlatformWindow.Handle;
            var shadow = GetWindow(handle, 2);
            Assert.True(GetWindowRect(shadow, out var bounds));
            return (handle, window.Root.Component, ShadowWidth: bounds.right - bounds.left);
        });
        var updated = NssCompiler.Compile(source, ":root { -window-frame-resize-inset: 0; -window-frame-resize-outset: 16px; -window-frame-border-radius: 24px; -window-frame-box-shadow: 0 4px 12px #00000088; }");
        Assert.True(updated.Success);
        var result = (JsonObject)(await channel.RequestAsync(DevChannelProtocol.StylesUpdate,
            new JsonObject { ["source"] = source, ["sheet"] = StyleSheetJson.ToJson(updated.StyleSheet) }))!;
        Assert.True(result["applied"]!.GetValue<bool>());
        channel.Harness.WaitUntil(window => window.PlatformWindow.ClientFrameRadius.TopLeft.X == 24, "hot-reloaded frame radius");
        channel.Harness.Read(window =>
        {
            Assert.Equal(before.handle, window.PlatformWindow.Handle);
            Assert.Same(before.Component, window.Root.Component);
            var point = new POINT { X = -(int)(12 * window.Scene.Scale), Y = (int)(50 * window.Scene.Scale) };
            Assert.True(ClientToScreen(before.handle, ref point));
            var input = WindowFromPoint(point);
            Assert.NotEqual(0, input);
            Assert.NotEqual(before.handle, input);
            Assert.True(GetWindowRect(GetWindow(before.handle, 2), out var shadow));
            Assert.True(shadow.right - shadow.left > before.ShadowWidth);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    private static readonly ApplicationRuntimeSettings CounterOptions = new() { Title = "Nazor test — DevChannel", Width = 320, Height = 200, ReducedMotion = true, Activate = false };

    [Fact]
    public async Task Initialize_answers_and_the_descriptor_lives_as_long_as_the_server()
    {
        using var channel = await Channel.OpenAsync();
        var result = (JsonObject)(await channel.RequestAsync(DevChannelProtocol.Initialize, Channel.InitializeParams()))!;

        Assert.Equal(DevChannelProtocol.ProtocolVersion, result["protocolVersion"]!.GetValue<int>());
        Assert.Equal(Environment.ProcessId, result["pid"]!.GetValue<int>());
        Assert.True(result["trackingEnabled"]!.GetValue<bool>());
        Assert.Equal(HotReloadManager.IsSupported, result["hotReloadSupported"]!.GetValue<bool>());
        var window = Assert.Single((JsonArray)result["windows"]!);
        Assert.Equal("Nazor test — DevChannel", window!["title"]!.GetValue<string>());
        Assert.Equal(1, channel.Server.ConnectionCount);

        Assert.True(File.Exists(channel.Server.DiscoveryFile));
        var descriptor = DevChannelDescriptor.Load(channel.Server.DiscoveryFile);
        Assert.Equal(Environment.ProcessId, descriptor.Pid);
        Assert.Equal(DevChannelProtocol.PipeName(Environment.ProcessId), descriptor.Pipe);
        Assert.Equal(DevChannelProtocol.ProtocolVersion, descriptor.ProtocolVersion);
        Assert.True(descriptor.IsAlive);
        Assert.False((descriptor with { StartTimeUtc = descriptor.StartTimeUtc.AddMinutes(-5) }).IsAlive);

        channel.Server.Dispose();
        Assert.False(File.Exists(channel.Server.DiscoveryFile));
    }

    [Fact]
    public async Task Styles_update_recolours_the_button_keeping_the_component_and_its_elements()
    {
        using var channel = await Channel.OpenAsync();
        await channel.RequestAsync(DevChannelProtocol.Initialize, Channel.InitializeParams());
        var original = StyleRegistry.GetScoped(typeof(Counter))!.Rules;
        static Color Background(NazorWindow window)
        {
            window.Input.PointerLeft();
            return window.Scene.GetStyle(window.Root.Single("Button")).BackgroundColor;
        }
        try
        {
            var (component, button, frames) = channel.Harness.Read(w => (w.Root.Component, w.Root.Single("Button"), w.FrameCount));
            Assert.Equal(Color.FromRgb(0x30, 0x30, 0x30), channel.Harness.Read(Background));
            var recompiled = NssCompiler.Compile("Counter.nazor", CounterStyle().Replace("#303030", "#ff0000"));
            Assert.True(recompiled.Success, string.Join("\n", recompiled.Diagnostics));

            var result = (JsonObject)(await channel.RequestAsync(DevChannelProtocol.StylesUpdate, new JsonObject
            {
                ["source"] = "Counter.nazor",
                ["scope"] = typeof(Counter).FullName,
                ["sheet"] = StyleSheetJson.ToJson(recompiled.StyleSheet),
            }))!;

            Assert.True(result["applied"]!.GetValue<bool>());
            Assert.Equal(1, result["sheets"]!.GetValue<int>());
            channel.Harness.WaitUntil(w => w.FrameCount > frames, "a frame after the style update");
            Assert.Equal(Color.FromRgb(0xff, 0, 0), channel.Harness.Read(Background));
            Assert.Same(component, channel.Harness.Read(w => w.Root.Component));
            Assert.Same(button, channel.Harness.Read(w => w.Root.Single("Button")));
            var log = await channel.LogAsync(l => l["message"]!.GetValue<string>() == "styles of Counter.nazor applied");
            Assert.Equal("info", log["level"]!.GetValue<string>());
            Assert.Empty(channel.Harness.Read(w => w.Exceptions));
        }
        finally
        {
            StyleRegistry.ReplaceScoped(typeof(Counter), new StyleSheet("Counter.nazor", original));
        }
    }

    [Fact]
    public async Task Unknown_scope_is_an_error_and_an_unused_source_is_pending()
    {
        using var channel = await Channel.OpenAsync();
        await channel.RequestAsync(DevChannelProtocol.Initialize, Channel.InitializeParams());
        var sheet = NssCompiler.Compile("test-local.nss", "Button { background: #ff0000; }").StyleSheet;
        const string unused = "Nobody/Uses.nss";
        var frames = channel.Harness.Read(w => w.FrameCount);

        var error = await Assert.ThrowsAsync<JsonRpcException>(() => channel.RequestAsync(DevChannelProtocol.StylesUpdate, new JsonObject
        {
            ["source"] = "Counter.nazor",
            ["scope"] = "No.Such.Component",
            ["sheet"] = StyleSheetJson.ToJson(sheet),
        }));
        Assert.Equal(JsonRpcException.ApplicationError, error.Code);
        Assert.Contains("'No.Such.Component' is not loaded", error.Message);
        Assert.Equal(Color.FromRgb(0x30, 0x30, 0x30), channel.Harness.Read(w => w.Scene.GetStyle(w.Root.Single("Button")).BackgroundColor));
        Assert.Equal(frames, channel.Harness.Read(w => w.FrameCount));

        try
        {
            var json = StyleSheetJson.ToJson(sheet);
            json["source"] = unused;
            var result = (JsonObject)(await channel.RequestAsync(DevChannelProtocol.StylesUpdate, new JsonObject { ["source"] = unused, ["sheet"] = json }))!;
            Assert.False(result["applied"]!.GetValue<bool>());
            Assert.Equal(0, result["sheets"]!.GetValue<int>());
            Assert.Equal("not in use yet", result["message"]!.GetValue<string>());
            Assert.Equal(frames, channel.Harness.Read(w => w.FrameCount));

            var later = new StyleSheet(unused, []);
            Assert.Equal(sheet.Describe(), later.Describe());
            Assert.Equal(1, later.Version);
            Assert.Contains(later, StyleRegistry.Find(unused));
        }
        finally
        {
            StyleRegistry.SetPending(unused, []);
        }

        var unknown = await Assert.ThrowsAsync<JsonRpcException>(() => channel.RequestAsync("app/unknown", new JsonObject()));
        Assert.Equal(JsonRpcException.MethodNotFound, unknown.Code);
    }

    public sealed class Generic<T> : ComponentBase;

    [Fact]
    public async Task A_generic_scope_with_the_arity_reaches_the_open_type_and_its_constructed_types()
    {
        using var channel = await Channel.OpenAsync();
        await channel.RequestAsync(DevChannelProtocol.Initialize, Channel.InitializeParams());
        var sheet = NssCompiler.Compile("generic-local.nss", "Button { padding: 7px; }").StyleSheet;
        Assert.Null(StyleRegistry.GetScoped(typeof(Generic<int>)));

        var result = (JsonObject)(await channel.RequestAsync(DevChannelProtocol.StylesUpdate, new JsonObject
        {
            ["source"] = "Components/Generic.nazor",
            ["scope"] = typeof(Generic<>).FullName,
            ["sheet"] = StyleSheetJson.ToJson(sheet),
        }))!;

        Assert.True(result["applied"]!.GetValue<bool>());
        var scoped = StyleRegistry.GetScoped(typeof(Generic<int>));
        Assert.NotNull(scoped);
        Assert.Same(scoped, StyleRegistry.GetScoped(typeof(Generic<string>)));
        Assert.Equal(sheet.Describe(), scoped.Describe());
    }

    [Fact]
    public async Task Dump_describes_the_tree_and_render_produces_a_batch()
    {
        using var channel = await Channel.OpenAsync();
        await channel.RequestAsync(DevChannelProtocol.Initialize, Channel.InitializeParams());
        var batches = 0;
        channel.Harness.Read(w =>
        {
            w.Renderer.BatchCompleted += (_, _) => batches++;
            return true;
        });

        var dump = (JsonObject)(await channel.RequestAsync(DevChannelProtocol.AppDump, new JsonObject { ["styles"] = true }))!;
        var window = Assert.Single((JsonArray)dump["windows"]!);
        var tree = window!["tree"]!.GetValue<string>();
        Assert.Contains("Button", tree);
        Assert.Contains("\"Increment\"", tree);
        Assert.Contains("background-color", tree);

        var info = (JsonObject)(await channel.RequestAsync(DevChannelProtocol.AppInfo, null))!;
        Assert.Equal(Environment.ProcessId, info["pid"]!.GetValue<int>());
        Assert.Equal(0, Assert.Single((JsonArray)info["windows"]!)!["exceptions"]!.GetValue<int>());

        var render = (JsonObject)(await channel.RequestAsync(DevChannelProtocol.AppRender, null))!;
        Assert.Equal(1, render["windows"]!.GetValue<int>());
        Assert.True(channel.Harness.Read(_ => batches) >= 1, "RenderOnHotReload produced no batch");
        Assert.Empty(channel.Harness.Read(w => w.Exceptions));
    }

    [Fact]
    public async Task A_connected_tool_that_does_nothing_costs_no_frames()
    {
        using var channel = await Channel.OpenAsync();
        await channel.RequestAsync(DevChannelProtocol.Initialize, Channel.InitializeParams());
        Thread.Sleep(300);
        var settled = channel.Harness.Read(w => w.FrameCount);
        Thread.Sleep(300);
        Assert.Equal(settled, channel.Harness.Read(w => w.FrameCount));
    }

    [Fact]
    public async Task A_hot_reload_delta_re_renders_the_window_once()
    {
        using var channel = await Channel.OpenAsync();
        await channel.RequestAsync(DevChannelProtocol.Initialize, Channel.InitializeParams());
        var (frames, count) = channel.Harness.Read(w => (w.FrameCount, w.HotReloadCount));
        Assert.Equal(0, count);

        HotReloadManager.ApplyUpdate(null);

        channel.Harness.WaitUntil(w => w.HotReloadCount == 1, "the delta applied to the window");
        channel.Harness.WaitUntil(w => w.FrameCount > frames, "a frame after the delta");
        Assert.Empty(channel.Harness.Read(w => w.Exceptions));
        Assert.Equal("Count: 0", channel.Harness.Read(w => w.Root.Elements("TextBlock").Single().Text()));
        var log = await channel.LogAsync(l => l["message"]!.GetValue<string>() == "hot reload applied: all types");
        Assert.Equal("info", log["level"]!.GetValue<string>());
    }

    [Fact]
    public async Task Run_with_the_channel_enabled_publishes_a_descriptor_until_the_window_closes()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nazor-devchannel-" + Guid.NewGuid().ToString("N"));
        var savedDirectory = Environment.GetEnvironmentVariable(DevChannelProtocol.DirectoryVariable);
        var tracking = StyleRegistry.TrackingEnabled;
        Task<int>? run = null;
        try
        {
            Environment.SetEnvironmentVariable(DevChannelProtocol.DirectoryVariable, directory);
            var options = new ApplicationRuntimeSettings { Title = "Nazor test — Run", Width = 320, Height = 200, ReducedMotion = true, Activate = false, DevChannel = true };
            Assert.True(NazorApplication.IsDevChannelEnabled(options));
            Assert.False(NazorApplication.IsDevChannelEnabled(new ApplicationRuntimeSettings { DevChannel = false }));
            run = Task.Run(() => ApplicationTestStartup.Run<Counter>(options), TestContext.Current.CancellationToken);

            var file = DevChannelDescriptor.PathIn(directory, Environment.ProcessId);
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!File.Exists(file) && DateTime.UtcNow < deadline && !run.IsCompleted) await Task.Delay(20, TestContext.Current.CancellationToken);
            Assert.True(File.Exists(file), run.IsCompleted ? $"the application exited ({run.Status})" : "no descriptor was written");
            Assert.True(StyleRegistry.TrackingEnabled);
            var descriptor = DevChannelDescriptor.Load(file);

            using (var stream = new NamedPipeClientStream(".", descriptor.Pipe, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly))
            {
                await stream.ConnectAsync(5000, TestContext.Current.CancellationToken);
                using var peer = new JsonRpcPeer(stream, stream);
                var result = (JsonObject)(await peer.RequestAsync(DevChannelProtocol.Initialize, Channel.InitializeParams(), TestContext.Current.CancellationToken))!;
                var window = Assert.Single((JsonArray)result["windows"]!);
                Assert.Equal("Nazor test — Run", window!["title"]!.GetValue<string>());
            }

            var open = Assert.Single(NazorWindow.OpenWindows, w => w.PlatformWindow.Title == "Nazor test — Run");
            await open.Dispatcher.InvokeAsync(open.Close).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.Equal(0, await run.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.False(File.Exists(file));
        }
        finally
        {
            Environment.SetEnvironmentVariable(DevChannelProtocol.DirectoryVariable, savedDirectory);
            StyleRegistry.TrackingEnabled = tracking;
            if (run is { IsCompleted: false })
                foreach (var window in NazorWindow.OpenWindows)
                    window.Dispatcher.Post(window.Close);
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    private static string CounterStyle()
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Counter.nazor"));
        var open = text.IndexOf("@style {", StringComparison.Ordinal);
        var end = open < 0 ? -1 : text.IndexOf("\n}", open, StringComparison.Ordinal);
        Assert.True(open >= 0 && end > open, "Samples/Nazor.Samples.Counter/Counter.nazor has no @style { ... } block: these tests recompile it");
        var style = text[(open + "@style {".Length)..end];
        Assert.Contains("#303030", style);
        return style;
    }

    private sealed class Channel : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "nazor-devchannel-" + Guid.NewGuid().ToString("N"));
        private readonly bool tracking = StyleRegistry.TrackingEnabled;
        private readonly List<JsonObject> logs = [];
        private NamedPipeClientStream? stream;

        public WindowHarness Harness { get; private set; } = null!;
        public DevChannelServer Server { get; private set; } = null!;
        public JsonRpcPeer Peer { get; private set; } = null!;

        public async Task<JsonObject> LogAsync(Func<JsonObject, bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (true)
            {
                lock (logs)
                    if (logs.FirstOrDefault(condition) is { } log) return log;
                if (DateTime.UtcNow >= deadline) throw new TimeoutException("No matching app/log notification arrived.");
                await Task.Delay(20, TestContext.Current.CancellationToken);
            }
        }

        public static JsonObject InitializeParams() => new() { ["client"] = "tests", ["version"] = "1", ["protocolVersion"] = DevChannelProtocol.ProtocolVersion };

        public Task<JsonNode?> RequestAsync(string method, JsonNode? parameters) => Peer.RequestAsync(method, parameters, TestContext.Current.CancellationToken);

        public static async Task<Channel> OpenAsync(ApplicationRuntimeSettings? settings = null)
        {
            var channel = new Channel();
            try
            {
                StyleRegistry.TrackingEnabled = true;
                channel.Harness = new WindowHarness(typeof(Counter), settings ?? CounterOptions);
                channel.Harness.WaitUntil(w => w.FrameCount > 0, "first frame");
                var dispatcher = channel.Harness.Read(w => w.Dispatcher);
                channel.Server = new DevChannelServer(dispatcher, () => NazorWindow.OpenWindows, logger: null, channel.directory);
                channel.Server.Start();
                channel.stream = new NamedPipeClientStream(".", channel.Server.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await channel.stream.ConnectAsync(5000, TestContext.Current.CancellationToken);
                channel.Peer = new JsonRpcPeer(channel.stream, channel.stream)
                {
                    NotificationHandler = (method, parameters) =>
                    {
                        if (method == DevChannelProtocol.AppLog && parameters is JsonObject log) lock (channel.logs) channel.logs.Add(log);
                    },
                };
                channel.Peer.Start();
                return channel;
            }
            catch
            {
                channel.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            Peer?.Dispose();
            stream?.Dispose();
            Server?.Dispose();
            Harness?.Dispose();
            StyleRegistry.TrackingEnabled = tracking;
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
