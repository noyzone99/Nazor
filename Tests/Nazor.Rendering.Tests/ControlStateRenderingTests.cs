using Nazor.Input;
using Nazor.Nss;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

[Collection("StyleRegistry")]
public sealed class ControlStateRenderingTests
{
    [Fact]
    public void Placeholder_shown_refreshes_during_composition_cancel_commit_and_value_changes()
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<PseudoContentPage>();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        var editor = root.Elements("Editor").Single();
        harness.Scene.Styles.Application = NssCompiler.Compile("states", "Editor:placeholder-shown { opacity: .5; }").StyleSheet;
        harness.Render(300, 160, 1);
        Assert.Equal(.5, harness.Scene.GetStyle(editor).Opacity);
        input.Focus(editor, visible: false);
        input.CompositionUpdate("draft", 2);
        Assert.Equal(1, harness.Scene.GetStyle(editor).Opacity);
        input.CompositionUpdate("", 0);
        Assert.Equal(.5, harness.Scene.GetStyle(editor).Opacity);
        input.CompositionUpdate("draft", 2);
        input.CompositionCommit("value");
        harness.Render(300, 160, 1);
        Assert.Equal(1, harness.Scene.GetStyle(editor).Opacity);
        input.SetText(editor, "");
        harness.Render(300, 160, 1);
        Assert.Equal(.5, harness.Scene.GetStyle(editor).Opacity);
    }
}
