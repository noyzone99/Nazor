using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class SearchFieldSuggestionTests
{
    public static IEnumerable<object[]> Appearances =>
        from theme in new[] { "basic", "fluent", "leaf" }
        from scheme in new[] { ColorScheme.Light, ColorScheme.Dark }
        from scale in new[] { 1f, 1.5f, 2f }
        select new object[] { theme, scheme, scale };

    [Theory]
    [MemberData(nameof(Appearances))]
    public async Task Suggestion_text_is_centered_in_its_row_and_empty_results_keep_a_visible_popup(string theme, ColorScheme scheme, float scale)
    {
        using var h = new RenderHarness();
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = theme switch
        {
            "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
            "leaf" => Nazor.Themes.NazorLeaf.NazorLeafTheme.Theme,
            _ => Nazor.Themes.Themes.Basic,
        };
        h.Scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true);
        if (theme is "fluent" or "leaf")
            foreach (var file in theme == "fluent" ? Nazor.Themes.Fluent.Themes.FontFiles : Nazor.Themes.NazorLeaf.NazorLeafTheme.FontFiles)
                ((SkiaFontProvider)h.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory, file));
        var root = h.Host.Render<SearchField<string>>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Control.Style)] = "width: 180px; margin: 20px;",
            [nameof(SearchField<string>.Suggestions)] = new[] { "Rome", "Milan", "Paris" },
            [nameof(SearchField<string>.Placeholder)] = "Search cities",
            [nameof(SearchField<string>.EmptyText)] = "No matching cities",
        }));
        Frame();
        var editor = root.Single("SearchFieldInput");
        input.Focus(editor, visible: false);
        input.TextInput("Rom"); Frame();
        var row = root.Single("SearchFieldOption");
        var label = row.Elements().Single(e => e.Name == "TextBlock");
        var rowBounds = h.Scene.GetBounds(row);
        var labelBounds = h.Scene.GetBounds(label);
        var popup = h.Scene.GetBounds(root.Single("SearchFieldPopup"));
        Assert.Equal(Display.Flex, h.Scene.GetStyle(row).Display);
        Assert.InRange(Math.Abs(CenterY(rowBounds) - CenterY(labelBounds)), 0, 1.1f / scale);
        Assert.True(labelBounds.X > popup.X);
        Assert.True(labelBounds.Right < popup.Right);
        Assert.True(rowBounds.Height >= labelBounds.Height);
        Assert.Equal("Rom", root.Single("Mark").Text());
        Assert.Equal(Length.Px(0), h.Scene.GetStyle(root.Single("Mark")).Get(StyleProperties.PaddingLeft));
        Save("match");
        await ((SearchField<string>)root.Component).ClearAsync(); Frame();
        input.TextInput("Romd"); Frame();
        var empty = root.Single("SearchFieldEmpty");
        var emptyBounds = h.Scene.GetBounds(empty);
        var emptyPopup = h.Scene.GetBounds(root.Single("SearchFieldPopup"));
        Assert.Equal("No matching cities", empty.Text());
        Assert.Empty(root.Elements("SearchFieldOption"));
        Assert.True(emptyBounds.Height >= rowBounds.Height);
        Assert.True(emptyPopup.Height >= emptyBounds.Height);
        Assert.Same(editor, input.Focused);
        Save("empty");

        void Frame() { h.Host.RunPending(); h.Render(420, 240, scale); h.Host.RunPending(); h.Render(420, 240, scale); }
        void Save(string state)
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "artifacts", "search-suggestions");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, $"{theme}-{scheme}-{scale}-{state}.png"), h.Graphics.EncodePng(h.Render(420, 240, scale)));
        }
    }

    private static float CenterY(Rect bounds) => bounds.Y + bounds.Height / 2;
}
