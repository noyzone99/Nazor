using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class DialogFocusPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Dialog>(0);
        builder.AddComponentParameter(1, nameof(Dialog.IsOpen), true);
        builder.AddComponentParameter(2, nameof(Dialog.Title), "Edit report");
        builder.AddComponentParameter(3, nameof(Dialog.ChildContent), (RenderFragment)(body =>
        {
            for (var index = 0; index < 8; index++)
            {
                body.OpenComponent<TextField>(0);
                body.SetKey(index);
                body.AddComponentParameter(1, nameof(TextField.Id), $"field-{index}");
                body.AddComponentParameter(2, nameof(TextField.Value), $"Value {index}");
                body.CloseComponent();
            }
        }));
        builder.AddComponentParameter(4, nameof(Dialog.Footer), (RenderFragment)(footer =>
        {
            footer.OpenComponent<Button>(0);
            footer.AddComponentParameter(1, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Save")));
            footer.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class DialogFocusRenderingTests
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
    public void Scrolling_dialog_fields_keep_their_focus_ink_on_both_sides(string theme, float scale)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = theme switch
        {
            "leaf" => NazorLeafTheme.Theme,
            "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
            _ => Nazor.Themes.Themes.Basic,
        };
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        var style = NssCompiler.Compile("dialog-focus", """
            Dialog { width: 360px; max-width: none; height: 260px; max-height: none; }
            DialogBody { scrollbar-visibility: none; }
            TextField { height: 34px; flex-shrink: 0; }
            TextField:focus-within { outline-style: none; box-shadow: 0 0 0 4px #ff00ff; }
            """);
        Assert.True(style.Success, string.Join("\n", style.Diagnostics));
        harness.Scene.Styles.Application = style.StyleSheet;
        var root = harness.Host.Render<DialogFocusPage>();
        harness.Render(640, 480, scale);
        harness.Host.RunPending();
        harness.Render(640, 480, scale);

        var body = root.Elements("DialogBody").Single();
        var scroll = harness.Scene.GetScrollState(body)!;
        Assert.True(scroll.CanScrollY);
        var header = root.Elements("DialogHeader").Single();
        var footer = root.Elements("DialogFooter").Single();
        var headerBounds = harness.Scene.GetBounds(header);
        var footerBounds = harness.Scene.GetBounds(footer);

        foreach (var index in new[] { 0, 7 })
        {
            var editor = root.Elements("TextFieldInput").Single(element => element.Id == $"field-{index}-input");
            input.Focus(editor, visible: true);
            input.BringIntoView(editor);
            var image = harness.Render(640, 480, scale);
            var field = editor.ParentElement!;
            var bounds = harness.Scene.GetBounds(field);
            var y = (int)MathF.Floor((bounds.Y + bounds.Height / 2) * scale);
            var left = image.GetPixel((int)MathF.Floor((bounds.X - 2) * scale), y);
            var right = image.GetPixel((int)MathF.Floor((bounds.Right + 2) * scale), y);
            Assert.Equal(new Nazor.Graphics.Color(255, 0, 255, 255), left);
            Assert.Equal(new Nazor.Graphics.Color(255, 0, 255, 255), right);
            Assert.Equal(headerBounds, harness.Scene.GetBounds(header));
            Assert.Equal(footerBounds, harness.Scene.GetBounds(footer));
        }

        Assert.True(scroll.OffsetY > 0);
    }
}
