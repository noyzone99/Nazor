using System.Globalization;
using Nazor.Controls.Tests.Navigation.Fixtures;
using Nazor.Graphics;
using Nazor.Input;

namespace Nazor.Controls.Tests.Navigation;

public sealed class FlipViewEntry(string name)
{
    public string Name { get; } = name;
    public string Text { get; set; } = "Editable content";
}

public sealed class FlipViewLifetime : ComponentBase, IDisposable
{
    [Parameter] public string Name { get; set; } = "";
    [Parameter] public Action<string>? Created { get; set; }
    [Parameter] public Action<string>? Disposed { get; set; }

    protected override void OnInitialized() => Created?.Invoke(Name);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "TextBlock");
        builder.AddAttribute(1, "class", "title");
        builder.AddContent(2, Name);
        builder.CloseElement();
    }

    public void Dispose() => Disposed?.Invoke(Name);
}

[Collection("Controls")]
public sealed class FlipViewTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    private FlipViewProbe Render(Action<FlipViewProbe>? setup = null)
    {
        h.Render<FlipViewProbe>();
        var probe = h.Component<FlipViewProbe>();
        if (setup is not null)
        {
            setup(probe);
            probe.Refresh();
            h.Frame();
        }
        return probe;
    }

    [Fact]
    public void Only_the_current_template_is_realized_and_position_is_exposed()
    {
        var probe = Render();
        Assert.Equal(["One"], probe.Created);
        Assert.Empty(probe.Disposed);
        var item = h.One("FlipViewItem");
        Assert.Equal("group", item.Attributes["role"]);
        Assert.Equal(1, item.Attributes["position-in-set"]);
        Assert.Equal(3, item.Attributes["size-of-set"]);
        Assert.Equal("Slide 1 of 3", item.Attributes["accessible-name"]);
        Assert.Equal("polite", h.One("FlipViewStatus").Attributes["live"]);
        Assert.Equal(true, h.One("FlipViewPrevious").Attributes["disabled"]);
        Assert.False(h.One("FlipViewNext").IsDisabled);

        h.Click(h.One("FlipViewNext"));
        Assert.Equal(1, probe.Index);
        Assert.Equal([1], probe.Changes);
        Assert.Equal(["One", "Two"], probe.Created);
        Assert.Equal(["One"], probe.Disposed);
        Assert.Single(h.All("FlipViewItem"));
        Assert.Equal(2, h.One("FlipViewItem").Attributes["position-in-set"]);
    }

    [Fact]
    public void Root_keyboard_navigation_is_bounded_and_does_not_wrap()
    {
        var probe = Render();
        h.Focus(h.One("FlipView"));
        h.Press("ArrowRight");
        Assert.Equal(1, probe.Index);
        h.Press("End");
        Assert.Equal(2, probe.Index);
        h.Press("ArrowRight");
        Assert.Equal([1, 2], probe.Changes);
        h.Press("Home");
        Assert.Equal(0, probe.Index);
        h.Press("ArrowLeft");
        Assert.Equal([1, 2, 0], probe.Changes);
        h.Press("ArrowRight", KeyModifiers.Control);
        Assert.Equal(0, probe.Index);
    }

    [Fact]
    public void Descendant_editors_actions_and_handled_keys_do_not_navigate()
    {
        var probe = Render();
        h.Focus(h.One("TextFieldInput"));
        h.Press("End");
        h.Press("ArrowLeft");
        h.Type("!");
        Assert.Equal(0, probe.Index);
        Assert.Contains("!", probe.Entries[0].Text);
        h.Click(h.ByClass("action"));
        Assert.Equal(1, probe.Actions);
        h.Press("ArrowRight");
        Assert.Equal(0, probe.Index);
        h.Focus(h.ByClass("handled"));
        h.Press("End");
        Assert.Equal(0, probe.Index);
        Assert.Empty(probe.Changes);
    }

    [Fact]
    public async Task Focus_is_recovered_after_an_interactive_item_is_replaced_or_a_button_reaches_the_bound()
    {
        var probe = Render();
        h.Focus(h.One("TextFieldInput"));
        await probe.View.SelectAsync(1);
        h.Frame();
        Assert.Same(h.One("FlipView"), h.Focused);
        h.Click(h.One("FlipViewNext"));
        Assert.Equal(2, probe.Index);
        Assert.Same(h.One("FlipView"), h.Focused);
        Assert.Equal(true, h.One("FlipViewNext").Attributes["disabled"]);
    }

    [Fact]
    public void Collection_changes_clamp_selection_and_empty_collections_have_no_realized_item()
    {
        var probe = Render(p => p.Index = 20);
        Assert.Equal(2, probe.View.CurrentIndex);
        Assert.Empty(probe.Changes);
        probe.Entries.RemoveAt(2);
        probe.Refresh();
        h.Frame();
        Assert.Equal(1, probe.View.CurrentIndex);
        probe.Entries.Clear();
        probe.Refresh();
        h.Frame();
        Assert.Equal(-1, probe.View.CurrentIndex);
        Assert.Empty(h.All("FlipViewItem"));
        Assert.Equal(true, h.One("FlipViewPrevious").Attributes["disabled"]);
        Assert.Equal(true, h.One("FlipViewNext").Attributes["disabled"]);
        probe.Entries.Add(new("New"));
        probe.Refresh();
        h.Frame();
        Assert.Equal(0, probe.View.CurrentIndex);
        Assert.Equal("New", h.ByClass("title").Text());
        Assert.Empty(probe.Changes);
    }

    [Fact]
    public void Reordering_items_follows_the_index_and_replaces_the_selected_template()
    {
        var probe = Render();
        probe.Entries.Reverse();
        probe.Refresh();
        h.Frame();
        Assert.Equal(0, probe.View.CurrentIndex);
        Assert.Equal("Three", h.ByClass("title").Text());
        Assert.Equal(["One"], probe.Disposed);
    }

    [Fact]
    public async Task Disabled_views_and_invalid_requests_do_not_navigate()
    {
        var probe = Render(p => p.Disabled = true);
        await probe.View.NextAsync();
        h.Frame();
        Assert.Equal(0, probe.View.CurrentIndex);
        Assert.Empty(probe.Changes);
        probe.Disabled = false;
        probe.Refresh();
        h.Frame();
        await probe.View.SelectAsync(-1);
        await probe.View.SelectAsync(3);
        h.Frame();
        Assert.Equal(0, probe.View.CurrentIndex);
        Assert.Empty(probe.Changes);
    }

    [Fact]
    public void Horizontal_swipes_navigate_while_short_vertical_and_disabled_swipes_do_not()
    {
        var probe = Render();
        Swipe(-70, 3);
        Assert.Equal(1, probe.Index);
        Swipe(70, 3);
        Assert.Equal(0, probe.Index);
        Swipe(-20, 0);
        Swipe(-40, 80);
        Assert.Equal([1, 0], probe.Changes);
        probe.SwipeEnabled = false;
        probe.Refresh();
        h.Frame();
        Swipe(-70, 0);
        Assert.Equal(0, probe.Index);
    }

    [Fact]
    public void Swipes_started_on_interactive_content_and_canceled_capture_do_not_select_an_item()
    {
        var probe = Render();
        var action = h.Bounds(h.ByClass("action"));
        var start = new Point(action.X + action.Width / 2, action.Y + action.Height / 2);
        h.Input.PointerPressed(start, PointerButton.Primary, KeyModifiers.None);
        h.Input.PointerMoved(new Point(start.X - 70, start.Y), KeyModifiers.None);
        h.Input.PointerReleased(new Point(start.X - 70, start.Y), PointerButton.Primary, KeyModifiers.None);
        h.Frame();
        Assert.Equal(0, probe.Index);
        start = SwipeStart();
        h.Input.PointerPressed(start, PointerButton.Primary, KeyModifiers.None);
        var capture = new ElementReference(h.One("FlipViewViewport"));
        Assert.True(capture.IsPointerCaptured);
        h.Input.PointerCanceled();
        Assert.False(capture.IsPointerCaptured);
        h.Input.PointerReleased(new Point(start.X - 70, start.Y), PointerButton.Primary, KeyModifiers.None);
        h.Frame();
        Assert.Equal(0, probe.Index);
        Swipe(-70, 0);
        Assert.Equal(1, probe.Index);
    }

    [Fact]
    public void Navigation_and_position_labels_use_the_current_ui_culture()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("it-IT");
            Render();
            Assert.Equal("Precedente", h.One("FlipViewPrevious").Attributes["accessible-name"]);
            Assert.Equal("Successivo", h.One("FlipViewNext").Attributes["accessible-name"]);
            Assert.Equal("Diapositiva 1 di 3", h.One("FlipViewItem").Attributes["accessible-name"]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    private Point SwipeStart()
    {
        var bounds = h.Bounds(h.One("FlipViewViewport"));
        return new Point(bounds.X + bounds.Width - 45, bounds.Y + bounds.Height - 12);
    }

    private void Swipe(float horizontal, float vertical)
    {
        var start = SwipeStart();
        h.Input.PointerPressed(start, PointerButton.Primary, KeyModifiers.None);
        h.Input.PointerMoved(new Point(start.X + horizontal, start.Y + vertical), KeyModifiers.None);
        h.Input.PointerReleased(new Point(start.X + horizontal, start.Y + vertical), PointerButton.Primary, KeyModifiers.None);
        h.Frame();
    }
}
