using Nazor.Input;

namespace Nazor.Controls.Tests.Feedback;

public sealed class BadgeContentProbe : ComponentBase
{
    public int? Count { get; set; } = 3;
    public int Max { get; set; } = 99;
    public bool ShowZero { get; set; }
    public bool Dot { get; set; }
    public bool Custom { get; set; }
    public IFontSymbol? Symbol { get; set; } = new FontSymbol("Fira Sans", "!");
    public string? Text { get; set; } = "Fallback";
    public string? Name { get; set; }
    public string Value { get; set; } = "Draft";
    public int Actions { get; private set; }
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Badge>(0);
        builder.AddComponentParameter(1, nameof(Badge.Count), Count);
        builder.AddComponentParameter(2, nameof(Badge.Max), Max);
        builder.AddComponentParameter(3, nameof(Badge.ShowZero), ShowZero);
        builder.AddComponentParameter(4, nameof(Badge.Dot), Dot);
        builder.AddComponentParameter(5, nameof(Badge.Icon), Symbol);
        builder.AddComponentParameter(6, nameof(Badge.Text), Text);
        builder.AddComponentParameter(7, nameof(Badge.AccessibleName), Name);
        builder.AddComponentParameter(8, nameof(Badge.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<TextField>(0);
            content.AddComponentParameter(1, nameof(TextField.Value), Value);
            content.AddComponentParameter(2, nameof(TextField.ValueChanged), EventCallback.Factory.Create<string>(this, value => Value = value));
            content.CloseComponent();
        }));
        builder.AddComponentParameter(9, nameof(Badge.IndicatorContent), Custom ? (RenderFragment)(content =>
        {
            content.OpenComponent<Button>(0);
            content.AddComponentParameter(1, nameof(Button.Class), "custom-action");
            content.AddComponentParameter(2, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => Actions++));
            content.AddComponentParameter(3, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Custom")));
            content.CloseComponent();
        }) : null);
        builder.CloseComponent();
        builder.OpenComponent<Button>(10);
        builder.AddComponentParameter(11, nameof(Button.Class), "outside");
        builder.AddComponentParameter(12, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Outside")));
        builder.CloseComponent();
    }
}

[Collection("Controls")]
public sealed class BadgeContentTests : IDisposable
{
    private readonly ControlHarness h = new();
    public void Dispose() => h.Dispose();
    private BadgeContentProbe Show() { h.Render<BadgeContentProbe>(); return h.Component<BadgeContentProbe>(); }
    private void Refresh(BadgeContentProbe probe) { probe.Refresh(); h.Frame(); }

    [Fact]
    public void Dot_count_custom_icon_and_text_have_explicit_precedence()
    {
        var probe = Show(); probe.Custom = true; probe.Dot = true; Refresh(probe);
        Assert.Equal("dot", h.One("BadgeIndicator").Attributes["content-kind"]);
        Assert.Empty(h.All("BadgeValue")); Assert.Empty(h.All("BadgeIcon")); Assert.Empty(h.All("BadgeIndicatorContent"));
        probe.Dot = false; Refresh(probe);
        Assert.Equal("count", h.One("BadgeIndicator").Attributes["content-kind"]);
        Assert.Equal("3", h.One("BadgeValue").Text());
        probe.Count = null; Refresh(probe);
        Assert.Equal("content", h.One("BadgeIndicator").Attributes["content-kind"]);
        Assert.Single(h.All("BadgeIndicatorContent")); Assert.Empty(h.All("BadgeIcon"));
        probe.Custom = false; Refresh(probe);
        Assert.Equal("icon", h.One("BadgeIndicator").Attributes["content-kind"]);
        Assert.Equal("!", h.One("Icon").Text());
        Assert.True(h.One("BadgeIndicator").Attributes.GetValueOrDefault("icon") is true);
        probe.Symbol = null; Refresh(probe);
        Assert.Equal("text", h.One("BadgeIndicator").Attributes["content-kind"]);
        Assert.Equal("Fallback", h.One("BadgeIndicator").Text());
        probe.Text = null; Refresh(probe);
        Assert.Empty(h.All("BadgeIndicator")); Assert.Single(h.All("TextFieldInput"));
    }

    [Fact]
    public void A_count_key_replaces_only_the_value_including_capped_changes_and_preserves_the_decorated_editor()
    {
        var probe = Show(); var editor = h.One("TextFieldInput"); var indicator = h.One("BadgeIndicator"); var first = h.One("BadgeValue");
        Assert.False(first.Attributes.GetValueOrDefault("changed") is true);
        h.Input.Focus(editor, visible: true); h.Press("End"); h.Type("!");
        Assert.Equal("Draft!", probe.Value);
        probe.Count = 4; Refresh(probe);
        var changed = h.One("BadgeValue"); Assert.NotSame(first, changed); Assert.True(first.IsRemoved);
        Assert.True(changed.Attributes.GetValueOrDefault("changed") is true);
        Assert.Same(editor, h.One("TextFieldInput")); Assert.Same(editor, h.Input.Focused); Assert.Same(indicator, h.One("BadgeIndicator"));
        Refresh(probe); Assert.Same(changed, h.One("BadgeValue")); Assert.True(changed.Attributes.GetValueOrDefault("changed") is true);
        probe.Count = 100; Refresh(probe); var capped = h.One("BadgeValue"); Assert.Equal("99+", capped.Text());
        probe.Count = 101; Refresh(probe); Assert.NotSame(capped, h.One("BadgeValue")); Assert.Equal("99+", h.One("BadgeValue").Text());
        Assert.Same(editor, h.Input.Focused); Assert.Equal("Draft!", probe.Value);
    }

    [Fact]
    public void Custom_indicator_content_rejects_pointer_activation_and_is_skipped_by_real_Tab_navigation()
    {
        var probe = Show(); probe.Count = null; probe.Custom = true; probe.Name = "Review required"; Refresh(probe);
        var wrapper = h.One("BadgeIndicatorContent"); var action = h.ByClass("custom-action");
        Assert.True(wrapper.Attributes.GetValueOrDefault("semantic-hidden") is true);
        Assert.True(action.IsDisabled); Assert.False(h.Input.Activate(action)); h.Click(action); Assert.Equal(0, probe.Actions);
        h.Input.Focus(h.One("TextFieldInput"), visible: true); h.Press("Tab");
        Assert.Same(h.ByClass("outside"), h.Input.Focused);
        Assert.Equal("Review required", h.One("BadgeIndicator").Attributes["accessible-name"]);
    }

    [Fact]
    public void The_count_cap_does_not_cap_the_accessible_count_and_zero_still_takes_precedence()
    {
        var probe = Show(); probe.Count = 150; Refresh(probe);
        Assert.Equal("99+", h.One("BadgeValue").Text());
        Assert.Equal("150 new", h.One("BadgeIndicator").Attributes["accessible-name"]);
        probe.Count = 0; Refresh(probe); Assert.Empty(h.All("BadgeIndicator"));
        probe.ShowZero = true; Refresh(probe); Assert.Equal("0", h.One("BadgeValue").Text());
        probe.Count = null; Refresh(probe); Assert.Equal("New activity", h.One("BadgeIndicator").Attributes["accessible-name"]);
        probe.Name = "Mentions"; Refresh(probe); Assert.Equal("Mentions", h.One("BadgeIndicator").Attributes["accessible-name"]);
    }
}
