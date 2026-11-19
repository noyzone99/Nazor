using Nazor.Controls.Tests.Foundation.Fixtures;

namespace Nazor.Controls.Tests.Foundation;

[Collection("Controls")]
public sealed class AnchorInteractionTests : IDisposable
{
    private readonly ControlHarness h = new();
    public void Dispose() => h.Dispose();
    private AnchorInteractionProbe Render()
    {
        h.Render<AnchorInteractionProbe>();
        return h.Component<AnchorInteractionProbe>();
    }

    [Fact]
    public void Query_clear_and_editor_presses_work_on_the_first_press_with_suggestions_open()
    {
        var probe = Render();
        h.Focus(h.One("SearchFieldInput")); h.Type("Ro");
        h.Click(h.One("SearchFieldInput"));
        Assert.True(probe.Search.Opened);
        h.Click(h.One("SearchFieldMark"));
        Assert.Equal(["Ro"], probe.Queries);
        Assert.False(probe.Search.Opened);
        h.Type("m");
        Assert.True(probe.Search.Opened);
        h.Click(h.One("SearchFieldClear"));
        Assert.Equal("", probe.Query);
        Assert.False(probe.Search.Opened);
        Assert.Same(h.One("SearchFieldInput"), h.Focused);
    }

    [Fact]
    public void A_combo_chevron_toggles_once_and_an_ordinary_select_retains_default_light_dismissal()
    {
        var probe = Render();
        h.Click(h.One("ComboBoxChevron"));
        Assert.True(probe.Combo.Opened);
        h.Click(h.One("ComboBoxChevron"));
        Assert.False(probe.Combo.Opened);
        h.Click(h.One("ComboBoxChevron"));
        Assert.True(probe.Combo.Opened);
        Assert.Single(h.Scene.Overlays);
        Assert.Null(probe.Choice);
        h.Click(h.ByClass("outside"));
        Assert.False(probe.Combo.Opened);
        Assert.Equal(0, probe.OutsideClicks);
        h.Click(h.One("Select"));
        Assert.True(probe.SelectOpen);
        h.Click(h.One("Select"));
        Assert.False(probe.SelectOpen);
    }

    [Fact]
    public void Anchor_interaction_does_not_bypass_an_active_modal_scope()
    {
        var probe = Render();
        h.Focus(h.One("SearchFieldInput")); h.Type("Ro");
        probe.Modal = true; probe.Refresh(); h.Frame();
        h.Click(h.One("SearchFieldMark"));
        Assert.True(probe.Modal);
        Assert.Empty(probe.Queries);
        Assert.Equal(0, probe.OutsideClicks);
    }
}
