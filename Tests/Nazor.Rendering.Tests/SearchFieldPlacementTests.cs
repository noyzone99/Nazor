using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.Fluent;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class SearchFieldPlacementTests
{
    public static IEnumerable<object[]> Placements =>
        from theme in new[] { "basic", "fluent", "leaf" }
        from placement in Enum.GetValues<SearchIconPlacement>()
        from suggestions in new[] { false, true }
        select new object[] { theme, placement, suggestions };

    [Theory]
    [MemberData(nameof(Placements))]
    public void Search_icon_moves_independently_of_scope_and_clear_in_plain_and_suggestion_fields(string theme, SearchIconPlacement placement, bool suggestions)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        if (theme == "fluent")
            foreach (var file in Nazor.Themes.Fluent.Themes.FontFiles)
                ((SkiaFontProvider)harness.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, file));
        harness.Scene.Styles.Theme = theme switch
        {
            "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
            "leaf" => NazorLeafTheme.Theme,
            _ => Nazor.Themes.Themes.Basic,
        };
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        var root = harness.Host.Render<SearchField<string>>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(SearchField<string>.Value)] = "R",
            [nameof(SearchField<string>.Scopes)] = new[] { "Everywhere", "People" },
            [nameof(SearchField<string>.Scope)] = "Everywhere",
            [nameof(SearchField<string>.Suggestions)] = suggestions ? new[] { "Rome" } : null,
            [nameof(SearchField<string>.IconPlacement)] = placement,
            [nameof(SearchField<string>.Style)] = "width: 380px; margin: 20px;",
        }));
        harness.Render(440, 180, 1);
        harness.Host.RunPending();
        harness.Render(440, 180, 1);
        var frame = root.Single("SearchField");
        var mark = root.Single("SearchFieldMark");
        var editor = root.Single("SearchFieldInput");
        var clear = root.Single("SearchFieldClear");
        Assert.Same(frame, mark.ParentElement);
        var scope = harness.Scene.GetBounds(root.Single("SearchFieldScope"));
        var editorBounds = harness.Scene.GetBounds(editor);
        var markBounds = harness.Scene.GetBounds(mark);
        var clearBounds = harness.Scene.GetBounds(clear);
        Assert.True(scope.Right <= editorBounds.X);
        Assert.True(editorBounds.Right <= clearBounds.X);
        var end = placement == SearchIconPlacement.End || placement == SearchIconPlacement.ThemeDefault && theme == "fluent";
        Assert.True(end ? clearBounds.Right <= markBounds.X : scope.Right <= markBounds.X && markBounds.Right <= editorBounds.X);
        if (theme != "basic")
        {
            var icon = harness.Scene.Styles.GetGeneratedElement(mark, PseudoElement.Before);
            Assert.NotNull(icon);
            Assert.NotEmpty(icon.Text());
            Assert.Equal(0d, harness.Scene.GetStyle(mark).Get(StyleProperties.StrokeTrimEnd));
            if (theme == "fluent")
            {
                var symbol = FluentSymbol.Search16Regular;
                Assert.Equal(symbol.Glyph, icon.Text());
                Assert.Equal([symbol.FontFamily], harness.Scene.GetStyle(icon).Get(StyleProperties.FontFamily).Items);
                var drawn = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), text => text.Layout.Text == symbol.Glyph);
                var run = Assert.Single(drawn.Layout.Shaped.Runs);
                Assert.Equal(symbol.FontFamily, run.Face.FamilyName);
                Assert.All(run.Glyphs, glyph => Assert.NotEqual((ushort)0, glyph));
                Assert.Equal(12f, drawn.Layout.Style.FontSize);
                Assert.Equal(28f, clearBounds.Width);
                Assert.Equal(28f, markBounds.Width);
                var clearIcon = harness.Scene.Styles.GetGeneratedElement(clear, PseudoElement.Before);
                Assert.Equal(FluentSymbol.Dismiss12Regular.Glyph, clearIcon?.Text());
                Assert.Equal(Length.Px(12), harness.Scene.GetStyle(clearIcon!).Get(StyleProperties.FontSize));
                Assert.Equal(0d, harness.Scene.GetStyle(clear).Get(StyleProperties.StrokeTrimEnd));
                Assert.InRange(Math.Abs(markBounds.Y + markBounds.Height / 2 - editorBounds.Y - editorBounds.Height / 2), 0, 1);
            }
        }
        input.Focus(editor, visible: true);
        harness.Render(440, 180, 1);
        Assert.Same(editor, input.Focused);
    }
}
