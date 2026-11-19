using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class FluentTimePickerTests
{
    [Theory]
    [InlineData(TimePickerPresentation.Columns, ColorScheme.Light, false, 1f)]
    [InlineData(TimePickerPresentation.Columns, ColorScheme.Dark, false, 1f)]
    [InlineData(TimePickerPresentation.List, ColorScheme.Light, false, 1f)]
    [InlineData(TimePickerPresentation.List, ColorScheme.Dark, false, 1f)]
    [InlineData(TimePickerPresentation.Columns, ColorScheme.Dark, true, 1f)]
    [InlineData(TimePickerPresentation.List, ColorScheme.Light, true, 1f)]
    [InlineData(TimePickerPresentation.Columns, ColorScheme.Dark, false, 1.5f)]
    [InlineData(TimePickerPresentation.Columns, ColorScheme.Light, false, 2f)]
    public void Classic_picker_has_inset_rows_and_a_selected_pill_with_working_keyboard_input(
        TimePickerPresentation presentation, ColorScheme scheme, bool forcedColors, float scale)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = Nazor.Themes.Fluent.Themes.Fluent;
        harness.Scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true, ForcedColors: forcedColors);
        var root = harness.Host.Render<TimePicker>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(TimePicker.Value)] = new TimeOnly(14, 30),
            [nameof(TimePicker.Use24Hour)] = true,
            [nameof(TimePicker.MinuteIncrement)] = 15,
            [nameof(TimePicker.Presentation)] = presentation,
            [nameof(Control.Style)] = "width: 150px; margin: 24px;",
        }));
        Frame();
        input.Focus(root.Single("TimePickerInput"), visible: true);
        input.KeyDown("F4", KeyModifiers.None, false);
        Frame();
        var rows = root.Elements("TimePickerOption").ToArray();
        var selected = rows.Where(row => row.Attributes.GetValueOrDefault("selected") is true).ToArray();
        Assert.Equal(presentation == TimePickerPresentation.Columns ? 2 : 1, selected.Length);
        foreach (var row in selected)
        {
            input.BringIntoView(row);
            Frame();
            var bounds = harness.Scene.GetBounds(row);
            var column = harness.Scene.GetBounds(row.ParentElement!);
            Assert.True(bounds.Height >= 32 - .01f);
            Assert.True(bounds.X - column.X >= 3.99f);
            Assert.InRange(bounds.Y, column.Y - .01f, column.Bottom - bounds.Height + .01f);
            var pill = harness.Scene.Styles.GetGeneratedElement(row, PseudoElement.Before);
            Assert.NotNull(pill);
            var marker = harness.Scene.GetBounds(pill);
            Assert.InRange(marker.Width, 3 - 1 / scale, 3 + 1 / scale);
            Assert.InRange(Math.Abs(bounds.Y + bounds.Height / 2 - marker.Y - marker.Height / 2), 0, 1 / scale);
            var text = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), drawing => drawing.Layout.Text == row.Text() && drawing.Alignment == TextAlignment.Center);
            var origin = harness.Scene.GetTransform(row).Transform(text.Origin);
            Assert.InRange(Math.Abs(origin.Y + text.Layout.Lines[0].Height / 2 - bounds.Y - bounds.Height / 2), 0, 1 / scale);
        }
        var other = rows.First(row => row.Attributes.GetValueOrDefault("selected") is not true);
        Assert.Null(harness.Scene.Styles.GetGeneratedElement(other, PseudoElement.Before));
        var directory = Path.Combine(AppContext.BaseDirectory, "artifacts", "fluent-fields");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, $"time-{presentation}-{scheme}-{forcedColors}-{scale}.png"), harness.Graphics.EncodePng(harness.Render(420, 360, scale)));
        input.Focus(selected[0], visible: true);
        input.KeyDown("ArrowDown", KeyModifiers.None, false);
        Frame();
        input.KeyDown("Enter", KeyModifiers.None, false);
        Frame();
        Assert.Empty(root.Elements("TimePickerPopup"));
        Assert.Equal(presentation == TimePickerPresentation.Columns ? "15:30" : "14:45", root.Single("TimePickerInput").Attributes["value"]);

        void Frame()
        {
            harness.Host.RunPending();
            harness.Render(420, 360, scale);
            harness.Host.RunPending();
            harness.Render(420, 360, scale);
        }
    }
}
