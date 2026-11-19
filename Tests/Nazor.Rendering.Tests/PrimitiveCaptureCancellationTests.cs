using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class PrimitiveCapturePage : ComponentBase
{
    [Parameter] public string Kind { get; set; } = "color";
    public bool Disabled { get; set; }
    public List<object> Previews { get; } = [];
    public int Completions { get; private set; }
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Surface");
        builder.AddAttribute(1, "style", "padding: 24px;");
        if (Kind == "color")
        {
            builder.OpenComponent<ColorArea>(10);
            builder.AddComponentParameter(11, nameof(ColorArea.Color), new HsvColor(30, 0.4, 0.6));
            builder.AddComponentParameter(12, nameof(ColorArea.Disabled), Disabled);
            builder.AddComponentParameter(13, nameof(ColorArea.Style), "width: 100px; height: 100px;");
            builder.AddComponentParameter(14, nameof(ColorArea.ColorChanged), EventCallback.Factory.Create<HsvColor>(this, value => Previews.Add(value)));
            builder.AddComponentParameter(15, nameof(ColorArea.OnChange), EventCallback.Factory.Create<HsvColor>(this, () => Completions++));
            builder.CloseComponent();
        }
        else if (Kind == "drag")
        {
            builder.OpenComponent<Draggable>(20);
            builder.AddComponentParameter(21, nameof(Draggable.Disabled), Disabled);
            builder.AddComponentParameter(22, nameof(Draggable.Style), "width: 100px; height: 100px;");
            builder.AddComponentParameter(23, nameof(Draggable.OnDrag), EventCallback.Factory.Create<DragEventArgs>(this, value => Previews.Add(value)));
            builder.AddComponentParameter(24, nameof(Draggable.OnDragEnd), EventCallback.Factory.Create<DragEventArgs>(this, () => Completions++));
            builder.CloseComponent();
        }
        else
        {
            builder.OpenComponent<ResizeHandle>(30);
            builder.AddComponentParameter(31, nameof(ResizeHandle.Width), 100d);
            builder.AddComponentParameter(32, nameof(ResizeHandle.Height), 100d);
            builder.AddComponentParameter(33, nameof(ResizeHandle.Edge), ResizeEdge.Right);
            builder.AddComponentParameter(34, nameof(ResizeHandle.MaxSize), 300d);
            builder.AddComponentParameter(35, nameof(ResizeHandle.Disabled), Disabled);
            builder.AddComponentParameter(36, nameof(ResizeHandle.Style), "width: 100px; height: 100px;");
            builder.AddComponentParameter(37, nameof(ResizeHandle.OnResize), EventCallback.Factory.Create<ResizeValue>(this, value => Previews.Add(value)));
            builder.AddComponentParameter(38, nameof(ResizeHandle.OnResizeEnd), EventCallback.Factory.Create<ResizeValue>(this, () => Completions++));
            builder.CloseComponent();
        }
        builder.OpenElement(50, "Other");
        builder.AddAttribute(51, "style", "width: 100px; height: 100px;");
        builder.CloseElement();
        builder.CloseElement();
    }
}

public sealed class PrimitiveCaptureCancellationTests
{
    public static IEnumerable<object[]> Cancellations()
    {
        foreach (var kind in new[] { "color", "drag", "resize" })
        foreach (var reason in new[] { "host", "transfer", "disabled" })
            yield return [kind, reason];
    }

    [Theory]
    [MemberData(nameof(Cancellations))]
    public void Canceled_or_transferred_capture_clears_gesture_state_and_rejects_stale_motion_and_completion(string kind, string reason)
    {
        Run(kind, reason);
    }

    [Theory]
    [InlineData("color")]
    [InlineData("drag")]
    [InlineData("resize")]
    public void Ordinary_release_still_completes_a_changed_gesture_once(string kind)
    {
        Run(kind, "release");
    }

    private static void Run(string kind, string reason)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        var root = harness.Host.Render<PrimitiveCapturePage>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(PrimitiveCapturePage.Kind)] = kind,
        }));
        var page = (PrimitiveCapturePage)root.Component;
        Frame();
        var element = root.Elements(kind switch { "color" => "ColorArea", "drag" => "Draggable", _ => "ResizeHandle" }).Single();
        var bounds = harness.Scene.GetBounds(element);
        var start = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        var moved = new Point(start.X + 10, start.Y);
        input.PointerPressed(start, PointerButton.Primary, KeyModifiers.None);
        input.PointerMoved(moved, KeyModifiers.None);
        Frame();
        Assert.Same(element, input.CapturedPointer);
        Assert.Equal(true, element.Attributes.GetValueOrDefault("dragging"));
        Assert.NotEmpty(page.Previews);
        var previews = page.Previews.ToArray();

        switch (reason)
        {
            case "release": input.PointerReleased(moved, PointerButton.Primary, KeyModifiers.None); break;
            case "transfer": Assert.True(input.CapturePointer(root.Elements("Other").Single())); break;
            case "disabled": page.Disabled = true; page.Refresh(); break;
            default: input.PointerCanceled(clearHover: false); break;
        }
        Frame();
        Assert.Equal(false, element.Attributes.GetValueOrDefault("dragging"));
        Assert.False(input.HasPointerCapture(element));
        Assert.Equal(reason == "release" ? 1 : 0, page.Completions);
        input.PointerMoved(new Point(start.X + 20, start.Y), KeyModifiers.None);
        input.PointerReleased(new Point(start.X + 20, start.Y), PointerButton.Primary, KeyModifiers.None);
        Frame();
        Assert.Equal(previews, page.Previews);
        Assert.Equal(reason == "release" ? 1 : 0, page.Completions);

        void Frame()
        {
            harness.Host.RunPending();
            harness.Render(300, 300, 1);
        }
    }
}
