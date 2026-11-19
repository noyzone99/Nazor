using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class PresenceMountedPage : ComponentBase
{
    public bool Present { get; set; }
    public bool KeepMounted { get; set; } = true;
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);
    public int Exits { get; private set; }
    public TransitionContentLog Log { get; } = new();
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Presence>(0);
        builder.AddComponentParameter(1, nameof(Presence.Present), Present);
        builder.AddComponentParameter(2, nameof(Presence.KeepMounted), KeepMounted);
        builder.AddComponentParameter(3, nameof(Presence.ExitDuration), Duration);
        builder.AddComponentParameter(4, nameof(Presence.OnExited), EventCallback.Factory.Create(this, () => Exits++));
        builder.AddComponentParameter(5, nameof(Presence.ChildContent), (RenderFragment<PresenceState>)(state => content =>
        {
            content.OpenElement(0, "MountedContent");
            content.AddAttribute(1, "disabled", !state.IsPresent);
            content.AddAttribute(2, "semantic-hidden", !state.IsPresent);
            content.AddAttribute(3, "exiting", state.IsExiting);
            content.OpenComponent<StatefulTransitionContent>(4);
            content.AddComponentParameter(5, nameof(StatefulTransitionContent.Caption), "inside");
            content.AddComponentParameter(6, nameof(StatefulTransitionContent.Log), Log);
            content.CloseComponent();
            content.CloseElement();
        }));
        builder.CloseComponent();
    }
}

public sealed class PresenceMountedTests : IDisposable
{
    private readonly RenderHarness harness = new();
    private readonly InputManager input;
    private readonly RootComponent root;
    private PresenceMountedPage Page => (PresenceMountedPage)root.Component;

    public PresenceMountedTests()
    {
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Viewport = new Size(400, 200);
        root = harness.Host.Render<PresenceMountedPage>();
        Frame();
    }

    public void Dispose() { input.Dispose(); harness.Dispose(); }
    private void Frame() { harness.Host.RunPending(); harness.Scene.Update(); harness.Host.RunPending(); harness.Scene.Update(); }
    private void Update(Action action) { action(); Page.Refresh(); Frame(); }

    [Fact]
    public void Kept_content_uses_the_same_subtree_before_opening_through_exit_and_after_completion()
    {
        var button = root.Single("Button");
        Assert.Equal(1, Page.Log.Created);
        Assert.Equal(0, Page.Exits);
        Assert.False(input.Activate(button));
        Update(() => Page.Present = true);
        Assert.Same(button, root.Single("Button"));
        Assert.True(input.Activate(button));
        Update(() => Page.Present = false);
        Assert.Equal(true, root.Single("MountedContent").Attributes["exiting"]);
        Assert.Equal(0, Page.Exits);
        Assert.False(input.Activate(button));
        Update(() => Page.Duration = TimeSpan.Zero);
        Assert.Same(button, root.Single("Button"));
        Assert.Equal(false, root.Single("MountedContent").Attributes["exiting"]);
        Assert.Equal(1, Page.Exits);
        Page.Refresh();
        Frame();
        Assert.Equal(1, Page.Exits);
        Update(() => Page.Present = true);
        Update(() => Page.Present = false);
        Assert.Equal(2, Page.Exits);
        Assert.Equal(0, Page.Log.Disposed);
    }

    [Fact]
    public void Releasing_keep_mounted_after_completion_disposes_the_closed_subtree_once()
    {
        var button = root.Single("Button");
        Update(() => { Page.Duration = TimeSpan.Zero; Page.Present = true; });
        Update(() => Page.Present = false);
        Assert.False(button.IsRemoved);
        Update(() => Page.KeepMounted = false);
        Assert.True(button.IsRemoved);
        Assert.Empty(root.Elements("MountedContent"));
        Assert.Equal(1, Page.Log.Disposed);
        Assert.Equal(1, Page.Exits);
    }
}
