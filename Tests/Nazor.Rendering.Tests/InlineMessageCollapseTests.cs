using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class InlineMessageCollapsePage : ComponentBase
{
    public bool Open { get; set; } = true;
    public int Changes { get; private set; }
    public int Closed { get; private set; }
    public int Actions { get; private set; }
    public InlineMessage Message { get; private set; } = null!;
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Column");
        builder.AddAttribute(1, "style", "display: flex; flex-direction: column; width: 340px;");
        builder.OpenComponent<InlineMessage>(10);
        builder.AddComponentParameter(11, nameof(InlineMessage.Title), "Earlier note");
        builder.CloseComponent();
        builder.OpenComponent<InlineMessage>(20);
        builder.AddComponentParameter(21, nameof(InlineMessage.Id), "target");
        builder.AddComponentParameter(22, nameof(InlineMessage.Title), "Build complete");
        builder.AddComponentParameter(23, nameof(InlineMessage.IsOpen), Open);
        builder.AddComponentParameter(24, nameof(InlineMessage.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => { Open = value; Changes++; }));
        builder.AddComponentParameter(25, nameof(InlineMessage.OnClose), EventCallback.Factory.Create(this, () => Closed++));
        builder.AddComponentParameter(26, nameof(InlineMessage.IsClosable), true);
        builder.AddComponentParameter(27, nameof(InlineMessage.ChildContent), (RenderFragment)(content => content.AddContent(0, "The report is ready to review.")));
        builder.AddComponentParameter(28, nameof(InlineMessage.Actions), (RenderFragment)(actions =>
        {
            actions.OpenComponent<Button>(0);
            actions.AddComponentParameter(1, nameof(Button.Id), "inside");
            actions.AddComponentParameter(2, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Actions++));
            actions.AddComponentParameter(3, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Review")));
            actions.CloseComponent();
        }));
        builder.AddComponentReferenceCapture(29, value => Message = (InlineMessage)value);
        builder.CloseComponent();
        builder.OpenElement(30, "Following");
        builder.AddAttribute(31, "style", "height: 20px; background: green;");
        builder.CloseElement();
        builder.CloseElement();
    }
}

public sealed class InlineMessageCollapseTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Leaf_collapses_the_complete_flow_box_and_spacing_without_remeasuring_clipped_content_or_restarting_exit(bool reduced)
    {
        var clock = new ManualAnimationClock();
        using var h = new RenderHarness(clock: clock);
        using var input = new InputManager(h.Host.Renderer, h.Scene);
        h.Scene.Styles.Theme = NazorLeafTheme.Theme;
        h.Scene.Transitions.ReducedMotion = reduced;
        h.Scene.Styles.Preferences = h.Scene.Styles.Preferences with { ReducedMotion = reduced };
        var root = h.Host.Render<InlineMessageCollapsePage>();
        var page = (InlineMessageCollapsePage)root.Component;
        Frame(); clock.Advance(600); Frame();
        var message = root.Elements("InlineMessage").Single(element => element.Id == "target");
        var viewport = root.Elements("InlineMessagePresence").Last();
        var content = root.Elements("InlineMessagePresenceContent").Last();
        var naturalHeight = Height(content);
        var fullHeight = Height(viewport);
        var followingY = h.Scene.GetBounds(root.Single("Following")).Y;
        Assert.Equal(naturalHeight, fullHeight);
        Assert.Equal(reduced ? TimeSpan.Zero : TimeSpan.FromMilliseconds(350), h.Scene.Styles.GetStyle(viewport).Get(StyleProperties.OverlayExitDuration));
        if (!reduced)
        {
            var duration = NssCompiler.Compile("stable-exit-test", "InlineMessagePresence { overlay-exit-duration: 10s; }");
            Assert.True(duration.Success);
            h.Scene.Styles.Application = duration.StyleSheet;
            Frame();
        }
        var inside = root.Elements("Button").Single(element => element.Id == "inside");
        input.Focus(inside, visible: true);
        await page.Message.CloseAsync(); Frame();
        Assert.False(page.Open);
        Assert.Equal(1, page.Changes);
        Assert.Equal(1, page.Closed);
        Assert.NotSame(inside, input.Focused);
        Assert.False(input.Activate(inside));
        Assert.Equal(0, page.Actions);
        if (reduced)
        {
            Assert.True(message.IsRemoved);
            Assert.DoesNotContain(content, root.Elements("InlineMessagePresenceContent"));
            Assert.Equal(0, h.Scene.GetBounds(viewport).Height);
            return;
        }
        Assert.Same(message, root.Elements("InlineMessage").Single(element => element.Id == "target"));
        Assert.Equal(naturalHeight, Height(content));
        clock.Advance(175); Frame();
        var halfway = Height(viewport);
        Assert.InRange(halfway, 0.01f, fullHeight - 0.01f);
        Assert.Equal(naturalHeight, Height(content));
        Assert.True(h.Scene.GetBounds(root.Single("Following")).Y < followingY);
        page.Open = true; page.Refresh(); Frame();
        Assert.Same(message, root.Elements("InlineMessage").Single(element => element.Id == "target"));
        Assert.Equal(halfway, Height(viewport));
        clock.Advance(600); Frame();
        Assert.Equal(fullHeight, Height(viewport));
        Assert.False(message.IsDisabled);
        Assert.Equal(1, page.Changes);
        float Height(ElementNode element) => h.Scene.GetLayoutNode(element)!.AbsoluteFrame.Height;
        void Frame() { for (var frame = 0; frame < 4; frame++) { h.Host.RunPending(); h.Render(400, 400, 1); } }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Basic_and_Fluent_keep_immediate_closing_when_no_exit_is_configured(bool fluent)
    {
        using var h = new RenderHarness();
        h.Scene.Styles.Theme = fluent ? Nazor.Themes.Fluent.Themes.Fluent : Nazor.Themes.Themes.Basic;
        var root = h.Host.Render<InlineMessageCollapsePage>();
        h.Host.RunPending(); h.Render(400, 400, 1); h.Host.RunPending(); h.Render(400, 400, 1);
        var page = (InlineMessageCollapsePage)root.Component;
        await page.Message.CloseAsync();
        h.Host.RunPending(); h.Render(400, 400, 1); h.Host.RunPending(); h.Render(400, 400, 1);
        Assert.DoesNotContain(root.Elements("InlineMessage"), element => element.Id == "target");
        Assert.Equal(1, page.Changes);
        Assert.Equal(1, page.Closed);
    }
}
