using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class DamagePage : ComponentBase
{
    public bool Blue;
    public string Text = "Uno";
    public bool Dim;
    public bool Shifted;
    public bool Raised;
    public bool ShowLast = true;

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(DamagePage));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            Box(page, 10, Blue ? "box blue" : "box");
            page.OpenComponent<TextBlock>(20, typeof(DamagePage));
            page.AddComponentParameter(21, "Class", "label");
            page.AddAttribute(22, "ChildContent", (RenderFragment)(text => text.AddContent(0, Text)));
            page.CloseComponent();
            Box(page, 30, Dim ? "faded dim" : "faded");
            Box(page, 40, Shifted ? "moved shifted" : "moved");
            Box(page, 50, Raised ? "a raised" : "a");
            Box(page, 60, "b");
            if (ShowLast) Box(page, 70, "last");
            page.OpenComponent<Container>(80, typeof(DamagePage));
            page.AddComponentParameter(81, "Class", "list");
            page.AddAttribute(82, "ChildContent", (RenderFragment)(list =>
            {
                for (var i = 0; i < 10; i++)
                {
                    list.OpenComponent<Container>(0, typeof(DamagePage));
                    list.SetKey(i);
                    list.AddComponentParameter(1, "Class", i % 2 == 0 ? "item even" : "item odd");
                    list.CloseComponent();
                }
            }));
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private static void Box(RenderTreeBuilder builder, int sequence, string classes)
    {
        builder.OpenComponent<Container>(sequence, typeof(DamagePage));
        builder.AddComponentParameter(sequence + 1, "Class", classes);
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class PartialRedrawTests : IDisposable
{
    private const string Sheet = """
        .page { width: 400px; height: 300px; }
        .box { position: absolute; left: 10px; top: 10px; width: 40px; height: 40px; background: #ff0000; box-shadow: 0 2px 6px #000000; border-radius: 6px; }
        .box.blue { background: #0000ff; }
        .label { position: absolute; left: 60px; top: 10px; font-size: 18px; }
        .faded { position: absolute; left: 10px; top: 60px; width: 40px; height: 40px; background: #00aa00; opacity: 0.8; }
        .faded.dim { opacity: 0.3; }
        .moved { position: absolute; left: 10px; top: 110px; width: 30px; height: 30px; background: #aa00aa; }
        .moved.shifted { transform: translateX(50px) rotate(10deg); }
        .a, .b { position: absolute; top: 160px; width: 40px; height: 40px; }
        .a { left: 10px; background: #ffaa00; }
        .b { left: 30px; background: #00aaff; }
        .a.raised { z-index: 1; }
        .last { position: absolute; left: 10px; top: 210px; width: 40px; height: 40px; background: #333333; }
        .list { position: absolute; left: 200px; top: 10px; width: 100px; height: 100px; overflow-y: auto; }
        .item { height: 30px; flex-shrink: 0; }
        .item.odd { background: #cccccc; }
        .item.even { background: #888888; }
        """;

    private const float Scale = 1.5f;
    private const int Width = 600;
    private const int Height = 450;

    private readonly RenderHarness harness = new();
    private readonly DamagePage page;

    public PartialRedrawTests()
    {
        var sheet = NssCompiler.Compile("DamagePage", Sheet).StyleSheet;
        StyleRegistry.RegisterScoped(typeof(DamagePage), () => sheet);
        page = (DamagePage)harness.Host.Render<DamagePage>().Component;
    }

    public void Dispose() => harness.Dispose();

    private DisplayList Frame()
    {
        harness.Host.RunPending();
        harness.Scene.Viewport = new Size(Width / Scale, Height / Scale);
        harness.Scene.Scale = Scale;
        harness.Scene.Update();
        return harness.Scene.BuildDisplayList();
    }

    private void Render(DisplayList list, byte[] buffer, IReadOnlyList<PixelRect>? damage)
    {
        var pinned = System.Runtime.InteropServices.GCHandle.Alloc(buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            harness.Graphics.Render(list, pinned.AddrOfPinnedObject(), Width, Height, Width * 4, Scale, harness.Scene.Background, damage);
        }
        finally
        {
            pinned.Free();
        }
    }

    private (byte[] Partial, byte[] Full, IReadOnlyList<PixelRect> Damage) Change(Action change)
    {
        var first = Frame();
        var buffer = new byte[Width * Height * 4];
        Render(first, buffer, null);
        change();
        page.Refresh();
        var second = Frame();
        var damage = DisplayListDamage.ToPixels(DisplayListDamage.Compute(first, second, Scale)!, Scale, Width, Height);
        Render(second, buffer, damage);
        var full = new byte[Width * Height * 4];
        Render(second, full, null);
        return (buffer, full, damage);
    }

    private static void AssertSame(byte[] partial, byte[] full)
    {
        var different = Enumerable.Range(0, partial.Length).Count(i => Math.Abs(partial[i] - full[i]) > 2);
        Assert.True(different == 0, $"{different} bytes differ from a full repaint");
    }

    private static int Area(IReadOnlyList<PixelRect> damage) => damage.Sum(r => r.Width * r.Height);

    [Theory]
    [InlineData("color")]
    [InlineData("text")]
    [InlineData("opacity")]
    [InlineData("transform")]
    [InlineData("order")]
    [InlineData("removed")]
    public void A_change_repaints_its_area_with_the_pixels_of_a_full_repaint(string kind)
    {
        Action change = kind switch
        {
            "color" => () => page.Blue = true,
            "text" => () => page.Text = "Due, tre",
            "opacity" => () => page.Dim = true,
            "transform" => () => page.Shifted = true,
            "order" => () => page.Raised = true,
            _ => () => page.ShowLast = false,
        };
        var (partial, full, damage) = Change(change);
        AssertSame(partial, full);
        Assert.NotEmpty(damage);
        Assert.True(Area(damage) < Width * Height / 10, $"{Area(damage)} pixels damaged");
    }

    [Fact]
    public void Scrolling_repaints_the_scroll_viewer()
    {
        var list = harness.Host.Renderer.RootComponents.Single().Elements().Single(e => e.Attributes.GetValueOrDefault("class") is "list");
        var (partial, full, damage) = Change(() => harness.Scene.ScrollBy(list, 45));
        AssertSame(partial, full);
        Assert.True(Area(damage) <= (100 + 4) * (100 + 4) * Scale * Scale, $"{Area(damage)} pixels damaged");
    }

    [Fact]
    public void Nothing_changed_damages_nothing()
    {
        var first = Frame();
        var second = Frame();
        Assert.Empty(DisplayListDamage.Compute(first, second)!);
        Assert.Null(DisplayListDamage.Compute(null, second));
    }
}
