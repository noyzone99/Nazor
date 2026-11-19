using Nazor.Animation;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class TransitionContentLog
{
    public int Created { get; set; }
    public int Disposed { get; set; }
    public int Actions { get; set; }
}

public sealed class StatefulTransitionContent : ComponentBase, IDisposable
{
    [Parameter] public string Caption { get; set; } = "";
    [Parameter] public TransitionContentLog Log { get; set; } = null!;
    protected override void OnInitialized() => Log.Created++;
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Button>(0);
        builder.AddComponentParameter(1, nameof(Control.Id), Caption);
        builder.AddComponentParameter(2, nameof(Button.ChildContent), (RenderFragment)(content => content.AddContent(0, Caption)));
        builder.AddComponentParameter(3, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Log.Actions++));
        builder.CloseComponent();
    }
    public void Dispose() => Log.Disposed++;
}

public sealed class ContentTransitionPage : ComponentBase
{
    public string Key { get; set; } = "one";
    public string Caption { get; set; } = "one";
    public ContentTransitionKind Transition { get; set; } = ContentTransitionKind.Slide;
    public TransitionContentLog Log { get; } = new();
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Page");
        builder.OpenComponent<Button>(1);
        builder.AddComponentParameter(2, nameof(Control.Id), "outside");
        builder.AddComponentParameter(3, nameof(Button.ChildContent), (RenderFragment)(content => content.AddContent(0, "Outside")));
        builder.CloseComponent();
        var caption = Caption;
        builder.OpenComponent<ContentTransition>(10);
        builder.AddComponentParameter(11, nameof(ContentTransition.ContentKey), Key);
        builder.AddComponentParameter(12, nameof(ContentTransition.Transition), Transition);
        builder.AddComponentParameter(13, nameof(ContentTransition.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<StatefulTransitionContent>(0);
            content.AddComponentParameter(1, nameof(StatefulTransitionContent.Caption), caption);
            content.AddComponentParameter(2, nameof(StatefulTransitionContent.Log), Log);
            content.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }
}

public sealed class ContentTransitionTests : IDisposable
{
    private readonly ManualAnimationClock clock = new();
    private readonly RenderHarness harness;
    private readonly InputManager input;
    private readonly RootComponent root;
    private ContentTransitionPage Page => (ContentTransitionPage)root.Component;
    private ElementNode Button(string id) => root.Elements("Button").Single(element => element.Id == id);

    public ContentTransitionTests()
    {
        harness = new RenderHarness(clock: clock);
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Viewport = new Size(400, 300);
        harness.Scene.Transitions.ReducedMotion = false;
        var sheet = NssCompiler.Compile("content-lifetime", """
            ContentTransition { overlay-exit-duration: 5000ms; }
            @keyframes enter { from { opacity: 0; transform: translateX(40px); } to { opacity: 1; transform: none; } }
            ContentTransitionLayer[current=true][transition=slide] { animation: enter 1000ms linear both; }
            ContentTransitionLayer[exiting=true] { opacity: 0; transition: opacity 1000ms linear; }
            """);
        Assert.True(sheet.Success, string.Join("\n", sheet.Diagnostics));
        harness.Scene.Styles.Application = sheet.StyleSheet;
        root = harness.Host.Render<ContentTransitionPage>();
        Frame();
    }

    public void Dispose() { input.Dispose(); harness.Dispose(); }

    private void Frame()
    {
        for (var i = 0; i < 3; i++)
        {
            harness.Host.RunPending();
            harness.Scene.Update();
            harness.Scene.BuildDisplayList();
        }
    }

    private void Replace(string name)
    {
        Page.Key = Page.Caption = name;
        Page.Refresh();
        Frame();
    }

    [Fact]
    public void Replacement_keeps_the_old_subtree_identity_but_suppresses_its_input_and_semantics()
    {
        var old = Button("one");
        Replace("two");
        Assert.Same(old, Button("one"));
        Assert.Equal(2, Page.Log.Created);
        Assert.Equal(0, Page.Log.Disposed);
        var outgoing = root.Elements("ContentTransitionLayer").Single(element => element.Attributes.GetValueOrDefault("exiting") is true);
        Assert.True(outgoing.IsDisabled);
        Assert.Equal(true, outgoing.Attributes["semantic-hidden"]);
        Assert.False(input.Activate(old));
        Assert.True(input.Activate(Button("two")));
        Assert.Equal(1, Page.Log.Actions);
        Assert.Equal(1d, harness.Scene.GetEffectiveValue(outgoing, Style.StyleProperties.Opacity));
    }

    [Fact]
    public void An_unchanged_identity_updates_its_content_without_remounting_or_restarting_motion()
    {
        var old = Button("one");
        Page.Caption = "updated";
        Page.Refresh();
        Frame();
        Assert.Same(old, Button("updated"));
        Assert.Equal(1, Page.Log.Created);
        Assert.Single(root.Elements("ContentTransitionLayer"));
        Replace("two");
        clock.Advance(500);
        Frame();
        var current = root.Elements("ContentTransitionLayer").Single(element => element.Attributes.GetValueOrDefault("current") is true);
        Assert.Equal(0.5, harness.Scene.GetEffectiveValue(current, Style.StyleProperties.Opacity), 4);
        Page.Caption = "latest";
        Page.Refresh();
        Frame();
        Assert.Equal(0.5, harness.Scene.GetEffectiveValue(current, Style.StyleProperties.Opacity), 4);
        Assert.Same(current, root.Elements("ContentTransitionLayer").Single(element => element.Attributes.GetValueOrDefault("current") is true));
    }

    [Fact]
    public void Rapid_changes_bound_retention_and_reduced_motion_disposes_the_last_outgoing_subtree()
    {
        Replace("two");
        Replace("three");
        Assert.True(Button("three").ParentElement?.Attributes.GetValueOrDefault("current") is true);
        Assert.DoesNotContain(root.Elements("Button"), element => element.Id == "one");
        Assert.Equal(1, Page.Log.Disposed);
        Assert.Equal(2, root.Elements("ContentTransitionLayer").Count());
        harness.Scene.Transitions.ReducedMotion = true;
        Frame();
        Assert.Single(root.Elements("ContentTransitionLayer"));
        Assert.Equal(2, Page.Log.Disposed);
    }

    [Fact]
    public void Interrupted_entrance_exits_from_its_visible_opacity_instead_of_flashing_opaque()
    {
        Replace("two");
        clock.Advance(400);
        Frame();
        var entering = root.Elements("ContentTransitionLayer").Single(element => element.Attributes.GetValueOrDefault("current") is true);
        Assert.Equal(0.4, harness.Scene.GetEffectiveValue(entering, Style.StyleProperties.Opacity), 4);
        Replace("three");
        Assert.Equal(0.4, harness.Scene.GetEffectiveValue(entering, Style.StyleProperties.Opacity), 4);
        clock.Advance(200);
        Frame();
        Assert.Equal(0.32, harness.Scene.GetEffectiveValue(entering, Style.StyleProperties.Opacity), 4);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Replacement_moves_only_focus_that_would_be_hidden(bool focusOutside)
    {
        input.Focus(Button(focusOutside ? "outside" : "one"), visible: true);
        Replace("two");
        Assert.Same(Button(focusOutside ? "outside" : "two"), input.Focused);
    }

    [Fact]
    public void None_replaces_immediately_even_when_the_theme_retains_exits()
    {
        Page.Transition = ContentTransitionKind.None;
        Replace("two");
        Assert.Single(root.Elements("ContentTransitionLayer"));
        Assert.Equal(1, Page.Log.Disposed);
    }
}
