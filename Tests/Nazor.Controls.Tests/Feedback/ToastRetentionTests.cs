using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;

namespace Nazor.Controls.Tests.Feedback;

public sealed class ToastRetentionProbe : ComponentBase
{
    public FeedbackClock Clock { get; } = new();
    public ToastQueue Queue { get; }
    public bool ShowHost { get; set; } = true;
    public ToastRetentionProbe() => Queue = new(Clock);
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Button>(0);
        builder.AddComponentParameter(1, nameof(Button.Id), "outside");
        builder.AddComponentParameter(2, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Outside")));
        builder.CloseComponent();
        if (!ShowHost) return;
        builder.OpenComponent<ToastHost>(10);
        builder.AddComponentParameter(11, nameof(ToastHost.Queue), Queue);
        builder.AddComponentParameter(12, nameof(ToastHost.Time), Clock);
        builder.AddComponentParameter(13, nameof(ToastHost.MaxVisible), 1);
        builder.CloseComponent();
    }
}

public sealed class ItemsExtentProbe : ComponentBase
{
    public IReadOnlyList<string> Items { get; set; } = [];
    public bool Insets { get; set; }
    public List<int> Counts { get; } = [];
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Panel");
        builder.AddAttribute(1, "style", "position: absolute; bottom: 20px; width: 100px;");
        builder.OpenComponent<ItemsTransition<string>>(10);
        builder.AddComponentParameter(11, nameof(ItemsTransition<string>.Items), Items);
        builder.AddComponentParameter(12, nameof(ItemsTransition<string>.ItemKey), (Func<string, object>)(item => item));
        builder.AddComponentParameter(13, nameof(ItemsTransition<string>.PreserveExtentDuringExit), true);
        builder.AddComponentParameter(14, nameof(ItemsTransition<string>.RenderedCountChanged), EventCallback.Factory.Create<int>(this, value => Counts.Add(value)));
        builder.AddComponentParameter(15, nameof(ItemsTransition<string>.ItemTemplate), (RenderFragment<string>)(item => content =>
        {
            content.OpenElement(0, "Row"); content.AddAttribute(1, "style", "height: 40px;"); content.AddContent(2, item); content.CloseElement();
        }));
        if (Insets) builder.AddComponentParameter(16, nameof(Control.Style), "padding: 8px; border: 4px solid black;");
        builder.CloseComponent(); builder.CloseElement();
    }
}

