using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Style;

namespace Nazor.Rendering.Tests;

public sealed class UnitsPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(UnitsPage));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            Box(page, 3, "half");
            Box(page, 6, "clamped");
        }));
        builder.CloseComponent();
    }

    private static void Box(RenderTreeBuilder builder, int sequence, string name)
    {
        builder.OpenComponent<Container>(sequence, typeof(UnitsPage));
        builder.AddComponentParameter(sequence + 1, "Class", name);
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class RelativeLengthRenderingTests : IDisposable
{
    private readonly RenderHarness harness = new();

    public RelativeLengthRenderingTests()
    {
        var result = NssCompiler.Compile("UnitsPage", """
            .page { display: flex; flex-direction: column; font-size: 10px; padding: 1em; align-items: flex-start; }
            .half { width: calc(50% - 1em); height: 10vh; background: #ff0000; }
            .clamped { width: clamp(50px, 25vw, 120px); height: 2rem; margin-top: max(1em, 2vh); }
            """);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var sheet = result.StyleSheet;
        StyleRegistry.RegisterScoped(typeof(UnitsPage), () => sheet);
        harness.Host.Render<UnitsPage>();
    }

    public void Dispose() => harness.Dispose();

    private Rect Bounds(string name) =>
        harness.Scene.RootLayout.Children.SelectMany(Flatten).Single(n => n.Tag is ElementNode e && e.Attributes.GetValueOrDefault("class") as string == name).RoundedFrame;

    private static IEnumerable<Nazor.Layout.LayoutNode> Flatten(Nazor.Layout.LayoutNode node) => node.Children.SelectMany(Flatten).Prepend(node);

    [Fact]
    public void Relative_lengths_lay_out_and_follow_the_viewport()
    {
        var image = harness.Render(400, 200, 1);
        Assert.Equal(new Rect(10, 10, 180, 20), Bounds("half"));
        Assert.Equal(new Rect(10, 40, 100, 20), Bounds("clamped"));
        Assert.Equal(0xff, image.GetPixel(100, 20).R);

        harness.Render(800, 400, 1);
        Assert.Equal(new Rect(10, 10, 380, 40), Bounds("half"));
        Assert.Equal(new Rect(10, 60, 120, 20), Bounds("clamped"));
    }
}
