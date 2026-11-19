using Nazor.Controls.Primitives;
using Nazor.Controls.Tests.Foundation.Fixtures;
using Nazor.Graphics;
using Nazor.Input;

namespace Nazor.Controls.Tests.Foundation;

[Collection("Controls")]
public sealed class OverlayTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    [Fact]
    public void A_popover_aligns_to_the_end_of_its_anchor_and_closes_outside()
    {
        h.Render<OverlayProbe>();
        var probe = h.Component<OverlayProbe>();
        h.Click(h.ByClass("anchor"));
        var surface = h.One("PopoverContainer");
        var anchor = h.Bounds(h.ByClass("anchor"));
        Assert.Equal(anchor.Bottom + 6, h.Bounds(surface).Y, 1);
        Assert.Equal(anchor.Right, h.Bounds(surface).Right, 1);
        h.Click(h.ByClass("outside"));
        Assert.False(probe.Popover);
        Assert.Equal(1, probe.Dismissed);
        Assert.Empty(h.All("PopoverContainer"));
    }

    [Fact]
    public void A_modal_dialog_traps_the_focus_shows_a_scrim_and_closes_with_escape()
    {
        h.Render<OverlayProbe>();
        var probe = h.Component<OverlayProbe>();
        probe.Dialog = true;
        RenderAgain(probe);
        Assert.Single(h.All("DialogContainerScrim"));
        var dialog = h.One("DialogContainer");
        Assert.Equal("dialog", dialog.Attributes["role"]);
        Assert.Equal("dialog-field", h.Focused?.ParentElement?.Class);
        h.Focus(h.ByClass("outside"));
        Assert.NotEqual("outside", h.Focused?.Class);
        h.Press("Escape");
        Assert.False(probe.Dialog);
        Assert.Empty(h.All("DialogContainer"));
        Assert.Empty(h.All("DialogContainerScrim"));
    }

    [Fact]
    public void An_edge_dialog_is_positioned_by_the_theme()
    {
        h.Render<OverlayProbe>();
        var probe = h.Component<OverlayProbe>();
        probe.Placement = DialogPlacement.End;
        probe.Dialog = true;
        RenderAgain(probe);
        var bounds = h.Bounds(h.One("DialogContainer"));
        Assert.Equal(640, bounds.Right, 1);
        Assert.Equal(0, bounds.Y, 1);
        Assert.Equal(480, bounds.Height, 1);
    }

    [Fact]
    public void A_tooltip_container_retains_its_description_and_opens_from_its_trigger()
    {
        h.Render<OverlayProbe>();
        var surface = h.One("TooltipContainer");
        var trigger = h.One("TooltipContainerTrigger");
        Assert.Equal(surface.Id, trigger.Attributes["tooltip-describes"]);
        Assert.Equal(false, surface.Attributes["overlay"]);
        var controller = (ITooltipController)trigger.Attributes["tooltip-controller"]!;
        h.Host.Renderer.Dispatcher.InvokeAsync(controller.OpenAsync);
        h.Frame();
        Assert.Equal(true, h.One("TooltipContainer").Attributes["overlay"]);
    }

    private void RenderAgain(OverlayProbe probe)
    {
        h.Host.Renderer.Dispatcher.InvokeAsync(probe.Refresh);
        h.Frame();
    }
}

