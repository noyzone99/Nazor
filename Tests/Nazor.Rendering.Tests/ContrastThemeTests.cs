using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Style;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;
public sealed class ThemedControls : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Window");
        builder.AddAttribute(0, "role", "window");
        builder.AddAttribute(1, "style", "padding: 12px; gap: 8px");
        builder.AddContent(2, (RenderFragment)(page =>
        {
            page.OpenComponent<TextBlock>(0);
            page.AddComponentParameter(1, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Impostazioni")));
            page.CloseComponent();
            page.OpenComponent<Button>(10);
            page.AddComponentParameter(11, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Salva")));
            page.CloseComponent();
            page.OpenComponent<Button>(20);
            page.AddComponentParameter(21, "IsDefault", true);
            page.AddComponentParameter(22, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Conferma")));
            page.CloseComponent();
            page.OpenComponent<CheckBox>(30);
            page.AddComponentParameter(31, "IsChecked", true);
            page.AddComponentParameter(32, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Solo attivi")));
            page.CloseComponent();
            page.OpenComponent<TextField>(40);
            page.AddComponentParameter(41, "Value", "Sensore");
            page.CloseComponent();
            page.OpenComponent<Button>(70);
            page.AddComponentParameter(72, "Appearance", ControlAppearance.Link);
            page.AddComponentParameter(71, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Guida")));
            page.CloseComponent();
            page.OpenComponent<Button>(80);
            page.AddComponentParameter(81, "Disabled", true);
            page.AddComponentParameter(82, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Elimina")));
            page.CloseComponent();
        }));
        builder.CloseElement();
    }
}

public sealed class ContrastThemeTests : IDisposable
{
    private static readonly SystemColors NightSky = SystemColors.Default with
    {
        Canvas = Style.Color.Black,
        CanvasText = Style.Color.White,
        Field = Style.Color.Black,
        FieldText = Style.Color.White,
        ButtonFace = Style.Color.Black,
        ButtonText = Style.Color.White,
        ButtonBorder = Style.Color.White,
        Highlight = Style.Color.FromRgb(0xd6, 0xb4, 0xfd),
        HighlightText = Style.Color.FromRgb(0x2b, 0x2b, 0x2b),
        GrayText = Style.Color.FromRgb(0xa6, 0xa6, 0xa6),
        LinkText = Style.Color.FromRgb(0x80, 0x80, 0xff),
    };
    private readonly RenderHarness harness = new();
    private readonly Nazor.Input.InputManager input;
    public ContrastThemeTests()
    {
        input = new Nazor.Input.InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true, ForcedColors: true, Colors: NightSky);
        harness.Scene.Background = Color.Black;
        harness.Host.Render<ThemedControls>();
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }

    [Fact]
    public void Every_color_drawn_is_a_system_color()
    {
        harness.Render(300, 420, 1);
        var palette = new[]
        {
            NightSky.Canvas,
            NightSky.CanvasText,
            NightSky.ButtonFace,
            NightSky.ButtonText,
            NightSky.Highlight,
            NightSky.HighlightText,
            NightSky.GrayText,
            NightSky.LinkText
        }.Select(c => new Color(c.R, c.G, c.B, c.A)).ToHashSet();
        var colors = harness.Scene.BuildDisplayList().Commands.SelectMany(command => command switch
        {
            FillRectangle fill => [fill.Color],
            DrawText text => [text.Color],
            DrawBorder border => new[] { border.Top.Color, border.Right.Color, border.Bottom.Color, border.Left.Color },
            DrawShadow shadow => [shadow.Color],
            DrawPolyline line => [line.Color],
            _ => [],
        }).Where(c => !c.IsTransparent).Distinct().ToList();
        Assert.NotEmpty(colors);
        Assert.All(colors, c => Assert.Contains(c, palette));
        Assert.Contains(palette.Single(c => c.R == 0xd6), colors);
        Assert.Contains(palette.Single(c => c.R == 0xa6), colors);
    }

    [Fact]
    public void Controls_in_a_contrast_theme_match_the_reference_image()
    {
        var image = harness.Render(300, 420, 1);
        ImageAssert.MatchesBaseline(harness.Graphics, image, "controls-contrast-300x420@1x");
    }
}
