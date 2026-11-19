using Nazor.Nss;
using Nazor.Testing;

namespace Nazor.Style.Tests;

[Collection("StyleRegistry")]
public sealed class SystemPaletteTests
{
    public static TheoryData<SystemColorProfile> Profiles => new(Enum.GetValues<SystemColorProfile>());

    [Theory]
    [MemberData(nameof(Profiles))]
    public void Every_profile_has_light_and_dark_fallbacks(SystemColorProfile profile)
    {
        var light = SystemColors.Resolve(profile: profile);
        var dark = SystemColors.Resolve(scheme: ColorScheme.Dark, profile: profile);
        Assert.NotEqual(light.Canvas, dark.Canvas);
        Assert.Equal(Color.Black, SystemColors.TextOn(light.Canvas));
        Assert.Equal(Color.White, SystemColors.TextOn(dark.Canvas));
        Assert.Equal(SystemColors.TextOn(light.AccentColor), light.AccentColorText);
        Assert.Equal(SystemColors.TextOn(dark.AccentColor), dark.AccentColorText);
        foreach (var palette in new[] { light, dark })
        {
            Assert.All(SystemColors.Names.Concat(SystemColors.AccentShades), name => Assert.NotNull(palette[name]));
            Assert.All(new[] { "ButtonHover", "ButtonDisabledText", "FieldHover", "Tooltip", "TooltipText", "Header", "HeaderText", "Success", "Warning", "Error" },
                name => Assert.NotNull(palette[name]));
        }
    }

    [Fact]
    public void Partial_palettes_preserve_native_colors_and_derive_the_missing_accent_shades()
    {
        var green = Color.FromRgb(0x10, 0x7c, 0x10);
        var supplied = new Dictionary<string, Color>
        {
            ["accentcolor"] = green, ["AccentDark2"] = Color.Black,
            ["Tooltip"] = Color.Black, ["TooltipText"] = Color.FromRgb(255, 255, 0),
            ["Header"] = Color.Black, ["ButtonHover"] = Color.Black,
            ["HeaderInactiveText"] = Color.White,
        };
        var colors = SystemColors.Resolve(supplied, profile: SystemColorProfile.KDE);
        supplied["Tooltip"] = Color.White;
        Assert.Equal(green, colors.AccentColor);
        Assert.Equal(Color.Black, colors.AccentDark2);
        Assert.True(colors.AccentDark1.G > colors.AccentDark1.R);
        Assert.True(colors.AccentLight1.G > green.G);
        Assert.True(colors.AccentDark3.G < green.G);
        Assert.Equal(Color.Black, colors["tooltip"]);
        Assert.Equal(Color.FromRgb(255, 255, 0), colors["TooltipText"]);
        Assert.Equal(Color.White, colors["HeaderText"]);
        Assert.Equal(Color.White, colors["ButtonHoverText"]);
        Assert.Equal(Color.White, colors["HeaderInactiveText"]);
    }

    [Fact]
    public void Native_foregrounds_win_and_missing_foregrounds_are_derived_from_their_background()
    {
        var colors = SystemColors.Resolve(new Dictionary<string, Color>
        {
            ["Canvas"] = Color.Black, ["CanvasText"] = Color.FromRgb(255, 255, 0),
            ["Field"] = Color.Black, ["ButtonFace"] = Color.White,
            ["AccentColor"] = Color.FromRgb(255, 185, 0),
            ["SelectedItem"] = Color.Black, ["SuccessBackground"] = Color.Black,
        });
        Assert.Equal(Color.FromRgb(255, 255, 0), colors.CanvasText);
        Assert.Equal(Color.White, colors.FieldText);
        Assert.Equal(Color.Black, colors.ButtonText);
        Assert.Equal(Color.Black, colors.AccentColorText);
        Assert.Equal(Color.White, colors.SelectedItemText);
        Assert.Equal(Color.White, colors["SuccessText"]);
    }

    [Fact]
    public void Semantic_and_backend_extension_colors_are_exposed_to_NSS_and_update_live()
    {
        using var host = new Nazor.Testing.TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Nested>();
        var compiled = NssCompiler.Compile("palette", "Top { background: var(--nz-system-tooltip); color: var(--nz-system-tooltip-text); } Leaf { color: var(--nz-system-header-inactive-text); }");
        Assert.True(compiled.Success, string.Join("\n", compiled.Diagnostics));
        engine.Application = compiled.StyleSheet;
        var colors = new Dictionary<string, Color> { ["Tooltip"] = Color.Black, ["TooltipText"] = Color.White, ["HeaderInactiveText"] = Color.White };
        engine.Preferences = new MediaPreferences(ColorScheme.Light, true, Colors: SystemColors.Resolve(colors));
        Assert.Equal(Color.Black, engine.GetStyle(root.Single("Top")).BackgroundColor);
        Assert.Equal(Color.White, engine.GetStyle(root.Single("Leaf")).Color);
        colors["Tooltip"] = Color.White;
        colors["TooltipText"] = Color.Black;
        engine.Preferences = engine.Preferences with { Colors = SystemColors.Resolve(colors) };
        Assert.Equal(Color.White, engine.GetStyle(root.Single("Top")).BackgroundColor);
        Assert.Equal(Color.Black, engine.GetStyle(root.Single("Top")).Color);
        Assert.Equal("--nz-system-header-inactive-text", SystemColors.Property("HeaderInactiveText"));
        Assert.Null(SystemColors.Variable("Tooltip"));
        Assert.Null(SystemColors.Property("bad); color: red"));
    }

    [Fact]
    public void Changing_only_the_scheme_refreshes_the_implicit_palette()
    {
        using var host = new Nazor.Testing.TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<Nested>();
        engine.Application = NssCompiler.Compile("palette", "Top { background: Canvas; color: CanvasText; }").StyleSheet;
        var light = engine.GetStyle(root.Single("Top")).BackgroundColor;
        engine.Preferences = new MediaPreferences(ColorScheme.Dark, true);
        Assert.NotEqual(light, engine.GetStyle(root.Single("Top")).BackgroundColor);
        Assert.Equal(SystemColors.Resolve(scheme: ColorScheme.Dark).CanvasText, engine.GetStyle(root.Single("Top")).Color);
    }
}
