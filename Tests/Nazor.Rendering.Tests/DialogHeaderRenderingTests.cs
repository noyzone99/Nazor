using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class DialogHeaderPage : ComponentBase
{
    [Parameter] public string Title { get; set; } = "Export report";
    [Parameter] public string? Subtitle { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Dialog>(0);
        builder.AddComponentParameter(1, nameof(Dialog.IsOpen), true);
        builder.AddComponentParameter(2, nameof(Dialog.Title), Title);
        builder.AddComponentParameter(3, nameof(Dialog.Subtitle), Subtitle);
        builder.AddComponentParameter(4, nameof(Dialog.Leading), (RenderFragment)(leading =>
        {
            leading.OpenElement(0, "HeaderMark");
            leading.AddAttribute(1, "style", "width: 14px; height: 14px; background: #000000;");
            leading.CloseElement();
        }));
        builder.CloseComponent();
    }
}

public sealed class DialogHeaderRenderingTests
{
    [Theory]
    [InlineData("basic", 1f)]
    [InlineData("basic", 1.5f)]
    [InlineData("basic", 2f)]
    [InlineData("fluent", 1f)]
    [InlineData("fluent", 1.5f)]
    [InlineData("fluent", 2f)]
    [InlineData("leaf", 1f)]
    [InlineData("leaf", 1.5f)]
    [InlineData("leaf", 2f)]
    public void The_leading_mark_stays_centered_on_the_first_title_line_when_text_wraps_or_grows(string theme, float scale)
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Theme = theme switch
        {
            "leaf" => NazorLeafTheme.Theme,
            "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
            _ => Nazor.Themes.Themes.Basic,
        };
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        var longTitle = "Export the selected quarterly financial report with its charts and notes";
        var longSubtitle = "Choose the output format and the pages to export, including the accompanying charts and notes.";
        foreach (var (title, subtitle, large) in new[]
        {
            ("Export report", (string?)null, false),
            (longTitle, "Choose a format", false),
            ("Export report", longSubtitle, false),
            ("Export report", longSubtitle, true),
        })
        {
            var style = NssCompiler.Compile("dialog-header", $$"""
                Dialog { width: 360px; max-width: none; {{(large ? "--nz-dialog-title-font-size: 32px; --nz-dialog-title-line-height: 1.4;" : "")}} }
                """);
            Assert.True(style.Success, string.Join("\n", style.Diagnostics));
            harness.Scene.Styles.Application = style.StyleSheet;
            var root = harness.Host.Render<DialogHeaderPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DialogHeaderPage.Title)] = title,
                [nameof(DialogHeaderPage.Subtitle)] = subtitle,
            }));
            var image = harness.Render(640, 480, scale);
            var titleElement = root.Elements("DialogTitle").Single();
            var titleDrawing = harness.Scene.BuildDisplayList().Commands.OfType<DrawText>().Single(text => text.Layout.Text == title);
            var titleBox = harness.Scene.GetContentBounds(titleElement);
            var firstLineCenter = titleBox.Y + titleDrawing.Layout.Lines[0].Height / 2;
            var mark = harness.Scene.GetBounds(root.Elements("HeaderMark").Single());
            Assert.InRange(mark.Y + mark.Height / 2, firstLineCenter - 0.75f, firstLineCenter + 0.75f);
            if (title == longTitle) Assert.True(titleDrawing.Layout.Lines.Count > 1);
            if (large) Assert.Equal(32, titleDrawing.Layout.Style.FontSize);

            var darkRows = new List<int>();
            var x = (int)MathF.Floor((mark.X + mark.Width / 2) * scale);
            for (var y = (int)MathF.Floor(mark.Y * scale); y < mark.Bottom * scale; y++)
            {
                var pixel = image.GetPixel(x, y);
                if (pixel.R < 50 && pixel.G < 50 && pixel.B < 50) darkRows.Add(y);
            }
            Assert.NotEmpty(darkRows);
            var pixelCenter = (darkRows.Min() + darkRows.Max() + 1) / 2f / scale;
            Assert.InRange(pixelCenter, firstLineCenter - 1, firstLineCenter + 1);
            harness.Host.Renderer.RemoveRootComponent(root);
        }
    }
}
