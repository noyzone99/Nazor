using Nazor.Controls.Tests.Navigation.Fixtures;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Tests.Navigation;

[Collection("Controls")]
public sealed class PipsPagerTests : IDisposable
{
    private readonly ControlHarness h = new();
    private PipsPagerProbe Probe => h.Component<PipsPagerProbe>();
    public void Dispose() => h.Dispose();
    private void Show(Action<PipsPagerProbe>? setup = null)
    {
        h.Render<PipsPagerProbe>(); setup?.Invoke(Probe); Probe.Refresh(); h.Frame();
    }
    private ElementNode Pip(int position) => h.All("PipsPagerPip").Single(element => element.Attributes["index"] is int value && value == position);
    private ElementNode Current => h.All("PipsPagerPip").Single(element => element.Attributes.GetValueOrDefault("current") is true);

    [Fact]
    public void Dots_have_one_Tab_stop_and_accessible_full_collection_positions()
    {
        Show();
        Assert.Equal("group", h.One("PipsPager").Attributes["role"]);
        Assert.Equal(5, h.All("PipsPagerPip").Count());
        Assert.Equal(0, Current.Attributes["tabindex"]);
        Assert.Single(h.All("PipsPagerPip"), element => element.Attributes["tabindex"] is 0);
        Assert.Equal("Page 1 of 12", Current.Attributes["accessible-name"]);
        Assert.Equal(1, Current.Attributes["position-in-set"]);
        Assert.Equal(12, Current.Attributes["size-of-set"]);
        Assert.True(h.One("PipsPagerPrevious").IsDisabled);
        Assert.False(h.One("PipsPagerNext").IsDisabled);
    }

    [Fact]
    public void Arrow_and_Home_End_navigation_shift_the_dot_window_and_preserve_focus()
    {
        Show(); h.Focus(Current); h.Press("End");
        Assert.Equal(11, Probe.Selected);
        Assert.Same(Pip(11), h.Focused);
        Assert.Equal(new[] { 7, 8, 9, 10, 11 }, h.All("PipsPagerPip").Select(element => (int)element.Attributes["index"]!));
        h.Press("ArrowLeft");
        Assert.Equal(10, Probe.Selected);
        Assert.Same(Pip(10), h.Focused);
        h.Press("Home");
        Assert.Equal(0, Probe.Selected);
        Assert.Same(Pip(0), h.Focused);
        Assert.Equal(new[] { 11, 10, 0 }, Probe.Changes);
    }

    [Fact]
    public void Click_and_step_actions_report_once_and_do_not_wrap_at_bounds()
    {
        Show(probe => probe.Count = 3);
        h.Click(Pip(1)); h.Click(Pip(1)); h.Click(h.One("PipsPagerNext")); h.Click(h.One("PipsPagerNext"));
        Assert.Equal(new[] { 1, 2 }, Probe.Changes);
        Assert.True(h.One("PipsPagerNext").IsDisabled);
        h.Click(h.One("PipsPagerPrevious"));
        Assert.Equal(new[] { 1, 2, 1 }, Probe.Changes);
    }

    [Fact]
    public void Empty_and_shrinking_collections_normalize_without_a_user_event_and_restore_hidden_dot_focus()
    {
        Show(probe => probe.Selected = 11); h.Focus(Current);
        Probe.Count = 2; Probe.Refresh(); h.Frame();
        Assert.Equal(1, Probe.Pager.CurrentIndex);
        Assert.Same(Current, h.Focused);
        Assert.Empty(Probe.Changes);
        Probe.Count = 0; Probe.Refresh(); h.Frame();
        Assert.Equal(-1, Probe.Pager.CurrentIndex);
        Assert.Empty(h.All("PipsPagerPip"));
        Assert.True(h.One("PipsPagerPrevious").IsDisabled);
        Assert.True(h.One("PipsPagerNext").IsDisabled);
        Assert.Empty(Probe.Changes);
    }

    [Fact]
    public void Very_large_counts_realize_only_the_configured_window_even_at_the_last_page()
    {
        Show(probe => { probe.Count = int.MaxValue; probe.Selected = int.MaxValue - 1; probe.MaxVisible = 7; });
        Assert.Equal(7, h.All("PipsPagerPip").Count());
        Assert.Equal(int.MaxValue, Current.Attributes["position-in-set"]);
        Assert.Equal(int.MaxValue, Current.Attributes["size-of-set"]);
        Assert.True(h.One("PipsPagerNext").IsDisabled);
    }

    [Fact]
    public void Vertical_navigation_uses_Up_Down_and_the_corresponding_arrow_glyphs()
    {
        Show(probe => probe.Orientation = Orientation.Vertical); h.Focus(Current);
        h.Press("ArrowRight"); Assert.Empty(Probe.Changes);
        h.Press("ArrowDown"); Assert.Equal(1, Probe.Selected);
        h.Press("ArrowUp"); Assert.Equal(0, Probe.Selected);
        Assert.Equal("chevron-up", h.One("PipsPagerPrevious").Attributes["glyph"]);
        Assert.Equal("chevron-down", h.One("PipsPagerNext").Attributes["glyph"]);
    }

    [Fact]
    public async Task Disabled_and_modified_key_requests_leave_the_value_unchanged()
    {
        Show(); h.Focus(Current); h.Press("ArrowRight", KeyModifiers.Control);
        Assert.Empty(Probe.Changes);
        Probe.Disabled = true; Probe.Refresh(); h.Frame();
        h.Click(Pip(1)); await Probe.Pager.SelectAsync(2); h.Frame();
        Assert.Empty(Probe.Changes);
        Assert.All(h.All("PipsPagerPip"), pip => Assert.True(pip.IsDisabled));
        Assert.Equal(0, Probe.Pager.CurrentIndex);
    }

    [Fact]
    public void Navigation_buttons_can_be_omitted_without_removing_keyboard_navigation()
    {
        Show(probe => probe.Arrows = false);
        Assert.Empty(h.All("PipsPagerPrevious")); Assert.Empty(h.All("PipsPagerNext"));
        h.Focus(Current); h.Press("ArrowRight");
        Assert.Equal(1, Probe.Selected);
    }
}
