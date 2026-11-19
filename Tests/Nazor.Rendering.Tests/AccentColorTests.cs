using Nazor.Controls;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;
public sealed class AccentPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Window");
        builder.AddAttribute(0, "role", "window");
        builder.AddContent(1, (RenderFragment)(page =>
        {
            page.OpenComponent<Button>(0);
            page.AddComponentParameter(1, "Class", "save");
            page.AddComponentParameter(2, "IsDefault", true);
            page.AddComponentParameter(3, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Salva")));
            page.CloseComponent();
            page.OpenComponent<Button>(10);
            page.AddComponentParameter(11, "Class", "help");
            page.AddComponentParameter(13, "Appearance", ControlAppearance.Link);
            page.AddComponentParameter(12, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Guida")));
            page.CloseComponent();
            page.OpenComponent<Container>(20);
            page.AddComponentParameter(21, "Class", "probe");
            page.CloseComponent();
        }));
        builder.CloseElement();
    }
}

[Collection("StyleRegistry")]
public sealed class AccentColorTests : IDisposable
{
    private static readonly SystemColors Green = SystemColors.Default with
    {
        AccentColor = Color.FromRgb(0x10, 0x7c, 0x10),
        AccentLight1 = Color.FromRgb(0x3f, 0xa3, 0x3f),
        AccentLight2 = Color.FromRgb(0x6c, 0xcb, 0x5f),
        AccentLight3 = Color.FromRgb(0xa8, 0xe6, 0xa3),
        AccentDark1 = Color.FromRgb(0x0e, 0x70, 0x0e),
        AccentDark2 = Color.FromRgb(0x0b, 0x5a, 0x0b),
        AccentDark3 = Color.FromRgb(0x08, 0x45, 0x08),
    };
    private readonly RenderHarness harness = new();
    private readonly RootComponent root;
    public AccentColorTests()
    {
        harness.Scene.Styles.Application = Compile(".probe { background: var(--nz-accent); color: var(--nz-on-accent); }");
        root = harness.Host.Render<AccentPage>();
    }

    public void Dispose() => harness.Dispose();
    private static StyleSheet Compile(string nss)
    {
        var result = NssCompiler.Compile("accent", nss, "accent.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }

    private ComputedStyle Style(string name)
    {
        harness.Render(300, 200, 1);
        return harness.Scene.GetStyle(root.Elements().Single(e => e.Class == name));
    }

    private void Use(ColorScheme scheme, SystemColors colors) => harness.Scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true, Colors: colors);
    [Fact]
    public void The_default_accent_comes_from_the_resolved_palette()
    {
        Assert.Equal(SystemColors.Resolve().AccentColor, Style("save").BackgroundColor);
        Assert.Equal(SystemColors.Resolve().AccentColorText, Style("save").Color);
        Assert.Equal(SystemColors.Resolve().LinkText, Style("help").Color);
        Assert.Equal(SystemColors.Resolve().AccentColor, Style("probe").BackgroundColor);
    }

    [Fact]
    public void In_light_mode_the_native_accent_and_text_are_preserved()
    {
        Use(ColorScheme.Light, Green);
        Assert.Equal(Green.AccentColor, Style("save").BackgroundColor);
        Assert.Equal(Green.AccentColorText, Style("save").Color);
        Assert.Equal(Green.AccentColor, Style("probe").BackgroundColor);
        Assert.Equal(Green.AccentColorText, Style("probe").Color);
    }

    [Fact]
    public void In_dark_mode_the_native_accent_and_text_are_preserved()
    {
        Use(ColorScheme.Dark, Green);
        Assert.Equal(Green.AccentColor, Style("save").BackgroundColor);
        Assert.Equal(Green.AccentColorText, Style("save").Color);
        Assert.Equal(Green.AccentColor, Style("probe").BackgroundColor);
    }

    [Fact]
    public void An_accent_token_of_the_application_restyles_the_theme()
    {
        harness.Scene.Styles.Application = Compile(":root { --nz-accent: #c42b1c; } .probe { background: var(--nz-accent); }");
        Use(ColorScheme.Light, Green);
        Assert.Equal(Color.FromRgb(0xc4, 0x2b, 0x1c), Style("save").BackgroundColor);
        Assert.Equal(Color.FromRgb(0xc4, 0x2b, 0x1c), Style("probe").BackgroundColor);
    }

    [Fact]
    public void A_new_accent_restyles_the_window()
    {
        Assert.Equal(SystemColors.Resolve().AccentColor, Style("save").BackgroundColor);
        Use(ColorScheme.Light, Green);
        Assert.Equal(Green.AccentColor, Style("save").BackgroundColor);
        var next = SystemColors.Resolve(new Dictionary<string, Color> { ["AccentColor"] = Color.FromRgb(0x88, 0x17, 0x98) });
        Use(ColorScheme.Light, next);
        Assert.Equal(next.AccentColor, Style("save").BackgroundColor);
    }
}
