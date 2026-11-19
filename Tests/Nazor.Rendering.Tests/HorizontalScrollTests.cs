using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Presentation;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class Strip : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Strip));
        builder.AddComponentParameter(1, "Class", "strip");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(strip =>
        {
            strip.OpenComponent<Container>(3, typeof(Strip));
            strip.AddComponentParameter(4, "Class", "row");
            strip.AddAttribute(5, "ChildContent", (RenderFragment)(row =>
            {
                for (var i = 0; i < 6; i++)
                {
                    row.OpenComponent<Button>(6, typeof(Strip));
                    row.SetKey(i);
                    row.AddAttribute(7, "ChildContent", (RenderFragment)(text => text.AddContent(8, $"Item {i}")));
                    row.CloseComponent();
                }
            }));
            strip.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class HorizontalScrollTests : IDisposable
{
    private readonly RenderHarness harness = new();
    private readonly InputManager input;
    private readonly RootComponent root;

    public HorizontalScrollTests()
    {
        var sheet = NssCompiler.Compile("Strip", """
            .strip { width: 200px; height: 60px; overflow-x: auto; overflow-y: hidden; }
            .row { display: flex; }
            Button { width: 100px; flex-shrink: 0; padding: 0; border-width: 0; }
            """).StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Strip), () => sheet);
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<Strip>();
        harness.Render(300, 100, 1);
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }

    private ElementNode Viewer => root.Elements("Container").Single(e => e.Class == "strip");

    private ScrollState State => harness.Scene.GetScrollState(Viewer)!;

    private ElementNode Item(int index) => root.Elements("Button").ElementAt(index);

    [Fact]
    public void Content_wider_than_the_viewer_scrolls_only_sideways()
    {
        Assert.True(State.CanScrollX);
        Assert.False(State.CanScrollY);
        Assert.Equal(600, State.ContentWidth);
        Assert.Equal(200, State.ViewportWidth);
        Assert.Equal(400, State.MaxOffsetX);
    }

    [Fact]
    public void Shift_wheel_and_the_horizontal_wheel_scroll_and_the_plain_wheel_does_not()
    {
        var probe = new Point(50, 30);
        input.PointerWheel(probe, -1, KeyModifiers.None);
        Assert.Equal(0, State.OffsetX);

        input.PointerWheel(probe, -1, KeyModifiers.Shift);
        Assert.Equal(48, State.OffsetX);
        input.PointerWheel(probe, 2, 0, KeyModifiers.None);
        Assert.Equal(144, State.OffsetX);
        input.PointerWheel(probe, -100, 0, KeyModifiers.None);
        Assert.Equal(0, State.OffsetX);
    }

    [Fact]
    public void Hit_testing_painting_and_bounds_follow_the_offset()
    {
        input.PointerWheel(new Point(50, 30), 1.5f, 0, KeyModifiers.None);
        harness.Render(300, 100, 1);
        Assert.Equal(72, State.OffsetX);
        Assert.Same(Item(1), Hit(new Point(50, 5)));
        Assert.Equal(100 - 72, harness.Scene.GetBounds(Item(1)).X);
        var commands = harness.Scene.BuildDisplayList().Commands;
        Assert.Contains(commands, c => c is PushTranslate { X: -72, Y: 0 });
        Assert.Contains(commands, c => c is FillRectangle { Rect.Height: 4 } bar && bar.Rect.Bottom == 58);
    }

    [Fact]
    public void Focus_brings_an_item_into_view_and_page_keys_page_sideways()
    {
        for (var i = 0; i < 4; i++) input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(Item(3), input.Focused);
        Assert.Equal(200, State.OffsetX);

        Assert.True(input.KeyDown("Home", KeyModifiers.None, false));
        Assert.Equal(0, State.OffsetX);
        Assert.True(input.KeyDown("PageDown", KeyModifiers.None, false));
        Assert.Equal(180, State.OffsetX, 3);
        Assert.True(input.KeyDown("End", KeyModifiers.None, false));
        Assert.Equal(400, State.OffsetX);
    }

    private ElementNode? Hit(Point point)
    {
        for (var element = harness.Scene.HitTest(point); element is not null; element = element.ParentElement)
            if (element.Name == "Button") return element;
        return null;
    }
}