[Collection("Controls")]
public sealed class PrimitiveTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    [Fact]
    public void A_collapsible_renders_its_content_only_while_open_and_publishes_its_height()
    {
        h.Render<PrimitiveProbe>();
        var probe = h.Component<PrimitiveProbe>();
        Assert.Empty(h.All("CollapsibleContent"));
        probe.Expanded = true;
        Refresh(probe);
        Assert.Single(h.All("CollapsibleContent"));
        h.Frame();
        Assert.Contains("--nz-collapsible-height", (string)h.ByClass("region").Attributes["style"]!);
    }

    [Fact]
    public void Presence_without_an_exit_duration_removes_the_content_at_once()
    {
        h.Render<PrimitiveProbe>();
        var probe = h.Component<PrimitiveProbe>();
        Assert.Single(h.Root.Elements(), e => e.Class == "present");
        probe.Shown = false;
        Refresh(probe);
        Assert.DoesNotContain(h.Root.Elements(), e => e.Class?.StartsWith("present") == true);
    }

    [Fact]
    public void A_roving_group_has_one_tab_stop_and_arrows_skip_disabled_items()
    {
        h.Render<PrimitiveProbe>();
        Assert.Equal(0, h.ByClass("r1").Attributes["tabindex"]);
        Assert.Equal(-1, h.ByClass("r3").Attributes["tabindex"]);
        h.Focus(h.ByClass("r1"));
        h.Press("ArrowRight");
        Assert.Equal("r3", h.Focused?.Class);
        Assert.Equal(0, h.ByClass("r3").Attributes["tabindex"]);
        Assert.Equal(-1, h.ByClass("r1").Attributes["tabindex"]);
        h.Press("Home");
        Assert.Equal("r1", h.Focused?.Class);
    }

    [Fact]
    public void A_multiple_listbox_toggles_with_control_and_extends_with_shift()
    {
        h.Render<PrimitiveProbe>();
        var probe = h.Component<PrimitiveProbe>();
        h.Click(h.ByClass("a"));
        h.Click(h.ByClass("c"), KeyModifiers.Shift);
        Assert.Equal(["a", "b", "c"], probe.Picked);
        h.Click(h.ByClass("b"), KeyModifiers.Control);
        Assert.Equal(["a", "c"], probe.Picked);
        Assert.Equal(false, h.ByClass("b").Attributes["selected"]);
        h.Focus(h.ByClass("a"));
        h.Press("a", KeyModifiers.Control);
        Assert.Equal(3, probe.Picked.Count);
    }

    [Fact]
    public void A_combobox_keeps_the_focus_in_the_text_while_choosing_a_suggestion()
    {
        h.Render<PrimitiveProbe>();
        var probe = h.Component<PrimitiveProbe>();
        var input = h.One("ComboboxInput");
        h.Focus(input);
        h.Type("Ap");
        Assert.Equal("Ap", probe.Query);
        Assert.Equal(2, h.All("ComboboxOption").Count());
        h.Press("ArrowDown");
        h.Press("ArrowDown");
        Assert.Equal(true, h.All("ComboboxOption").ElementAt(1).Attributes["current"]);
        Assert.Same(input, h.Focused);
        h.Press("Enter");
        Assert.Equal("Apricot", probe.Chosen);
        Assert.Equal("Apricot", probe.Query);
        Assert.Empty(h.All("ComboboxPopup"));
    }

    [Fact]
    public void Pressable_reports_clicks_and_keyboard_activation()
    {
        h.Render<PrimitiveProbe>();
        var probe = h.Component<PrimitiveProbe>();
        h.Click(h.ByClass("press"));
        h.Focus(h.ByClass("press"));
        h.Press("Enter");
        Assert.Equal(2, probe.Presses);
    }

    [Fact]
    public void A_virtualizer_realizes_only_the_visible_rows_and_follows_scrolling()
    {
        h.Render<PrimitiveProbe>();
        var viewport = h.ByClass("virtual");
        var realized = h.Root.Elements().Count(e => e.Name == "VirtualizerItem");
        Assert.InRange(realized, 5, 12);
        h.Input.ScrollTo(viewport, null, 20 * 5000);
        h.Frame();
        var first = h.Root.Elements().First(e => e.Name == "VirtualizerItem");
        Assert.InRange((int)first.Attributes["index"]!, 4990, 5000);
        Assert.InRange(h.Root.Elements().Count(e => e.Name == "VirtualizerItem"), 5, 12);
    }

    [Fact]
    public void An_overflow_container_hands_the_items_that_do_not_fit_to_its_overflow_template()
    {
        h.Render<PrimitiveProbe>();
        h.Frame();
        var more = h.Root.Elements().SingleOrDefault(e => e.Class == "more");
        Assert.NotNull(more);
        var visible = h.Root.Elements().Count(e => e.Name == "OverflowContainerItem");
        Assert.Equal($"+{7 - visible}", more!.Text());
        Assert.True(h.Bounds(h.One("OverflowContainerOverflow")).Right <= h.Bounds(h.ByClass("overflow-frame")).Right + 0.5);
    }

    [Fact]
    public void A_resize_handle_reports_clamped_sizes_from_the_keyboard_and_the_pointer()
    {
        h.Render<PrimitiveProbe>();
        var probe = h.Component<PrimitiveProbe>();
        h.Focus(h.ByClass("handle"));
        h.Press("ArrowRight");
        Assert.Equal(110, probe.Width);
        var handle = h.Bounds(h.ByClass("handle"));
        var start = new Point(handle.X + 1, handle.Y + 1);
        h.Input.PointerPressed(start, PointerButton.Primary, KeyModifiers.None);
        h.Input.PointerMoved(new Point(start.X + 500, start.Y), KeyModifiers.None);
        h.Input.PointerReleased(new Point(start.X + 500, start.Y), PointerButton.Primary, KeyModifiers.None);
        h.Frame();
        Assert.Equal(300, probe.Width);
    }

    private void Refresh(ComponentBase component)
    {
        h.Host.Renderer.Dispatcher.InvokeAsync(() => ((IRefreshable)component).Refresh());
        h.Frame();
    }
}
