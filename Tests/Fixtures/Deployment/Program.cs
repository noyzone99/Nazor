using System.Globalization;
using System.Reflection;
using Nazor;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Themes.NazorLeaf;

namespace Deployment;

internal static class Program
{
    internal static string Output = "";
    internal static bool Passed;
    private static int Main(string[] args)
    {
        var identity = System.Reflection.Assembly.GetEntryAssembly()!.GetCustomAttributes<System.Reflection.AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "Nazor.ApplicationId").Value;
        if (identity != "org.nazor.deployment") throw new InvalidOperationException("Application identity metadata was not preserved.");
        Output = Path.GetFullPath(args[0]);
        if (File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "payload.txt")).Trim() != "deployment asset")
            throw new InvalidOperationException("The bundled content lost its file path.");
        var plain = new PlainParameters();
        ParameterView.FromDictionary(new Dictionary<string, object?> { ["Value"] = 42 }).SetParameterProperties(plain);
        if (plain.Value != 42) throw new InvalidOperationException("Attributed plain-object parameter assignment changed.");
        var builder = NazorApplication.CreateBuilder();
        builder.Diagnostics.DevChannel = false;
        builder.Fonts.AddNazorLeafFonts();
        builder.Application.Culture = builder.Application.UICulture = CultureInfo.GetCultureInfo("en-US");
        builder.Application.CreateMainWindow<NativeProbe>(window =>
        {
            window.Title = "Deployment probe";
            window.Width = 320;
            window.Height = 200;
            window.Activate = false;
            window.Decorations = WindowDecorationPolicy.Prefer(
                WindowDecorations.Native(WindowTitleBarContent.Nazor, WindowCaptionButtons.Native),
                WindowDecorations.Nazor());
        });
        using var app = builder.Build();
        app.Appearance.Theme = NazorLeafTheme.Theme;
        app.Run();
        return Passed ? 0 : 1;
    }
}

public sealed class PlainParameters
{
    [Parameter] public int Value { get; set; }
}

public abstract class ProbeServices : ComponentBase
{
    [Inject] private IWindow InjectedWindow { get; set; } = null!;
    protected IWindow Window => InjectedWindow;
}

