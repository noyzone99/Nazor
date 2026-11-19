using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed record LostCapture(string Owner, ElementNode? Capture, PointerEventArgs Arguments);

public sealed class PointerCaptureLifecyclePage : ComponentBase
{
    public bool Disabled { get; set; }
    public bool ExitingModal { get; set; }
    public List<LostCapture> Lost { get; } = [];
    public int ParentNotifications { get; private set; }
    public Func<ElementNode?> CurrentCapture { get; set; } = () => null;
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Surface");
        builder.AddAttribute(1, "onlostpointercapture", EventCallback.Factory.Create<PointerEventArgs>(this, () => ParentNotifications++));
        foreach (var name in new[] { "Target", "Other" })
        {
            builder.OpenElement(10, name);
            builder.SetKey(name);
            builder.AddAttribute(11, "style", "width: 100px; height: 100px;");
            builder.AddAttribute(12, "disabled", name == "Target" && Disabled);
            builder.AddAttribute(13, "onlostpointercapture", EventCallback.Factory.Create<PointerEventArgs>(this,
                e => Lost.Add(new(name, CurrentCapture(), e))));
            builder.CloseElement();
        }
        if (ExitingModal)
        {
            builder.OpenElement(20, "ExitingModal");
            builder.AddAttribute(21, "overlay", true);
            builder.AddAttribute(22, "modal", true);
            builder.AddAttribute(23, "exiting", true);
            builder.AddAttribute(24, "style", "width: 40px; height: 40px;");
            builder.CloseElement();
        }
        builder.CloseElement();
    }
}

public sealed class PointerCaptureLifecycleTests
{
    [Fact]
    public void Capture_transfer_and_release_notify_each_outgoing_owner_once_after_ownership_changes()
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        var root = harness.Host.Render<PointerCaptureLifecyclePage>();
        var page = (PointerCaptureLifecyclePage)root.Component;
        page.CurrentCapture = () => input.CapturedPointer;
        harness.Render(300, 300, 1);
        var target = root.Elements("Target").Single();
        var other = root.Elements("Other").Single();
        input.PointerPressed(new Point(20, 20), PointerButton.Primary, KeyModifiers.Shift);
        Assert.True(input.CapturePointer(target));
        Assert.True(input.CapturePointer(target));
        Assert.Empty(page.Lost);
        Assert.True(input.CapturePointer(other));
        var transferred = Assert.Single(page.Lost);
        Assert.Equal("Target", transferred.Owner);
        Assert.Same(other, transferred.Capture);
        Assert.Same(target, transferred.Arguments.OriginalTarget);
        Assert.Equal((20d, 20d, PointerButton.None, KeyModifiers.Shift),
            (transferred.Arguments.X, transferred.Arguments.Y, transferred.Arguments.Button, transferred.Arguments.Modifiers));
        input.ReleasePointerCapture(other);
        Assert.Equal(2, page.Lost.Count);
        Assert.Equal("Other", page.Lost[1].Owner);
        Assert.Null(page.Lost[1].Capture);
        input.ReleasePointerCapture(other);
        input.PointerCanceled();
        Assert.Equal(2, page.Lost.Count);
        Assert.Equal(2, page.ParentNotifications);
    }

    [Theory]
    [InlineData("host")]
    [InlineData("disabled")]
    [InlineData("modal-exit")]
    public void Capture_cleanup_reaches_the_live_owner_when_host_canceled_disabled_or_its_modal_scope_exits(string reason)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        var root = harness.Host.Render<PointerCaptureLifecyclePage>();
        var page = (PointerCaptureLifecyclePage)root.Component;
        page.CurrentCapture = () => input.CapturedPointer;
        harness.Render(300, 300, 1);
        var target = root.Elements("Target").Single();
        input.PointerPressed(new Point(20, 20), PointerButton.Primary, KeyModifiers.None);
        Assert.True(input.CapturePointer(target));
        page.Disabled = reason == "disabled";
        page.ExitingModal = reason == "modal-exit";
        page.Refresh();
        harness.Host.RunPending();
        harness.Render(300, 300, 1);
        if (reason == "modal-exit") Assert.Single(harness.Scene.Overlays);
        input.PointerCanceled(clearHover: false);
        var lost = Assert.Single(page.Lost);
        Assert.Equal("Target", lost.Owner);
        Assert.Same(target, lost.Arguments.OriginalTarget);
        Assert.Null(lost.Capture);
        Assert.Equal(1, page.ParentNotifications);
        Assert.Null(input.CapturedPointer);
    }
}

public sealed class DialCaptureLifecyclePage : ComponentBase
{
    public Dial Dial { get; private set; } = null!;
    public bool Disabled { get; set; }
    public List<string> Events { get; } = [];
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Dial>(0);
        builder.AddComponentParameter(1, nameof(Dial.Value), 50d);
        builder.AddComponentParameter(2, nameof(Dial.Disabled), Disabled);
        builder.AddComponentParameter(3, nameof(Dial.ValueChanged), EventCallback.Factory.Create<double>(this, () => Events.Add("value")));
        builder.AddComponentParameter(4, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, () => Events.Add("input")));
        builder.AddComponentParameter(5, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, () => Events.Add("change")));
        builder.AddComponentReferenceCapture(6, component => Dial = (Dial)component);
        builder.CloseComponent();
        builder.OpenElement(10, "Other");
        builder.AddAttribute(11, "style", "width: 100px; height: 100px;");
        builder.CloseElement();
    }
}

public sealed class DialCaptureLifecycleTests
{
    [Theory]
    [InlineData("host")]
    [InlineData("transfer")]
    [InlineData("disabled")]
    public void Losing_capture_cancels_the_dial_drag_without_a_commit_or_stale_move(string reason)
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        var root = harness.Host.Render<DialCaptureLifecyclePage>();
        var page = (DialCaptureLifecyclePage)root.Component;
        harness.Render(300, 300, 1);
        input.PointerPressed(new Point(32, 32), PointerButton.Primary, KeyModifiers.None);
        harness.Host.RunPending();
        harness.Render(300, 300, 1);
        Assert.True(page.Dial.IsDragging);
        input.PointerMoved(new Point(42, 32), KeyModifiers.None);
        harness.Host.RunPending();
        harness.Render(300, 300, 1);
        Assert.Equal(["value", "input"], page.Events);
        var value = page.Dial.CurrentValue;

        if (reason == "transfer") Assert.True(input.CapturePointer(root.Elements("Other").Single()));
        else if (reason == "disabled") { page.Disabled = true; page.Refresh(); }
        else input.PointerCanceled(clearHover: false);
        harness.Host.RunPending();
        harness.Render(300, 300, 1);
        Assert.False(page.Dial.IsDragging);
        Assert.False(page.Dial.Element.IsPointerCaptured);
        input.PointerMoved(new Point(52, 32), KeyModifiers.None);
        input.PointerReleased(new Point(52, 32), PointerButton.Primary, KeyModifiers.None);
        harness.Host.RunPending();
        harness.Render(300, 300, 1);
        Assert.Equal(value, page.Dial.CurrentValue);
        Assert.Equal(["value", "input"], page.Events);
    }
}
