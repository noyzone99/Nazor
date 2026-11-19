using System.Globalization;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class SegmentedPickerRenderingTests
{
    public static IEnumerable<object[]> Cases =>
        from theme in new[] { "basic", "fluent", "leaf" }
        from scale in new[] { 1f, 1.5f, 2f }
        from culture in new[] { "en-US", "it-IT", "ja-JP" }
        select new object[] { theme, scale, culture };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Date_columns_keep_nine_slots_center_the_selection_and_follow_themes_and_culture(string theme, float scale, string culture)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        Configure(harness, theme);
        var root = harness.Host.Render<SegmentedDatePicker>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(SegmentedDatePicker.Value)] = new DateOnly(2026, 11, 12),
            [nameof(SegmentedDatePicker.Culture)] = CultureInfo.GetCultureInfo(culture),
            [nameof(Control.Style)] = "margin: 300px 0 0 60px",
        }));
        Frame();
        await ((SegmentedDatePicker)root.Component).OpenAsync();
        Frame();
        var popup = root.Single("SegmentedDatePickerPopup");
        var bounds = harness.Scene.GetBounds(popup);
        var entry = harness.Scene.GetBounds(root.Single("SegmentedDatePicker"));
        Assert.Equal(296f, bounds.Width, 3);
        Assert.InRange(bounds.Height, 397f, 399f);
        Assert.NotEqual(0, harness.Scene.GetStyle(popup).BackgroundColor.A);
        var columns = root.Elements("SegmentedPickerColumn").ToArray();
        Assert.Equal(root.Elements("SegmentedPickerSegment").Select(e => e.Attributes["segment"]), columns.Select(e => e.Attributes["column"]));
        foreach (var column in columns)
        {
            var options = column.Elements().Where(e => e.Name is "SegmentedPickerOption" or "SegmentedPickerSpacer").ToArray();
            Assert.Equal(9, options.Length);
            var selected = Assert.Single(options, e => e.Attributes.GetValueOrDefault("current") is true);
            var row = harness.Scene.GetBounds(selected);
            Assert.InRange(row.Height, 40 - 1 / scale, 40 + 1 / scale);
            Assert.InRange(row.Y + row.Height / 2 - entry.Y - entry.Height / 2, -1 / scale, 1 / scale);
        }
        if (theme == "fluent")
        {
            var month = columns.Single(e => e.Attributes["column"] is "month");
            var day = columns.Single(e => e.Attributes["column"] is "day");
            Assert.True(harness.Scene.GetBounds(month).Width > harness.Scene.GetBounds(day).Width * 1.6);
        }
        Assert.All(root.Elements("SegmentedPickerActionIcon"), icon =>
        {
            var box = harness.Scene.GetBounds(icon);
            Assert.Equal(16f, box.Width, 3);
            Assert.Equal(16f, box.Height, 3);
        });
        var actions = root.Single("SegmentedPickerActions");
        Assert.Equal(Length.Px(41), harness.Scene.GetStyle(actions).Get(StyleProperties.Height).Length);
        Assert.InRange(harness.Scene.GetBounds(actions).Height, 41 - .5f / scale - .0001f, 41 + .5f / scale + .0001f);
        var cancel = root.Elements("SegmentedPickerActionIcon").Single(e => e.ParentElement?.Name == "SegmentedPickerCancel");
        var firstStroke = Assert.IsType<ElementNode>(harness.Scene.Styles.GetGeneratedElement(cancel, PseudoElement.Before));
        var secondStroke = Assert.IsType<ElementNode>(harness.Scene.Styles.GetGeneratedElement(cancel, PseudoElement.After));
        Assert.NotEqual(0, harness.Scene.GetStyle(firstStroke).BackgroundColor.A);
        Assert.NotEqual(0, harness.Scene.GetStyle(secondStroke).BackgroundColor.A);
        if (scale == 1 && culture == "en-US") Save(harness, theme);

        void Frame()
        {
            for (var i = 0; i < 3; i++) { harness.Host.RunPending(); harness.Render(900, 900, scale); }
        }
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    public async Task Time_popup_keeps_selected_rows_centered_in_a_constrained_viewport(string theme)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        Configure(harness, theme);
        var root = harness.Host.Render<SegmentedTimePicker>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(SegmentedTimePicker.Value)] = new TimeOnly(11, 51),
            [nameof(SegmentedTimePicker.Culture)] = CultureInfo.GetCultureInfo("it-IT"),
            [nameof(Control.Style)] = "margin-top: 100px",
        }));
        Frame();
        await ((SegmentedTimePicker)root.Component).OpenAsync();
        Frame();
        var popup = harness.Scene.GetBounds(root.Single("SegmentedTimePickerPopup"));
        Assert.Equal(242f, popup.Width, 3);
        Assert.InRange(popup.Y, 0, 1);
        Assert.InRange(popup.Bottom, 259, 260);
        var viewport = harness.Scene.GetBounds(root.Single("SegmentedPickerColumns"));
        foreach (var selected in root.Elements("SegmentedPickerOption").Where(e => e.Attributes.GetValueOrDefault("current") is true))
        {
            var row = harness.Scene.GetBounds(selected);
            Assert.InRange(row.Y + row.Height / 2 - viewport.Y - viewport.Height / 2, -1, 1);
        }
        Assert.InRange(harness.Scene.GetBounds(root.Single("SegmentedPickerActions")).Bottom, 259, 260);

        void Frame()
        {
            for (var i = 0; i < 3; i++) { harness.Host.RunPending(); harness.Render(500, 260, 1); }
        }
    }

    private static void Configure(RenderHarness harness, string theme)
    {
        harness.Scene.Styles.Theme = theme switch
        {
            "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
            "leaf" => NazorLeafTheme.Theme,
            _ => Nazor.Themes.Themes.Basic,
        };
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
    }

    private static void Save(RenderHarness harness, string theme, [System.Runtime.CompilerServices.CallerFilePath] string source = "")
    {
        var directory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "../../artifacts/rendering"));
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, $"segmented-picker-{theme}.png"), harness.Graphics.EncodePng(harness.Render(900, 900, 1)));
    }
}
