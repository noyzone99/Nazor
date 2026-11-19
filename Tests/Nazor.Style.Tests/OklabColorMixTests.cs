using System.Text.Json;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

[Collection("StyleRegistry")]
public sealed class OklabColorMixTests
{
    public static IEnumerable<object[]> OpaqueReferenceSurfaces()
    {
        using var input = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryPaths.Root, "Tests", "Verification", "Oklab-Reference-Colors.json")));
        foreach (var palette in input.RootElement.GetProperty("palettes").EnumerateArray())
        {
            var variables = palette.GetProperty("variables");
            foreach (var (name, percentage) in new[] { ("tray", variables.GetProperty("tint").GetString()!), ("pane", "1.5%"), ("s1", "4%"), ("s2", "6%"), ("s3", "9%") })
            {
                var bytes = palette.GetProperty("surfaces").GetProperty(name).GetProperty("rgba").EnumerateArray().Select(value => value.GetByte()).ToArray();
                yield return [palette.GetProperty("scheme").GetString()!, palette.GetProperty("seed").GetString()!, name,
                    variables.GetProperty("primary").GetString()!, variables.GetProperty("n-" + name).GetString()!, percentage,
                    (int)bytes[0], (int)bytes[1], (int)bytes[2], (int)bytes[3]];
            }
        }
    }

    [Theory]
    [MemberData(nameof(OpaqueReferenceSurfaces))]
    public void All_seed_and_scheme_surfaces_match_the_actual_opaque_Chrome_reference_bytes(string scheme, string seed, string surface, string primary, string neutral, string percentage, int red, int green, int blue, int alpha)
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<OklabColorPage>();
        engine.Application = Compile($"OklabRoot {{ --accent: {primary}; --neutral: {neutral}; --amount: {percentage}; }} OklabChild {{ background: color-mix(in oklab, var(--accent) var(--amount), var(--neutral)); }}");
        var color = engine.GetStyle(root.Single("OklabChild")).BackgroundColor;
        Assert.True(color == new Color((byte)red, (byte)green, (byte)blue, (byte)alpha), $"{scheme}/{seed}/{surface}: {color}");
    }

    [Theory]
    [InlineData("color-mix(in oklab, red, blue)", "#8c53a2")]
    [InlineData("color-mix(in oklab, white, black)", "#636363")]
    [InlineData("color-mix(in oklab, red 25%, blue)", "#5147d2")]
    [InlineData("color-mix(in oklab, red, blue 75%)", "#5147d2")]
    [InlineData("color-mix(in oklab, red 30%, blue 30%)", "#8c53a299")]
    [InlineData("color-mix(in oklab, red 60%, blue 60%)", "#8c53a2")]
    [InlineData("color-mix(in oklab, red, white)", "#ffa191")]
    [InlineData("color-mix(in oklab, blue, yellow)", "#6cabc7")]
    [InlineData("color-mix(in oklab, #005fb8 90%, #fff)", "#2a70c0")]
    [InlineData("color-mix(in oklab, #123456 0%, #abcdef)", "#abcdef")]
    [InlineData("COLOR-MIX(IN OKLAB, RED, BLUE)", "#8c53a2")]
    [InlineData("color-mix(in oklab, color-mix(in oklab, white, black), black)", "#222222")]
    [InlineData("color-mix(in oklab, color-mix(in oklab, red, white), blue)", "#877cd4")]
    public void Rectangular_interpolation_normalization_and_clipping_follow_the_reference(string value, string expected) =>
        Assert.Equal(expected, Parse(value).ToString());

    [Theory]
    [InlineData("color-mix(in oklab, transparent, #ff0000)", "#ff000080")]
    [InlineData("color-mix(in oklab, #123456 25%, transparent 25%)", "#12345640")]
    [InlineData("color-mix(in oklab, #ff000000, #00ff0000)", "#00000000")]
    [InlineData("color-mix(in oklab, #ff000080 20%, #ff000040 60%)", "#ff000040")]
    [InlineData("color-mix(in oklab, #ff000080 0%, #0000ff40)", "#0000ff40")]
    [InlineData("color-mix(in oklab, color-mix(in oklab, white 5%, black 5%) 96%, transparent 4%)", "#63636318")]
    public void Alpha_is_premultiplied_before_color_interpolation_and_scaled_after_normalization(string value, string expected) =>
        Assert.Equal(expected, Parse(value).ToString());

    [Theory]
    [InlineData("color-mix(in hsl, red, blue)")]
    [InlineData("color-mix(in oklch, red, blue)")]
    [InlineData("color-mix(in oklab, red 0%, blue 0%)")]
    [InlineData("color-mix(in oklab, red 101%, blue)")]
    [InlineData("color-mix(in oklab, red 1e999%, blue)")]
    [InlineData("color-mix(in oklab, currentcolor, blue)")]
    [InlineData("color-mix(in oklab, red, currentcolor)")]
    public void Unsupported_spaces_dynamic_currentcolor_and_invalid_percentages_remain_diagnostics(string value)
    {
        var result = NssCompiler.Compile("invalid", $"Item {{ color: {value}; }}");
        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "NZS102");
    }

    [Fact]
    public void Pending_variables_and_system_aliases_follow_live_changes_without_recompiling_the_sheet()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        var root = host.Render<OklabColorPage>();
        var page = (OklabColorPage)root.Component;
        engine.Application = Compile("OklabRoot { --accent: red; --mixed: color-mix(in oklab, var(--accent), Canvas); } OklabChild { background: var(--mixed); }");
        engine.Preferences = engine.Preferences with { Colors = SystemColors.Default with { Canvas = Color.White } };
        var child = root.Single("OklabChild");
        Assert.Equal(Parse("color-mix(in oklab, red, white)"), engine.GetStyle(child).BackgroundColor);
        page.RootStyle = "--accent: blue"; page.Refresh(); host.RunPending();
        Assert.Equal(Parse("color-mix(in oklab, blue, white)"), engine.GetStyle(child).BackgroundColor);
        engine.Preferences = engine.Preferences with { Colors = SystemColors.Default with { Canvas = Color.Black } };
        Assert.Equal(Parse("color-mix(in oklab, blue, black)"), engine.GetStyle(child).BackgroundColor);
        page.RootStyle = "--accent: red; --nz-system-canvas: #00ff00"; page.Refresh(); host.RunPending();
        Assert.Equal(Parse("color-mix(in oklab, red, #00ff00)"), engine.GetStyle(child).BackgroundColor);
    }

    [Fact]
    public void Compiler_JSON_and_CSharp_preserve_static_and_pending_color_mixes()
    {
        var sheet = Compile("OklabRoot { --accent: red; --mixed: color-mix(in oklab, var(--accent), blue); background: color-mix(in oklab, red, blue); } OklabChild { background: var(--mixed); }");
        var restored = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(sheet));
        Assert.Equal(sheet.Describe(), restored.Describe());
        Assert.Equal(CSharpEmitter.Emit(sheet), CSharpEmitter.Emit(restored));
        Assert.Contains("oklab", CSharpEmitter.Emit(sheet));
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer) { Application = restored };
        var root = host.Render<OklabColorPage>();
        Assert.Equal(Parse("color-mix(in oklab, red, blue)"), engine.GetStyle(root.Single("OklabRoot")).BackgroundColor);
        Assert.Equal(engine.GetStyle(root.Single("OklabRoot")).BackgroundColor, engine.GetStyle(root.Single("OklabChild")).BackgroundColor);
    }

    private static Color Parse(string value) => Assert.IsType<Color>(Assert.IsType<ValueDeclaration>(Assert.Single(Assert.Single(Compile($"Item {{ color: {value}; }}").Rules).Declarations)).Value);
    private static StyleSheet Compile(string text)
    {
        var result = NssCompiler.Compile("oklab", text);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }
}

public sealed class OklabColorPage : ComponentBase
{
    public string? RootStyle { get; set; }
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "OklabRoot");
        builder.AddAttribute(1, "style", RootStyle);
        builder.OpenElement(2, "OklabChild");
        builder.CloseElement();
        builder.CloseElement();
    }
}