public sealed class NativeProbe : ProbeServices
{
    private enum DeploymentMode { Automatic, Manual }
    [Inject] public NavigationManager Navigation { get; set; } = null!;
    [Inject] public IWindowService Windows { get; set; } = null!;
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Probe>(0);
        builder.CloseComponent();
        builder.OpenComponent<CascadingValue<string>>(1);
        builder.AddComponentParameter(2, "Value", "cascade");
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenComponent<Readout<int>>(0);
            content.AddComponentParameter(1, "Value", 42);
            content.AddComponentParameter(2, "Default", null);
            content.AddComponentParameter(3, "extra", "captured");
            content.CloseComponent();
        }));
        builder.CloseComponent();
        builder.OpenComponent<Frame>(4);
        builder.CloseComponent();
        builder.OpenComponent<GenericDefault<GenericOnlyValue>>(5);
        builder.AddComponentParameter(6, "Value", null);
        builder.CloseComponent();
        builder.OpenComponent<Nested>(7);
        builder.CloseComponent();
        builder.OpenComponent<PropertyGrid>(8);
        builder.AddComponentParameter(9, nameof(PropertyGrid.ShowToolbar), false);
        builder.AddComponentParameter(10, nameof(PropertyGrid.ShowDescription), false);
        builder.AddComponentParameter(11, nameof(PropertyGrid.Items), new PropertyGridItem[]
        {
            new()
            {
                Name = "DeploymentMode", PropertyType = typeof(DeploymentMode), Getter = static () => DeploymentMode.Automatic,
                Setter = static _ => { }, ChoiceText = static value => value is DeploymentMode.Automatic ? "Automatic deployment" : "Manual deployment",
            },
        });
        builder.CloseComponent();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        var window = (NazorWindow)Window;
        try
        {
            await Task.Delay(200);
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                using var process = System.Diagnostics.Process.GetCurrentProcess();
                if (!process.Modules.Cast<System.Diagnostics.ProcessModule>().Any(module => module.ModuleName == "Microsoft.UI.Windowing.dll"))
                    throw new InvalidOperationException("The published native title bar silently fell back to Win32.");
            }
            await Navigation.NavigateToAsync("/details/42");
            foreach (var invalid in new[] { new Dictionary<string, object?>(), new Dictionary<string, object?> { ["Label"] = null } })
            {
                var rejected = false;
                try { Windows.Open<Secondary>(invalid); }
                catch (InvalidOperationException) { rejected = true; }
                if (!rejected) throw new InvalidOperationException("Required window parameter/nullability validation was lost.");
            }
            var secondary = Windows.Open<Secondary>(new Dictionary<string, object?> { ["Label"] = "secondary" }, new() { Activate = false });
            var retained = ((NazorWindow)secondary).Root.Component;
            await secondary.SetDecorationsAsync(WindowDecorationPolicy.Require(
                WindowDecorations.Nazor(WindowTitleBarContent.User, WindowCaptionButtons.User)));
            var userNodes = Elements(((NazorWindow)secondary).Root.Nodes).ToArray();
            if (!userNodes.Any(element => element.Name == "TitleBar" && element.Attributes.GetValueOrDefault("source") is "user") ||
                !userNodes.Any(element => element.Id == "user-close"))
                throw new InvalidOperationException("Published User decoration content was not mounted.");
            await secondary.SetDecorationsAsync(WindowDecorationPolicy.Require(WindowDecorations.Nazor()));
            var themedNodes = Elements(((NazorWindow)secondary).Root.Nodes).ToArray();
            if (!ReferenceEquals(retained, ((NazorWindow)secondary).Root.Component) ||
                !themedNodes.Any(element => element.Name == "LeafMark") || themedNodes.Any(element => element.Id == "user-close"))
                throw new InvalidOperationException("Published static theme presenters or inactive User content changed.");
            secondary.Close();
            var button = Elements(window.Root.Nodes).Single(e => e.Id == "increment");
            await window.Renderer.DispatchEventAsync(button, "onclick", new PointerEventArgs { Button = PointerButton.Primary });
            await Task.Delay(200);
            if (!Elements(window.Root.Nodes).Any(e => e.Name == "TextBlock" && Text(e) == "Deployment 1"))
                throw new InvalidOperationException("The published component did not handle its event.");
            var text = string.Concat(window.Root.Nodes.Select(Text));
            if (!text.Contains("Automatic deployment", StringComparison.Ordinal))
                throw new InvalidOperationException("The published PropertyGrid lost its enum editor or explicit choice labels.");
            if (!text.Contains("cascade:42:7:captured", StringComparison.Ordinal) || !text.Contains("Route 42", StringComparison.Ordinal) || !text.Contains("generic-default:9", StringComparison.Ordinal) || !text.Contains("nested", StringComparison.Ordinal))
                throw new InvalidOperationException("Generic parameters, cascading values or route discovery changed: " + text);
            if (window.FrameCount == 0 || window.Exceptions.Count != 0) throw new InvalidOperationException("The native window failed to render.");
            // Await the submitted UI frame: GPU capture alone does not flush pending window paint messages.
            var presented = 0;
            Action<PresentedFrame> completed = frame => presented = Math.Max(presented, frame.UiFrame);
            window.FramePresented += completed;
            RenderedImage image;
            try
            {
                var target = window.FrameCount + 1;
                window.RequestFrame();
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (presented < target)
                {
                    if (DateTime.UtcNow >= deadline) throw new TimeoutException("The final UI frame was not presented.");
                    await Task.Delay(10);
                }
                image = await window.CaptureFrameAsync();
            }
            finally { window.FramePresented -= completed; }
            Directory.CreateDirectory(Path.GetDirectoryName(Program.Output)!);
            File.WriteAllBytes(Program.Output, image.Pixels);
            Console.WriteLine($"PASS: native window, injection, Razor, styles, event, content; image {image.Width}x{image.Height}");
            Program.Passed = true;
        }
        finally { window.Close(); }
    }
    private static IEnumerable<ElementNode> Elements(IEnumerable<Node> nodes) =>
        nodes.OfType<ElementNode>().SelectMany(e => new[] { e }.Concat(Elements(e.Children)));
    private static string Text(Node node) => node is TextNode text ? text.Text : string.Concat(((ElementNode)node).Children.Select(Text));
    private sealed class Nested : ComponentBase
    {
        public Nested() { }
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        { builder.OpenElement(0, "TextBlock"); builder.AddContent(1, "nested"); builder.CloseElement(); }
    }
}

public struct DefaultValue
{
    public int Number;
    public DefaultValue() => Number = 7;
}

public struct GenericOnlyValue
{
    public int Number;
    public GenericOnlyValue() => Number = 9;
    public override string ToString() => Number.ToString(CultureInfo.InvariantCulture);
}

public sealed class GenericDefault<T> : ComponentBase
{
    [Parameter] public T? Value { get; set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "TextBlock"); builder.AddContent(1, "generic-default:" + Value); builder.CloseElement();
    }
}

public sealed class Secondary : WindowComponentBase
{
    [Parameter, EditorRequired] public string Label { get; set; } = null!;
    protected override void OnInitialized() => Window.Decorations = WindowDecorationPolicy.Require(WindowDecorations.Nazor());
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "TextBlock"); builder.AddContent(1, Label); builder.CloseElement();
        builder.OpenComponent<UserTitleBar>(2);
        builder.AddComponentParameter(3, nameof(UserTitleBar.Title), "User caption");
        builder.CloseComponent();
        builder.OpenComponent<UserCaptionButtons>(4);
        builder.AddComponentParameter(5, nameof(UserCaptionButtons.ChildContent), (RenderFragment)(buttons =>
        {
            buttons.OpenComponent<Button>(0);
            buttons.AddComponentParameter(1, nameof(Button.Id), "user-close");
            buttons.AddComponentParameter(2, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Close")));
            buttons.AddComponentParameter(3, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Window.Close()));
            buttons.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class Readout<T> : ComponentBase
{
    [Parameter] public T? Value { get; set; }
    [Parameter] public DefaultValue Default { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object?> Additional { get; set; } = null!;
    [CascadingParameter] private string? Cascade { get; set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "TextBlock");
        builder.AddContent(1, $"{Cascade}:{Value}:{Default.Number}:{Additional["extra"]}");
        builder.CloseElement();
    }
}

[Route("/details/{Number:int}")]
public sealed class Details : ComponentBase
{
    [Parameter] public int Number { get; set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "TextBlock"); builder.AddContent(1, "Route " + Number); builder.CloseElement();
    }
}
