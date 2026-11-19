using System.Reflection;
using System.Text;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Nss;
using Nazor.Presentation;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.Fluent;
using Nazor.Themes.NazorLeaf;
using FluentThemes = Nazor.Themes.Fluent.Themes;

namespace Nazor.Rendering.Tests;

public sealed class FontSymbolFallbackTests
{
    private sealed class Face(string family, params int[] coverage) : FontFace
    {
        public override string FamilyName => family;
        public override int Weight => 400;
        public override bool Italic => false;
        public override FontMetrics Metrics => new(1000, 800, 200, 0);
        public override bool HasGlyph(int codepoint) => coverage.Contains(codepoint);
    }

    private sealed class Provider(params FontFace[] faces) : IFontProvider
    {
        public int Version => 0;
        public int Matches { get; private set; }
        public (int Weight, bool Italic, float Size)? LastRequest { get; private set; }
        public FontFace Match(IReadOnlyList<string> families, int weight, bool italic) => Match(families, weight, italic, 14);
        public FontFace Match(IReadOnlyList<string> families, int weight, bool italic, float size)
        {
            Matches++;
            LastRequest = (weight, italic, size);
            return faces.FirstOrDefault(face => families.Contains(face.FamilyName, StringComparer.OrdinalIgnoreCase)) ?? faces[0];
        }
        public FontFace? Fallback(int codepoint, FontFace preferred) => throw new InvalidOperationException("Symbol candidates must match without character fallback.");
    }

    private sealed class Alternative(string family, string glyph) : IFontSymbol
    {
        public string FontFamily => family;
        public string Glyph => glyph;
        public IReadOnlyList<IFontSymbol> Fallbacks { get; set; } = [];
    }

    [Theory]
    [InlineData(true, true, "Native", "A")]
    [InlineData(true, false, "Portable", "😀")]
    [InlineData(false, true, "Portable", "😀")]
    public void The_exact_family_and_all_Unicode_scalars_must_be_available(bool nativeFamily, bool nativeGlyph, string family, string glyph)
    {
        var provider = new Provider(new Face("Default", 'A'), new Face("Portable", 0x1f600),
            new Face(nativeFamily ? "Native" : "Other", nativeGlyph ? 'A' : 'B'));
        var style = new TextStyle(["Ignored"], 24, 600, true);
        var selected = FontSymbolResolution.Resolve([new("Native", "A"), new("Portable", "😀")], style, provider);
        Assert.Equal(new FontSymbol(family, glyph), selected);
        Assert.Equal((600, true, 24f), provider.LastRequest);
    }

    [Fact]
    public void A_partially_covered_sequence_is_skipped_and_missing_candidates_draw_no_symbol()
    {
        var provider = new Provider(new Face("Native", 'A'), new Face("Portable", 'B'));
        Assert.Equal(new FontSymbol("Portable", "B"), FontSymbolResolution.Resolve(
            [new("Native", "A😀"), new("Portable", "B")], new(["Native"], 16), provider));
        Assert.Null(FontSymbolResolution.Resolve([new("Native", "😀")], new(["Native"], 16), provider));
    }

    [Fact]
    public void Cycles_and_long_graphs_have_bounded_resolution_and_stable_priority()
    {
        var cycle = new Alternative("Missing", "A");
        cycle.Fallbacks = [cycle, new FontSymbol("Fira Sans", "B")];
        using var harness = new RenderHarness();
        harness.Host.Render<Icon>(Parameters(cycle));
        harness.Render(80, 80, 1);
        var text = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.Equal("B", text.Layout.Text);
        Assert.All(text.Layout.Shaped.Runs, run => Assert.Equal("Fira Sans", run.Face.FamilyName));

        var head = new Alternative("Missing0", "A");
        var tail = head;
        for (var i = 1; i < 33; i++)
        {
            var next = new Alternative(i == 32 ? "Fira Sans" : "Missing" + i, "B");
            tail.Fallbacks = [next];
            tail = next;
        }
        using var bounded = new RenderHarness();
        bounded.Host.Render<Icon>(Parameters(head));
        bounded.Render(80, 80, 1);
        Assert.Empty(bounded.Scene.BuildDisplayList().Commands.OfType<DrawText>());
    }

