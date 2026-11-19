using Nazor.Controls;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class Shapes : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Shapes));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            Box(page, 3, "ellipse");
            Box(page, 6, "pill");
        }));
        builder.CloseComponent();
    }

    private static void Box(RenderTreeBuilder builder, int sequence, string name)
    {
        builder.OpenComponent<Container>(sequence, typeof(Shapes));
        builder.AddComponentParameter(sequence + 1, "Class", name);
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class PaintTests : IDisposable
{
    private readonly RenderHarness harness = new();

    public PaintTests()
    {
        var sheet = NssCompiler.Compile("Shapes", """
            .ellipse { width: 200px; height: 100px; background: #ff0000; border-radius: 50%; }
            .pill { width: 200px; height: 100px; margin-top: 20px; background: #0000ff; border-radius: 50px; }
            """).StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Shapes), () => sheet);
        harness.Host.Render<Shapes>();
    }

    public void Dispose() => harness.Dispose();

    [Fact]
    public void A_percentage_of_border_radius_follows_the_width_and_the_height()
    {
        var image = harness.Render(240, 260, 1);
        var commands = harness.Scene.BuildDisplayList().Describe();
        Assert.Contains("r=100x50 ", commands);
        Assert.Contains("r=50 ", commands);

        Assert.True(IsWhite(image.GetPixel(15, 15)), $"{image.GetPixel(15, 15)} outside the ellipse");
        Assert.True(IsBlue(image.GetPixel(15, 120 + 15)), $"{image.GetPixel(15, 135)} inside the pill");
        Assert.True(IsRed(image.GetPixel(3, 50)));
        Assert.True(IsBlue(image.GetPixel(3, 120 + 50)));
    }

    private static bool IsWhite(Nazor.Graphics.Color color) => color is { R: > 240, G: > 240, B: > 240 };

    private static bool IsRed(Nazor.Graphics.Color color) => color is { R: > 200, G: < 60, B: < 60 };

    private static bool IsBlue(Nazor.Graphics.Color color) => color is { R: < 60, G: < 60, B: > 200 };
}
