using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools.Tests;

public sealed class InspectorShellTests
{
    [Fact]
    public void Appearance_is_selectable_from_settings_and_layout_remains_usable_in_both_schemes()
    {
        using var harness = new Harness();
        var store = new InspectionStore();
        store.Connecting(CapturedData.Target().Identity, "My Workspace");
        store.Bind(CapturedData.Target()); store.Apply(CapturedData.LinkedTree()); store.Select("element", "e");
        var chosen = Nazor.Style.ColorScheme.System;
        var root = harness.Host.Render<InspectorShell>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Store"] = store,
            ["SchemeChanged"] = EventCallback.Factory.Create<Nazor.Style.ColorScheme>(store, value => chosen = value)
        }));
        harness.Settle();
        Assert.Contains(root.Elements("Tab"), t => t.Text() == "Elements" && t.Attributes["selected"] is true);
        harness.Click(root.Elements("Button").Single(e => e.Id == "settings"));
        var settings = harness.Scene.GetBounds(root.Elements("Popover").Single());
        foreach (var radio in root.Elements("Segment"))
        {
            var bounds = harness.Scene.GetBounds(radio);
            var label = harness.Scene.GetBounds(radio.Elements().Single(e => e.Name == "SegmentLabel"));
            Assert.True(bounds.X >= settings.X && bounds.Right <= settings.Right, $"{radio.Id} leaves the flyout: {bounds} / {settings}");
            Assert.True(label.X >= bounds.X && label.Right <= bounds.Right && label.Y >= bounds.Y && label.Bottom <= bounds.Bottom, $"{radio.Id} label overflows: {label} / {bounds}");
            Assert.True(Math.Abs(label.X + label.Width / 2 - (bounds.X + bounds.Width / 2)) < 1 && Math.Abs(label.Y + label.Height / 2 - (bounds.Y + bounds.Height / 2)) < 1,
                $"{radio.Id} label is not centered: {label} / {bounds}");
        }
        harness.Snapshot("devtools-appearance-images", "settings-flyout");
        harness.Scene.Styles.Preferences = new(Nazor.Style.ColorScheme.Dark, true);
        harness.Snapshot("devtools-appearance-images", "settings-flyout-dark");
        harness.Scene.Styles.Preferences = new(Nazor.Style.ColorScheme.Light, true);
        harness.Click(root.Elements("Segment").Single(e => e.Id == "scheme-dark"));
        Assert.Equal(Nazor.Style.ColorScheme.Dark, chosen);
        harness.Key("ArrowLeft");
        Assert.Equal(Nazor.Style.ColorScheme.Light, chosen);
        harness.Key("Escape");
        Assert.Empty(root.Elements("Segment"));
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(repository.FullName, "Nazor.slnx"))) repository = repository.Parent!;
        var output = Path.Combine(repository.FullName, "artifacts", "devtools-appearance-images", OperatingSystem.IsWindows() ? "windows" : "linux");
        Directory.CreateDirectory(output);
        foreach (var scheme in new[] { Nazor.Style.ColorScheme.Light, Nazor.Style.ColorScheme.Dark })
        foreach (var (width, height) in new[] { (1180f, 780f), (1020f, 780f), (1020f, 640f) })
        {
            harness.Scene.Viewport = new Size(width, height);
            harness.Scene.Styles.Preferences = new(scheme, true);
            harness.Settle();
            foreach (var id in new[] { "pick-control", "connection-menu", "refresh-capture", "settings", "tree-search", "highlight-selection" })
            {
                var bounds = harness.Scene.GetBounds(root.Elements().Single(e => e.Id == id));
                Assert.True(bounds.Width > 0 && bounds.X >= 0 && bounds.Right <= width, $"{id}: {bounds}");
            }
            var tabs = root.Elements("Tab").Where(t => t.Text() is "Components" or "Elements" or "Performance").Select(harness.Scene.GetBounds).ToArray();
            Assert.True(tabs.Max(t => t.Right) <= harness.Scene.GetBounds(root.Elements().Single(e => e.Id == "connection-menu")).X);
            Assert.True(harness.Scene.GetBounds(root.Elements("TreeView").Single()).Height >= (height == 640 ? 130 : 200));
            var image = harness.Graphics.RenderToImage(harness.Scene.BuildDisplayList(), 1, harness.Scene.Background);
            File.WriteAllBytes(Path.Combine(output, scheme + "-" + width + "-" + height + ".png"), harness.Graphics.EncodePng(image));
        }
        harness.Click(root.Elements("Button").Single(e => e.Id == "connection-menu"));
        foreach (var id in new[] { "switch-connect", "disconnect-target", "clear-capture" })
        {
            var bounds = harness.Scene.GetBounds(root.Elements().Single(e => e.Id == id));
            Assert.True(bounds.Width > 0 && bounds.X >= 0 && bounds.Right <= 1020, $"{id}: {bounds}");
        }
        var flyout = harness.Graphics.RenderToImage(harness.Scene.BuildDisplayList(), 1, harness.Scene.Background);
        File.WriteAllBytes(Path.Combine(output, "connection-flyout.png"), harness.Graphics.EncodePng(flyout));
    }

    [Fact]
    public async Task Expanding_a_live_row_loads_its_children_in_the_inspector()
    {
        using var target = await AgentProcess.StartAsync(wide: true);
        var store = new InspectionStore();
        using var client = new InspectorClient(store, target.Directory) { UsePagedTree = true };
        await client.ConnectAsync(target.Descriptor, TestContext.Current.CancellationToken);
        using var harness = new Harness();
        var root = harness.Host.Render<InspectorShell>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Store"] = store, ["Client"] = client }));
        harness.Settle();
        var page = Assert.Single(store.Read().Tree!.Components, node => node.TypeName.EndsWith(".TreePage", StringComparison.Ordinal));
        Assert.Empty(page.Children);
        var row = root.Elements("TreeViewItem").Single(item => item.Text().Contains("TreePage", StringComparison.Ordinal));
        harness.Click(row.Elements().Single(element => element.Name == "TreeViewExpander"));
        harness.Host.RunUntil(() => store.Read().Tree!.Components.Any(node => node.TypeName.EndsWith(".PassThrough", StringComparison.Ordinal)));
        harness.Settle();
        Assert.Contains(root.Elements("TreeViewItem"), item => item.Text().Contains("PassThrough", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Choosing_a_tree_node_highlights_it_in_the_app_until_the_option_is_cleared()
    {
        using var target = await AgentProcess.StartAsync();
        var store = new InspectionStore();
        using var client = new InspectorClient(store, target.Directory);
        await client.ConnectAsync(target.Descriptor, TestContext.Current.CancellationToken);
        // The test host's dispatcher belongs to this thread: everything below stays synchronous.
        using var harness = new Harness();
        var follow = (bool?)null;
        var root = harness.Host.Render<InspectorShell>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Store"] = store, ["Client"] = client,
            ["FollowSelectionChanged"] = EventCallback.Factory.Create<bool>(store, value => follow = value)
        }));
        harness.Settle();
        var left = store.Read().Tree!.Components.Single(c => c.Key == "left");
        harness.SetText(root.Elements().Single(e => e.Id == "tree-search-input"), "left");
        harness.Click(root.Elements("TreeViewItem").First(e => e.Text().Contains("key=\"left\"", StringComparison.Ordinal)));
        Assert.Equal(left.Id, store.Read().Selection?.NodeId);
        Highlighted(left.Id);
        harness.Click(root.Elements("Switch").Single(e => e.Id == "follow-selection"));
        Assert.False(follow);
        Highlighted("NONE");
        Assert.Equal(left.Id, store.Read().Selection?.NodeId);
        harness.Click(root.Elements("Button").Single(e => e.Id == "output-" + left.OutputElements[0]));
        Assert.Equal(left.OutputElements[0], store.Read().Selection?.NodeId);
        Thread.Sleep(300);
        Highlighted("NONE");
        harness.Click(root.Elements("Switch").Single(e => e.Id == "follow-selection"));
        Assert.True(follow);
        Highlighted(left.OutputElements[0]);

        void Highlighted(string expected)
        {
            var deadline = DateTime.UtcNow.AddSeconds(6);
            while (true)
            {
                harness.Host.RunPending();
                target.SendAsync("highlighted").GetAwaiter().GetResult();
                var actual = target.LineAsync().GetAwaiter().GetResult();
                if (actual == expected) return;
                if (DateTime.UtcNow > deadline) throw new TimeoutException($"Expected highlight {expected}, found {actual}.");
                Thread.Sleep(20);
            }
        }
    }

    [Fact]
    public void An_empty_inspector_offers_discovery_and_connection_guidance()
    {
        using var harness = new Harness();
        var root = harness.Host.Render<InspectorShell>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Store"] = new InspectionStore() }));
        harness.Settle();
        var text = root.Nodes[0].Text();
        Assert.Contains("No captured data", text);
        Assert.Contains("builder.UseDevTools();", text);
        Assert.True(root.Elements("Button").Single(e => e.Id == "connect-target").Attributes["disabled"] is true);
        Assert.True(root.Elements("Button").Single(e => e.Id == "pick-control").Attributes["disabled"] is true);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "Nazor.slnx"))) directory = directory.Parent!;
        var output = Path.Combine(directory.FullName, "artifacts", "devtools-shell-images", OperatingSystem.IsWindows() ? "windows" : "linux");
        Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, "welcome.png"), harness.Graphics.EncodePng(harness.Graphics.RenderToImage(harness.Scene.BuildDisplayList(), 1, harness.Scene.Background)));
    }

    [Fact]
    public void Unavailable_debugger_fields_do_not_render_adapter_error_text_as_a_current_value()
    {
        using var harness = new Harness();
        var store = new InspectionStore();
        store.Bind(CapturedData.Target()); store.Apply(CapturedData.Tree()); store.Select("component", "c");
        store.SetDebugger(new(42, "VS Code", "paused", "stop-1", true, null));
        Assert.True(store.Apply(new DebuggerFields(CapturedData.Stamp(stop: "stop-1"), "c",
            [new("Dangerous", "int", "Implicit function evaluation is turned off by the user", false)], null)));
        var root = harness.Host.Render<InspectorShell>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Store"] = store }));
        harness.Settle();
        Assert.Contains("Dangerous:<unavailable>", root.Nodes[0].Text());
        Assert.DoesNotContain("Implicit function evaluation", root.Nodes[0].Text());
    }

    [Fact]
    public void Cached_components_elements_and_details_remain_keyboard_navigable_after_disconnect()
    {
        using var harness = new Harness();
        var store = new InspectionStore();
        store.Bind(CapturedData.Target());
        store.Apply(CapturedData.Tree());
        var root = harness.Host.Render<InspectorShell>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Store"] = store }));
        harness.Settle();
        var component = root.Elements("TreeViewItem").Single();
        harness.Click(component);
        Assert.Equal("c", store.Read().Selection!.NodeId);
        store.Disconnected("The app exited.");
        harness.Settle();
        var text = root.Nodes[0].Text();
        Assert.Contains("Disconnected", text);
        Assert.Contains("Retained capture", text);
        Assert.Contains("Fixture.Page", text);
        var elementsTab = root.Elements("Tab").Single(e => e.Text() == "Elements");
        harness.Click(elementsTab);
        Assert.Equal("e", store.Read().Selection!.NodeId);
        var element = root.Elements("TreeViewItem").Single();
        harness.Click(element);
        Assert.Equal("element", store.Read().Selection!.Kind);
        Assert.Equal("e", store.Read().Selection!.NodeId);
        harness.Click(root.Elements("Tab").Single(e => e.Text() == "Properties"));
        Assert.Contains("Hidden", root.Nodes[0].Text());
        Assert.Contains("unavailable", root.Nodes[0].Text());
        harness.Click(root.Elements("TreeViewItem").Single());
        harness.Key("ArrowRight");
        Assert.Equal(2, root.Elements("TreeViewItem").Count());
        harness.Key("ArrowDown");
        Assert.Equal("text", store.Read().Selection!.NodeId);
        Assert.Contains("Captured content", root.Nodes[0].Text());
        harness.Click(root.Elements("Button").Single(e => e.Id == "connection-menu"));
        harness.Click(root.Elements("Button").Single(e => e.Id == "clear-capture"));
        Assert.Null(store.Read().Tree);
        Assert.Contains("No captured data", root.Nodes[0].Text());
    }

    [Fact]
    public void Shell_layout_has_scrollable_details_and_visible_tree_at_supported_headless_scales()
    {
        using var harness = new Harness();
        var store = new InspectionStore();
        store.Bind(CapturedData.Target());
        store.Apply(CapturedData.Tree());
        store.Select("element", "e");
        store.Disconnected();
        var root = harness.Host.Render<InspectorShell>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Store"] = store }));
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(repository.FullName, "Nazor.slnx"))) repository = repository.Parent ?? throw new InvalidOperationException();
        var directory = Path.Combine(repository.FullName, "artifacts", "devtools-shell-images", OperatingSystem.IsWindows() ? "windows" : "linux");
        Directory.CreateDirectory(directory);
        foreach (var scale in new[] { 1f, 1.5f, 2f })
        {
            harness.Scene.Scale = scale;
            harness.Settle();
            var tree = root.Elements("TreeView").Single();
            var bounds = harness.Scene.GetBounds(tree);
            Assert.True(bounds.Width >= 200 && bounds.Height >= 200, $"Tree bounds: {bounds}");
            Assert.True(harness.Scene.GetBounds(root.Elements("ResizeHandle").Single()).Height >= 200);
            var image = harness.Graphics.RenderToImage(harness.Scene.BuildDisplayList(), scale, harness.Scene.Background);
            File.WriteAllBytes(Path.Combine(directory, "shell-" + scale.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".png"), harness.Graphics.EncodePng(image));
        }
    }

    [Fact]
    public void Linked_outputs_scope_breadcrumbs_filters_and_no_output_instances_preserve_selection()
    {
        using var harness = new Harness();
        var store = new InspectionStore();
        store.Bind(CapturedData.Target()); store.Apply(CapturedData.LinkedTree());
        var root = harness.Host.Render<InspectorShell>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Store"] = store }));
        harness.Settle();
        harness.Click(root.Elements("Button").Single(e => e.Id == "expand-tree"));
        harness.Click(root.Elements("TreeViewItem").Single(e => e.Text().Contains("#child", StringComparison.Ordinal)));
        Assert.Equal("child", store.Read().Selection!.NodeId);
        Assert.Contains("Captured title", root.Nodes[0].Text());
        Assert.Contains("no single layout box", root.Nodes[0].Text());
        Assert.Contains(root.Elements("Button"), b => b.Id == "owner-framework");
        harness.Snapshot("devtools-shell-images", "components-light");
        harness.Scene.Styles.Preferences = new(Nazor.Style.ColorScheme.Dark, true);
        harness.Snapshot("devtools-shell-images", "components-dark");
        harness.Scene.Styles.Preferences = new(Nazor.Style.ColorScheme.Light, true);
        harness.Click(root.Elements("Button").Single(e => e.Id == "output-e2"));
        Assert.Equal("e2", store.Read().Selection!.NodeId);
        Assert.Contains("Hidden", root.Nodes[0].Text());
        harness.Click(root.Elements("Button").Single(e => e.Id == "show-declaring-component"));
        Assert.Equal("c", store.Read().Selection!.NodeId);
        store.Select("element", "e"); harness.Settle();
        Assert.Contains("Captured attribute", root.Nodes[0].Text());
        Assert.Contains("class=\"card selected\"", root.Nodes[0].Text());
        harness.Click(root.Elements("Tab").Single(t => t.Text() == "Properties"));
        Assert.Contains(".card.selected", root.Nodes[0].Text());
        Assert.Contains(":focus", root.Nodes[0].Text());
        Assert.Contains("onclick", root.Nodes[0].Text());
        harness.Click(root.Elements("Button").Single(e => e.Id == "show-producing-component"));
        Assert.Equal("child", store.Read().Selection!.NodeId);
        store.Select("component", "framework"); harness.Settle();
        harness.Click(root.Elements("Switch").Single(e => e.Id == "framework-detail"));
        Assert.Equal("framework", store.Read().Selection!.NodeId);
        Assert.Contains("retained outside this filter", root.Nodes[0].Text());
        harness.Click(root.Elements("Button").Single(e => e.Id == "reveal-selection"));
        Assert.Contains(root.Elements("TreeViewItem"), e => e.Text().Contains("#framework", StringComparison.Ordinal));
        harness.SetText(root.Elements().Single(e => e.Id == "tree-search-input"), "no match");
        Assert.Empty(root.Elements("TreeViewItem"));
        Assert.Equal("framework", store.Read().Selection!.NodeId);
        harness.Click(root.Elements("Button").Single(e => e.Id == "reveal-selection"));
        store.Select("component", "empty"); harness.Settle();
        Assert.Contains("no physical output", root.Nodes[0].Text());
        store.Disconnected(); harness.Settle();
        Assert.True(root.Elements("Button").Single(e => e.Id == "pick-control").Attributes["disabled"] is true);
    }

    [Fact]
    public void Style_computed_and_frame_panels_keep_provenance_and_remain_readable_while_paused()
    {
        using var harness = new Harness();
        var store = new InspectionStore(); store.Bind(CapturedData.Target());
        var tree = CapturedData.LinkedTree(); tree.Elements[0] = tree.Elements[0] with { IsRendered = true };
        store.Apply(tree); store.Select("element", "e");
        store.Apply(new Nazor.DevTools.Protocol.StyleDetails(CapturedData.Stamp(), "e",
            [new("color", "red", "rule") { Rule = 1, Declaration = 0 }, new("font-size", "12px", "inherited"), new("display", "block", "initial")],
            [new(".card", "theme.nss", null, [new("color", "blue", "overridden") { Longhands = ["color"], Winning = [] }]) { Level = "theme", Specificity = "0,1,0", Order = 0 },
             new(".card", "app.nss", null, [new("color", "red", "winning") { Longhands = ["color"], Winning = ["color"] }]) { Level = "application", Specificity = "0,1,0", Order = 1 }])
            { CustomProperties = [new("--accent", "red", "inherited")] });
        store.Apply(new Nazor.DevTools.Protocol.GeometryDetails(CapturedData.Stamp(), "e", new(14, 4, 80, 24), new(4, 4, 4, 4), new(2, 2, 2, 2), new(4, 0, 0, 0), 1.5)
            { MarginBox = new(10, 0, 88, 32), PaddingBox = new(16, 6, 76, 20), ContentBox = new(16, 10, 76, 16) });
        store.Apply(new Nazor.DevTools.Protocol.FrameBatch(CapturedData.Stamp(), 0, 1,
            Enumerable.Range(1, 3).Select(i => CapturedData.Frame(i) with { NodeCount = 0, PaintCount = 0, StylesRecomputed = 2, HasNodeCount = false, HasPaintCount = false, HasDamage = true,
                Timings = CapturedData.Frame(i).Timings with { OverlayMs = 0.5, ElapsedMs = i * 10 } }).ToArray()), true);
        var root = harness.Host.Render<InspectorShell>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Store"] = store })); harness.Settle();
        harness.Click(root.Elements("Tab").Single(t => t.Text() == "Styles"));
        var rules = root.Elements().Where(e => e.Class?.Split(' ').Contains("rule") == true).ToArray();
        Assert.StartsWith(".card {app.nss", rules[0].Text());
        Assert.DoesNotContain("overridden", rules[0].Text());
        Assert.Contains("color:blue;overridden", rules[1].Text());
        Assert.Contains("specificity 0,1,0", root.Nodes[0].Text());
        CapturePanel("styles");
        harness.Click(root.Elements("Tab").Single(t => t.Text() == "Computed"));
        Assert.Contains("Target scale: 1.5", root.Nodes[0].Text()); Assert.Contains("x 21, y 6, 120 × 36", root.Nodes[0].Text());
        Assert.Contains(root.Elements("ListViewItem"), item => item.Text() == "font-size:12pxinherited");
        Assert.DoesNotContain(root.Elements("ListViewItem"), item => item.Text().StartsWith("display", StringComparison.Ordinal));
        CapturePanel("computed");
        harness.Click(root.Elements("Tab").Single(t => t.Text() == "Performance"));
        Assert.Contains("Total node countUnavailable", root.Nodes[0].Text()); Assert.Contains("Overlay0.5 ms", root.Nodes[0].Text());
        Assert.Contains("Damage rectangles (physical pixels): 1", root.Nodes[0].Text());
        harness.Click(root.Elements("ListViewItem").First(t => t.Text().StartsWith("#1", StringComparison.Ordinal)));
        store.Apply(new Nazor.DevTools.Protocol.FrameBatch(CapturedData.Stamp(), 1, 2, [CapturedData.Frame(4)])); harness.Settle();
        Assert.Contains("Frame 1 · causes", root.Nodes[0].Text());
        store.Bind(CapturedData.Target(2, "paused", "stop-2")); harness.Settle();
        Assert.Contains("previous execution context", root.Nodes[0].Text());
        Assert.True(root.Elements("Button").Single(b => b.Id == "refresh-details").Attributes["disabled"] is true);
        CapturePanel("frames");
        void CapturePanel(string name)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (!File.Exists(Path.Combine(directory.FullName, "Nazor.slnx"))) directory = directory.Parent ?? throw new InvalidOperationException();
            var output = Path.Combine(directory.FullName, "artifacts", "devtools-details-images", OperatingSystem.IsWindows() ? "windows" : "linux");
            Directory.CreateDirectory(output);
            foreach (var scale in new[] { 1f, 1.5f, 2f })
            {
                harness.Scene.Scale = scale; harness.Settle();
                var image = harness.Graphics.RenderToImage(harness.Scene.BuildDisplayList(), scale, harness.Scene.Background);
                File.WriteAllBytes(Path.Combine(output, name + "-" + scale.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".png"), harness.Graphics.EncodePng(image));
            }
        }
    }

    [Fact]
    public void The_tools_host_has_no_Agent_Diagnostics_or_IDE_dependency()
    {
        var pending = new Stack<System.Reflection.Assembly>();
        var seen = new HashSet<string>();
        pending.Push(typeof(InspectorShell).Assembly);
        while (pending.TryPop(out var assembly))
        {
            if (!seen.Add(assembly.FullName!)) continue;
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                Assert.False(reference.Name is "Nazor.DevTools.Agent" or "Nazor.Diagnostics" || reference.Name!.StartsWith("Microsoft.VisualStudio", StringComparison.Ordinal) || reference.Name.StartsWith("EnvDTE", StringComparison.Ordinal));
                if (reference.Name.StartsWith("Nazor.", StringComparison.Ordinal)) pending.Push(System.Reflection.Assembly.Load(reference));
            }
        }
        Assert.Contains(seen, value => value.StartsWith("Nazor.DevTools.Protocol,", StringComparison.Ordinal));
    }

    private sealed class Harness : IDisposable
    {
        internal TestHost Host { get; } = new();
        // The inspector's own fonts, copied beside the tests by the NazorLeaf package, so images match the tool.
        internal SkiaRenderer Graphics { get; } = new(new FontOptions
        {
            PrivateFontFiles = [.. Nazor.Themes.NazorLeaf.NazorLeafTheme.FontFiles.Select(file => Path.Combine(AppContext.BaseDirectory, file)),
                Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf")],
            UseSystemFonts = false, DefaultFamily = "Geist"
        });
        internal Scene Scene { get; }
        private readonly InputManager input;
        internal Harness()
        {
            Scene = new(Host.Renderer, Graphics.Fonts, Graphics.Shaper, images: Graphics.Images) { Viewport = new Size(1180, 780) };
            Scene.Styles.Theme = InspectorTheme.Theme;
            Scene.Styles.Application = InspectorTheme.StyleSheet;
            Scene.Transitions.ReducedMotion = true;
            input = new(Host.Renderer, Scene);
        }
        internal void Settle()
        {
            for (var i = 0; i < 3; i++) { Scene.Update(); Scene.BuildDisplayList(); Host.RunPending(); }
            Assert.True(Scene.WhenImagesLoaded().Wait(TimeSpan.FromSeconds(10)), "Icons did not load.");
            Scene.Update(); Scene.BuildDisplayList(); Host.RunPending();
        }
        internal void Click(ElementNode element)
        {
            var bounds = Scene.GetBounds(element);
            var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
            input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
            Host.RunPending(); Settle();
        }
        internal void SetText(ElementNode box, string text) { input.SetText(box, text); Host.RunPending(); Settle(); }
        internal void Snapshot(string folder, string name)
        {
            Settle();
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (!File.Exists(Path.Combine(directory.FullName, "Nazor.slnx"))) directory = directory.Parent ?? throw new InvalidOperationException();
            var output = Path.Combine(directory.FullName, "artifacts", folder, OperatingSystem.IsWindows() ? "windows" : "linux");
            Directory.CreateDirectory(output);
            File.WriteAllBytes(Path.Combine(output, name + ".png"), Graphics.EncodePng(Graphics.RenderToImage(Scene.BuildDisplayList(), Scene.Scale, Scene.Background)));
        }
        internal void Key(string key)
        {
            input.KeyDown(key, KeyModifiers.None, false); input.KeyUp(key, KeyModifiers.None);
            Host.RunPending(); Settle();
        }
        public void Dispose() { input.Dispose(); Scene.Dispose(); Host.Dispose(); Graphics.Dispose(); }
    }
}