    [Theory]
    [InlineData("block")]
    [InlineData("flex")]
    [InlineData("grid")]
    public void Typed_candidates_ignore_NSS_remapping_and_use_their_own_pairs_in_each_layout(string display)
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Application = Compile("Icon { display: " + display + "; font-symbol: \"Fira Sans\" \"C\"; }");
        harness.Host.Render<Icon>(Parameters(new Alternative("Missing", "A") { Fallbacks = [new FontSymbol("Fira Sans", "B")] }));
        harness.Render(80, 80, 1);
        Assert.Equal("B", Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>()).Layout.Text);
    }

    [Theory]
    [InlineData("block")]
    [InlineData("flex")]
    [InlineData("grid")]
    public void Explicit_Fluent_symbols_keep_their_packaged_mapping_when_NSS_requests_another_symbol(string display)
    {
        using var harness = new RenderHarness();
        RegisterPortable(harness.Graphics.FontProvider);
        harness.Scene.Styles.Application = Compile("Icon { display: " + display + "; font-symbol: \"Fira Sans\" \"C\"; }");
        harness.Host.Render<Icon>(Parameters(FluentSymbol.Search16Regular));
        harness.Render(80, 80, 1);
        var drawing = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.Equal(FluentSymbol.Search16Regular.Glyph, drawing.Layout.Text);
        Assert.All(drawing.Layout.Shaped.Runs, run => Assert.Equal("FluentSystemIcons-Regular", run.Face.FamilyName));
    }

    public static IEnumerable<object[]> IconBoxes =>
        from theme in new[] { "basic", "fluent", "leaf" }
        from scale in new[] { 1f, 1.5f, 2f }
        from nested in new[] { false, true }
        select new object[] { theme, scale, nested };

    [Theory]
    [MemberData(nameof(IconBoxes))]
    public void Icon_boxes_use_one_em_line_metrics_and_paint_inside_the_box_in_each_theme(string theme, float scale, bool nested)
    {
        using var harness = new RenderHarness();
        RegisterPortable(harness.Graphics.FontProvider);
        harness.Scene.Styles.Theme = theme switch
        {
            "fluent" => FluentThemes.Fluent,
            "leaf" => NazorLeafTheme.Theme,
            _ => Nazor.Themes.Themes.Basic,
        };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Icon.Symbol)] = WinUISymbol.Search,
            [nameof(Icon.Style)] = "font-size: 24px; color: #ff00ff; margin: 12px",
        });
        var element = nested ? harness.Host.Render<IconBoxProbe>(parameters).Single("Icon") : harness.Host.Render<Icon>(parameters).Single("Icon");
        var image = harness.Render(80, 80, scale);
        var box = harness.Scene.GetContentBox(element);
        var drawing = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.Equal(24f, drawing.Layout.Height);
        Assert.Equal(box.Bottom, drawing.Origin.Y + drawing.Layout.Baseline);
        var ink = Enumerable.Range(0, image.Width * image.Height).Where(index =>
        {
            var pixel = image.GetPixel(index % image.Width, index / image.Width);
            return pixel.R > pixel.G + 20 && pixel.B > pixel.G + 20;
        }).ToArray();
        Assert.NotEmpty(ink);
        Assert.All(ink, index =>
        {
            var x = (index % image.Width + .5f) / scale;
            var y = (index / image.Width + .5f) / scale;
            Assert.InRange(x, box.X, box.Right);
            Assert.InRange(y, box.Y, box.Bottom);
        });
    }

    [Fact]
    public void The_application_can_override_the_icon_line_height()
    {
        using var harness = new RenderHarness();
        RegisterPortable(harness.Graphics.FontProvider);
        harness.Host.Render<Icon>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Icon.Symbol)] = WinUISymbol.Search,
            [nameof(Icon.Style)] = "font-size: 24px; line-height: 2",
        }));
        harness.Render(80, 80, 1);
        Assert.Equal(48f, Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>()).Layout.Height);
    }

    [Fact]
    public void Registering_a_preferred_private_font_invalidates_paired_glyph_selection()
    {
        using var harness = new RenderHarness();
        IFontSymbol symbol = new Alternative("FluentSystemIcons-Regular", FluentSymbol.Search16Regular.Glyph)
        {
            Fallbacks = [new FontSymbol("Fira Sans", "B")],
        };
        harness.Host.Render<Icon>(Parameters(symbol));
        harness.Render(80, 80, 1);
        Assert.Equal("B", Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>()).Layout.Text);
        RegisterPortable(harness.Graphics.FontProvider);
        harness.Render(80, 80, 1);
        var text = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.Equal(FluentSymbol.Search16Regular.Glyph, text.Layout.Text);
        Assert.All(text.Layout.Shaped.Runs, run => Assert.Equal("FluentSystemIcons-Regular", run.Face.FamilyName));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Every_WinUI_symbol_shapes_and_paints_its_explicit_portable_fallback_without_system_fonts(float scale)
    {
        foreach (var symbol in Catalog())
        {
            using var harness = new RenderHarness();
            RegisterPortable(harness.Graphics.FontProvider);
            harness.Host.Render<Icon>(Parameters(symbol));
            var image = harness.Render(80, 80, scale);
            var expected = symbol.Fallbacks[^1];
            var text = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>());
            Assert.Equal(expected.Glyph, text.Layout.Text);
            Assert.All(text.Layout.Shaped.Runs, run =>
            {
                Assert.Equal(expected.FontFamily, run.Face.FamilyName);
                Assert.All(run.Glyphs, glyph => Assert.NotEqual((ushort)0, glyph));
            });
            Assert.Contains(Enumerable.Range(0, image.Width * image.Height), index =>
            {
                var pixel = image.GetPixel(index % image.Width, index / image.Width);
                return pixel.R > pixel.G + 20 && pixel.B > pixel.G + 20;
            });
        }
    }

    [Fact]
    public void Font_symbol_is_noninherited_does_not_rewrite_editable_text_and_restores_ordinary_text_with_none()
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Application = Compile("* { font-family: Fira Sans; } Box, Parent { font-symbol: \"Fira Sans\" \"B\"; } Editor { font-symbol: \"Fira Sans\" \"C\"; }");
        harness.Host.Render<SymbolsProbe>();
        harness.Render(300, 120, 1);
        var texts = harness.Scene.BuildDisplayList().Commands.OfType<DrawText>().Select(draw => draw.Layout.Text).ToArray();
        Assert.Equal(["B", "child", "editable"], texts);
        harness.Scene.Styles.Application = Compile("* { font-family: Fira Sans; } Box { font-symbol: none; }");
        harness.Render(300, 120, 1);
        Assert.Equal(["original", "child", "editable"], harness.Scene.BuildDisplayList().Commands.OfType<DrawText>().Select(draw => draw.Layout.Text));
    }

    [Fact]
    public void Windows_installed_Segoe_can_be_used_privately_without_system_font_discovery_or_redistribution()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Requires an installed Windows Segoe Fluent Icons font; portable coverage runs separately.");
        var installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "SegoeIcons.ttf");
        Assert.SkipUnless(File.Exists(installed), "The Windows Segoe Fluent Icons font is unavailable.");
        using var harness = new RenderHarness();
        RegisterPortable(harness.Graphics.FontProvider);
        harness.Graphics.FontProvider.Register(installed);
        harness.Host.Render<Icon>(Parameters(WinUISymbol.Search));
        harness.Render(80, 80, 1);
        var text = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.Equal("\uE721", text.Layout.Text);
        Assert.All(text.Layout.Shaped.Runs, run => Assert.Equal("Segoe Fluent Icons", run.Face.FamilyName));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Fluent_internal_icon_parts_paint_paired_portable_glyphs_without_system_fonts(float scale)
    {
        foreach (var (part, kind, symbol) in Parts()) AssertPart(part, kind, symbol, scale, nativeFont: null);
    }

    [Fact]
    public void Fluent_internal_icon_parts_use_covered_native_glyphs_when_the_actual_private_provider_has_Segoe()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Requires an installed Windows Segoe Fluent Icons font; portable coverage runs separately.");
        var installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "SegoeIcons.ttf");
        Assert.SkipUnless(File.Exists(installed), "The Windows Segoe Fluent Icons font is unavailable.");
        foreach (var (part, kind, symbol) in Parts()) AssertPart(part, kind, symbol, 1.5f, installed);
    }

    private static void AssertPart(string part, string? kind, WinUISymbol symbol, float scale, string? nativeFont)
    {
        using var harness = new RenderHarness();
        RegisterPortable(harness.Graphics.FontProvider);
        if (nativeFont is not null) harness.Graphics.FontProvider.Register(nativeFont);
        harness.Scene.Styles.Theme = FluentThemes.Fluent;
        var root = harness.Host.Render<PartProbe>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(PartProbe.Part)] = part, [nameof(PartProbe.Kind)] = kind,
        }));
        harness.Render(100, 100, scale);
        var before = harness.Scene.Styles.GetGeneratedElement(root.Single(part), PseudoElement.Before);
        Assert.NotNull(before);
        var candidates = harness.Scene.GetStyle(before).Get(StyleProperties.FontSymbol).Items;
        Assert.Equal(3, candidates.Count);
        IFontSymbol expected = nativeFont is null ? symbol.Fallbacks[^1] : symbol;
        Assert.Equal(new FontSymbol(expected.FontFamily, expected.Glyph), candidates[nativeFont is null ? 2 : 0]);
        var drawing = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>());
        Assert.Equal(expected.Glyph, drawing.Layout.Text);
        if (part is "CalendarPrevious" or "CalendarNext") Assert.Equal(8f, drawing.Layout.Style.FontSize);
        Assert.All(drawing.Layout.Shaped.Runs, run =>
        {
            Assert.Equal(expected.FontFamily, run.Face.FamilyName);
            Assert.All(run.Glyphs, glyph => Assert.NotEqual((ushort)0, glyph));
        });
        var ink = harness.Graphics.RenderToImage(new DisplayList(new Size(100, 100),
            [drawing with { Color = new Nazor.Graphics.Color(255, 0, 255, 255) }]), scale, new Nazor.Graphics.Color(255, 255, 255, 255));
        Assert.Contains(Enumerable.Range(0, ink.Width * ink.Height), index =>
        {
            var pixel = ink.GetPixel(index % ink.Width, index / ink.Width);
            return pixel.R > pixel.G + 20 && pixel.B > pixel.G + 20;
        });
    }

    private static IEnumerable<(string Part, string? Kind, WinUISymbol Symbol)> Parts()
    {
        yield return ("SearchFieldMark", null, WinUISymbol.Search);
        yield return ("DatePickerButton", null, WinUISymbol.Calendar);
        yield return ("CalendarPrevious", null, WinUISymbol.CalendarPrevious);
        yield return ("CalendarNext", null, WinUISymbol.CalendarNext);
        yield return ("TabsAdd", null, WinUISymbol.Add);
        yield return ("TabClose", null, WinUISymbol.Cancel);
        yield return ("MessageBoxIcon", null, WinUISymbol.Information);
        yield return ("MessageBoxIcon", "warning", WinUISymbol.Warning);
        yield return ("MessageBoxIcon", "error", WinUISymbol.Error);
        yield return ("MessageBoxIcon", "question", WinUISymbol.Help);
        yield return ("PasswordFieldReveal", null, WinUISymbol.RevealPassword);
    }

    private sealed class PartProbe : ComponentBase
    {
        [Parameter] public string Part { get; set; } = "";
        [Parameter] public string? Kind { get; set; }

        protected override void BuildRenderTree(Nazor.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, Part is "TabsAdd" or "TabClose" ? "Tabs" : "Container");
            builder.AddAttribute(1, "presentation", "document");
            builder.AddAttribute(2, "style", "display: block; margin: 12px; width: 60px; height: 60px");
            builder.OpenElement(3, Part);
            builder.AddAttribute(4, "icon", Kind);
            builder.CloseElement();
            builder.CloseElement();
        }
    }

    private sealed class IconBoxProbe : ComponentBase
    {
        [Parameter] public IFontSymbol? Symbol { get; set; }
        [Parameter] public string? Style { get; set; }

        protected override void BuildRenderTree(Nazor.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Container");
            builder.OpenComponent<Icon>(1);
            builder.AddComponentParameter(2, nameof(Icon.Symbol), Symbol);
            builder.AddComponentParameter(3, nameof(Icon.Style), Style);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }

    private sealed class SymbolsProbe : ComponentBase
    {
        protected override void BuildRenderTree(Nazor.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Stack");
            builder.AddAttribute(1, "style", "display: flex; flex-direction: column; gap: 4px");
            builder.OpenElement(2, "Box"); builder.AddContent(3, "original"); builder.CloseElement();
            builder.OpenElement(4, "Parent");
            builder.OpenElement(5, "Child"); builder.AddContent(6, "child"); builder.CloseElement(); builder.CloseElement();
            builder.OpenElement(7, "Editor"); builder.AddAttribute(8, "role", "textbox"); builder.AddAttribute(9, "contenteditable", true);
            builder.AddContent(10, "editable"); builder.CloseElement();
            builder.CloseElement();
        }
    }

    private static IEnumerable<WinUISymbol> Catalog() => typeof(WinUISymbol).GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Select(property => Assert.IsType<WinUISymbol>(property.GetValue(null)));

    private static ParameterView Parameters(IFontSymbol symbol) => ParameterView.FromDictionary(new Dictionary<string, object?>
    {
        [nameof(Icon.Symbol)] = symbol, [nameof(Icon.Style)] = "font-size: 32px; color: #ff00ff",
    });

    private static void RegisterPortable(SkiaFontProvider provider)
    {
        foreach (var path in FluentThemes.FontFiles) provider.Register(Path.Combine(AppContext.BaseDirectory, path));
    }

    private static StyleSheet Compile(string source)
    {
        var result = NssCompiler.Compile("symbols", source);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }
}
