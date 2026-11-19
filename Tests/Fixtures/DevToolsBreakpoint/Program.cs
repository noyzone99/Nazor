using System.Diagnostics;
using Nazor.Diagnostics;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.DevTools.BreakpointFixture;

public static class FixtureState
{
    public static IWindow Window = null!;
    public static DevToolsSession Session = null!;
    public static ParentComponent Parent = null!;
    public static ChildComponent Child = null!;
    public static int CompletedValue;
    public static string CompletedText = "";
    public static string Phase = "initial";
}

public sealed class ParentComponent : ComponentBase
{
    protected override void OnInitialized() => FixtureState.Parent = this;
    public int Value = 7;
    public int GetterCalls;
    public int ToStringCalls;
    public bool BreakDuringRender;

    public int Dangerous { get { GetterCalls++; throw new InvalidOperationException("Inspection evaluated a getter."); } }
    public override string ToString() { ToStringCalls++; throw new InvalidOperationException("Inspection called ToString."); }
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Panel");
        if (BreakDuringRender)
        {
            Value = 31;
            FixtureState.Phase = "render-mutation";
            Debugger.Break();
        }
        builder.OpenComponent<ChildComponent>(1);
        builder.AddComponentParameter(2, nameof(ChildComponent.Parameter), Value);
        builder.AddComponentReferenceCapture(3, child => FixtureState.Child = (ChildComponent)child);
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class ChildComponent : ComponentBase
{
    public int ParameterField;
    [Parameter] public int Parameter { get => ParameterField; set => ParameterField = value; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "TextBlock");
        builder.AddContent(1, ParameterField);
        builder.CloseElement();
    }
}

internal static class Program
{
    private static void Main(string[] args)
    {
        if (Environment.GetEnvironmentVariable("NAZOR_DEVTOOLS_NATIVE_PROBE") == "1") { RunNative(args[0]); return; }
        new Thread(Idle) { IsBackground = true, Name = "Unrelated fixture thread" }.Start();
        using var host = new TestHost();
        using var graphics = new SkiaRenderer();
        using var scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper, images: graphics.Images) { Viewport = new Size(320, 200) };
        using var input = new InputManager(host.Renderer, scene);
        using var session = new DevToolsSession(host.Renderer, scene, input);
        FixtureState.Window = host.Window;
        FixtureState.Session = session;
        using var agent = new DevToolsAgent(session);
        var root = host.Render<ParentComponent>();
        FixtureState.Parent = (ParentComponent)root.Component;
        scene.Update();
        var image = graphics.RenderToImage(scene.BuildDisplayList(), 1, Color.White);
        File.WriteAllBytes(args[0] + ".png", graphics.EncodePng(image));
        FixtureState.CompletedValue = FixtureState.Parent.Value;
        FixtureState.CompletedText = root.Text();
        host.RunPending();
        if (!SpinWait.SpinUntil(() => DebuggerRoots.Windows.Any(w => w.SessionId == session.SessionId && w.TreeJson.Length > 0), TimeSpan.FromSeconds(5)))
            throw new TimeoutException("No debugger tree was materialized.");
        File.WriteAllText(args[0] + ".descriptor", System.Text.Json.JsonSerializer.Serialize(agent.Descriptor));
        File.WriteAllText(args[0] + ".ready", System.Text.Json.JsonSerializer.Serialize(new { processId = Environment.ProcessId, runtime = Environment.Version.ToString(), os = System.Runtime.InteropServices.RuntimeInformation.OSDescription }));
        if (!SpinWait.SpinUntil(() => File.Exists(args[0]), TimeSpan.FromSeconds(120)))
            throw new TimeoutException("No debugger controller attached.");
        FixtureState.Parent.Value = 11;
        FixtureState.Phase = "before-render";
        Debugger.Break();
        FixtureState.Parent.Value = 12;
        FixtureState.Parent.BreakDuringRender = true;
        FixtureState.Parent.Refresh();
        host.RunPending();
        FixtureState.Phase = "after-render";
        Debugger.Break();
        if (FixtureState.Parent.GetterCalls != 0 || FixtureState.Parent.ToStringCalls != 0)
            throw new InvalidOperationException("Inspection executed target code.");
        File.WriteAllText(args[0] + ".result", root.Text());
    }

    private static void RunNative(string gate)
    {
        var idle = new Thread(Idle) { IsBackground = true, Name = "Unrelated debugger frame" };
        idle.Start();
        var builder = NazorApplication.CreateBuilder(platformHost: new DesktopPlatformHost());
        builder.Diagnostics.DevChannel = false;
        builder.UseDevTools();
        builder.Application.CreateMainWindow<ParentComponent>(window => { window.Title = "Nazor DevTools breakpoint target"; window.Width = 320; window.Height = 200; window.Activate = false; });
        builder.Diagnostics.ConfigureWindow += window =>
        {
            FixtureState.Window = window;
            FixtureState.Session = window.GetDiagnostics()!;
            var agent = window.GetDevTools()!;
            _ = Task.Run(async () =>
            {
                if (!SpinWait.SpinUntil(() => DebuggerRoots.Windows.Any(w => w.SessionId == agent.Descriptor.Identity.SessionId && w.TreeJson.Length > 0), TimeSpan.FromSeconds(10))) return;
                window.Dispatcher.Post(() =>
                {
                    FixtureState.CompletedValue = FixtureState.Parent.Value;
                    FixtureState.CompletedText = window.Root.Text();
                    File.WriteAllText(gate + ".descriptor", System.Text.Json.JsonSerializer.Serialize(agent.Descriptor));
                    File.WriteAllText(gate + ".ready", System.Text.Json.JsonSerializer.Serialize(new { processId = Environment.ProcessId, runtime = Environment.Version.ToString(), os = System.Runtime.InteropServices.RuntimeInformation.OSDescription, native = true }));
                });
                while (!File.Exists(gate)) await Task.Delay(25);
                if (Environment.GetEnvironmentVariable("NAZOR_DEVTOOLS_NATIVE_F12") == "1")
                {
                    await window.Dispatcher.InvokeAsync(() => { ((Nazor.Platform.IWindowHost)window).OnKeyDown("F12", KeyModifiers.None, false); ((Nazor.Platform.IWindowHost)window).OnKeyUp("F12", KeyModifiers.None); });
                    var opening = Stopwatch.StartNew();
                    while (!agent.IsConnected && opening.Elapsed < TimeSpan.FromSeconds(20)) await Task.Delay(50);
                    if (!agent.IsConnected) throw new TimeoutException("F12 did not connect the native inspector before the first breakpoint.");
                }
                window.Dispatcher.Post(() =>
                {
                    FixtureState.Parent.Value = 11;
                    FixtureState.Phase = "before-render";
                    Debugger.Break();
                    FixtureState.Parent.Value = 12;
                    FixtureState.Parent.BreakDuringRender = true;
                    FixtureState.Parent.Refresh();
                    window.Dispatcher.Post(() =>
                    {
                        FixtureState.Phase = "after-render";
                        Debugger.Break();
                        if (FixtureState.Parent.GetterCalls != 0 || FixtureState.Parent.ToStringCalls != 0) throw new InvalidOperationException("Inspection executed target code.");
                        File.WriteAllText(gate + ".result", window.Root.Text());
                        window.Close();
                    });
                });
            });
        };
        using var app = builder.Build();
        app.Run();
    }

    private static void Idle()
    {
        while (true) Thread.Sleep(100);
    }
}