[Collection("Controls")]
public sealed class ToastRetentionTests : IDisposable
{
    private readonly ControlHarness h = new();
    public void Dispose() => h.Dispose();
    private void Frame() { for (var frame = 0; frame < 3; frame++) h.Frame(); }
    private ToastRetentionProbe Show()
    {
        h.Scene.Transitions.ReducedMotion = false;
        var result = NssCompiler.Compile("retained-toasts", "ToastHostRegion > ItemsTransition, ItemsTransition { overlay-exit-duration: 10s; }");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics)); h.Scene.Styles.Application = result.StyleSheet;
        h.Render<ToastRetentionProbe>(); Frame(); return h.Component<ToastRetentionProbe>();
    }
    private ElementNode[] Current() => h.All("Toast").Where(element => element.ParentElement?.Attributes.GetValueOrDefault("exiting") is not true).ToArray();
    private static ElementNode Part(ElementNode toast, string name) => toast.Elements().SelectMany(element => element.Elements().Prepend(element)).First(element => element.Name == name);

    [Fact]
    public void Queue_pause_expiry_promotion_and_announcement_stay_authoritative_while_old_visuals_exit()
    {
        var probe = Show();
        var first = probe.Queue.Show(new ToastOptions { Message = "First", Duration = TimeSpan.FromMilliseconds(4200), ActionText = "Undo" });
        probe.Queue.Show(new ToastOptions { Message = "Second", Duration = null }); Frame();
        var old = Assert.Single(Current()); var action = Part(old, "ToastAction");
        Assert.Equal(1, probe.Queue.PendingCount);
        Assert.Equal("First", h.One("ToastHostAnnouncement").Text());
        probe.Clock.Advance(TimeSpan.FromMilliseconds(1000)); Frame();
        var remaining = first.Remaining(probe.Clock.GetUtcNow());
        var box = h.Bounds(old);
        h.Input.PointerMoved(new(box.X + box.Width / 2, box.Y + box.Height / 2), KeyModifiers.None); Frame();
        h.Input.Focus(action, visible: true); Frame();
        h.Input.PointerMoved(new(0, 0), KeyModifiers.None); Frame();
        Assert.True(first.IsPaused);
        probe.Clock.Advance(TimeSpan.FromSeconds(6)); Frame();
        Assert.Equal(remaining, first.Remaining(probe.Clock.GetUtcNow()));
        Assert.Equal("First", Part(Assert.Single(Current()), "ToastMessage").Text());
        h.Input.Focus(h.All("Button").Single(element => element.Id == "outside"), visible: true); Frame();
        Assert.False(first.IsPaused);
        probe.Clock.Advance(TimeSpan.FromMilliseconds(3199)); Frame(); Assert.Same(first, Assert.Single(probe.Queue.Visible));
        probe.Clock.Advance(TimeSpan.FromMilliseconds(1)); Frame();
        Assert.Equal("Second", Assert.Single(probe.Queue.Visible).Options.Message);
        Assert.Equal("Second", h.One("ToastHostAnnouncement").Text());
        Assert.Equal("Second", Part(Assert.Single(Current()), "ToastMessage").Text());
        Assert.False(old.IsRemoved); Assert.True(old.IsDisabled);
        Assert.True(old.ParentElement!.Attributes.GetValueOrDefault("exiting") is true);
        Assert.False(h.Input.Activate(action));
    }

    [Fact]
    public void An_action_runs_once_and_keeps_application_focus_while_the_notification_exits()
    {
        var probe = Show(); var actions = 0;
        var outside = h.All("Button").Single(element => element.Id == "outside");
        probe.Queue.Show(new ToastOptions
        {
            Message = "Saved", Duration = null, ActionText = "Undo",
            Action = () => { actions++; h.Input.Focus(outside, visible: true); return Task.CompletedTask; },
        }); Frame();
        var toast = Assert.Single(Current()); var action = Part(toast, "ToastAction");
        h.Input.Focus(action, visible: true); Frame();
        Assert.True(h.Input.Activate(action)); Frame();
        Assert.Equal(1, actions); Assert.Empty(probe.Queue.Visible); Assert.Same(outside, h.Focused);
        Assert.False(toast.IsRemoved); Assert.True(toast.IsDisabled);
        Assert.False(h.Input.Activate(action)); Assert.Equal(1, actions);
    }

    [Fact]
    public void Rapid_churn_is_bounded_and_reduced_motion_finishes_the_last_exit_without_an_empty_overlay()
    {
        var probe = Show();
        Assert.DoesNotContain(h.Scene.Overlays, element => element.Name == "ToastHostRegion");
        for (var index = 0; index < 40; index++)
        {
            var entry = probe.Queue.Show(new ToastOptions { Message = "Notice " + index, Duration = null }); Frame();
            Assert.True(probe.Queue.Dismiss(entry)); Frame();
            Assert.InRange(h.All("ItemsTransitionItem").Count(), 1, 32);
        }
        Assert.Equal(32, h.All("ItemsTransitionItem").Count());
        Assert.Empty(probe.Queue.Visible); Assert.Empty(Current());
        Assert.Contains(h.Scene.Overlays, element => element.Name == "ToastHostRegion");
        h.Scene.Transitions.ReducedMotion = true; Frame();
        Assert.Empty(h.All("Toast")); Assert.Empty(h.All("ItemsTransitionItem"));
        Assert.DoesNotContain(h.Scene.Overlays, element => element.Name == "ToastHostRegion");
        Assert.True(h.One("ToastHostRegion").Attributes.GetValueOrDefault("semantic-hidden") is true);
        Assert.Equal(Display.None, h.Style(h.One("ToastHostRegion")).Display);
    }

    [Fact]
    public void Disposing_the_host_cancels_countdown_and_retained_lifetimes_and_detaches_the_queue()
    {
        var probe = Show();
        var old = probe.Queue.Show(new ToastOptions { Message = "Old", Duration = null }); Frame();
        probe.Queue.Dismiss(old); Frame();
        probe.Queue.Show(new ToastOptions { Message = "Timed", Duration = TimeSpan.FromMilliseconds(4200) }); Frame();
        Assert.True(probe.Clock.ActiveTimers > 0);
        probe.ShowHost = false; probe.Refresh(); Frame();
        Assert.Equal(0, probe.Clock.ActiveTimers); Assert.Empty(h.All("Toast"));
        probe.Queue.Show("After removal"); probe.Clock.Advance(TimeSpan.FromSeconds(20)); Frame();
        Assert.Equal(0, probe.Clock.ActiveTimers);
        Assert.DoesNotContain(h.Scene.Overlays, element => element.Name == "ToastHostRegion");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Preserved_extent_and_rendered_counts_include_outgoing_items_until_their_real_exit_completes(bool insets)
    {
        h.Scene.Transitions.ReducedMotion = false;
        var result = NssCompiler.Compile("extent", "ItemsTransition { overlay-exit-duration: 10s; }");
        Assert.True(result.Success); h.Scene.Styles.Application = result.StyleSheet;
        h.Render<ItemsExtentProbe>(); var probe = h.Component<ItemsExtentProbe>(); probe.Insets = insets; probe.Refresh(); Frame();
        var extra = insets ? 24 : 0;
        Assert.Equal(new[] { 0 }, probe.Counts);
        probe.Items = ["A", "B"]; probe.Refresh(); Frame();
        Assert.Equal(new[] { 0, 2 }, probe.Counts);
        var top = h.Bounds(h.One("ItemsTransition")).Y;
        var outgoing = h.All("ItemsTransitionItem").Last();
        var outgoingBox = h.Bounds(outgoing);
        probe.Items = ["A"]; probe.Refresh(); Frame();
        Assert.Equal(80 + extra, h.Bounds(h.One("ItemsTransition")).Height);
        Assert.Equal(top, h.Bounds(h.One("ItemsTransition")).Y);
        Assert.Equal(outgoingBox, h.Bounds(outgoing));
        Assert.Equal(new[] { 0, 2 }, probe.Counts);
        h.Scene.Transitions.ReducedMotion = true; Frame();
        Assert.Equal(40 + extra, h.Bounds(h.One("ItemsTransition")).Height);
        Assert.Equal(new[] { 0, 2, 1 }, probe.Counts);
        probe.Items = []; probe.Refresh(); Frame();
        Assert.Equal(extra, h.Bounds(h.One("ItemsTransition")).Height);
        Assert.Equal(new[] { 0, 2, 1, 0 }, probe.Counts);
    }
}
