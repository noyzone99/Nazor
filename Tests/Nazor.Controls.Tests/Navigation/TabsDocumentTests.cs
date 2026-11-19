using Nazor.Controls.Tests.Navigation.Fixtures;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Tests.Navigation;

[Collection("Controls")]
public sealed class TabsDocumentTests : IDisposable
{
    private readonly ControlHarness h = new(width: 800, height: 500);
    public void Dispose() => h.Dispose();
    private TabsDocumentProbe Probe => h.Component<TabsDocumentProbe>();

    private void Show(Action<TabsDocumentProbe>? configure = null)
    {
        h.Render<TabsDocumentProbe>();
        configure?.Invoke(Probe);
        Probe.Refresh();
        h.Frame();
        h.Frame();
    }

    private ElementNode Tab(string key) => h.ByClass("document-" + key);
    private ElementNode Editor(string key) => h.Root.Elements().Single(element => element.Id == "editor-" + key);
    private Point At(ElementNode element, double fraction = .3)
    {
        var box = h.Bounds(element);
        return new(box.X + (float)(box.Width * fraction), box.Y + box.Height / 2);
    }
    private void Down(Point point) { h.Input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None); h.Frame(); }
    private void Move(Point point) { h.Input.PointerMoved(point, KeyModifiers.None); h.Frame(); }
    private void Up(Point point) { h.Input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None); h.Frame(); }

    [Fact]
    public void Document_presentation_adds_actions_without_changing_tab_semantics()
    {
        Show();
        Assert.Equal("document", h.One("Tabs").Attributes["presentation"]);
        Assert.Equal("equal", h.One("Tabs").Attributes["width-mode"]);
        Assert.Equal("tablist", h.One("TabsList").Attributes["role"]);
        Assert.Equal("tabpanel", h.One("TabsPanel").Attributes["role"]);
        Assert.Equal("button", h.One("TabsAdd").Attributes["role"]);
        Assert.Equal("New tab", h.One("TabsAdd").Attributes["accessible-name"]);
        Assert.Equal(0, Tab("a").Attributes["tabindex"]);
        Assert.Equal(-1, Tab("b").Attributes["tabindex"]);
    }

    [Fact]
    public void Add_button_and_control_T_ask_the_owner_to_create_and_select_documents()
    {
        Show();
        h.Click(h.One("TabsAdd"));
        Assert.Equal("new-1", Probe.SelectedKey);
        h.Focus(Tab("new-1"));
        h.Press("t", KeyModifiers.Control);
        Assert.Equal(2, Probe.Adds);
        Assert.Equal("new-2", Probe.SelectedKey);
        Assert.Equal(4, Probe.SelectedIndex);
    }

    [Fact]
    public void Missing_add_and_close_handlers_disable_actions_and_leave_shortcuts_unhandled()
    {
        Show(probe => { probe.AddEnabled = false; probe.CloseEnabled = false; });
        Assert.True(h.One("TabsAdd").IsDisabled);
        Assert.All(h.All("TabClose"), close => Assert.True(close.IsDisabled));
        h.Focus(Tab("a"));
        Assert.False(h.Input.KeyDown("t", KeyModifiers.Control, false));
        Assert.False(h.Input.KeyDown("F4", KeyModifiers.Control, false));
        h.Frame();
        Assert.Equal(0, Probe.Adds);
        Assert.Empty(Probe.Closed);
    }

    [Fact]
    public void Keyboard_reorder_preserves_tab_panel_and_editor_identity_and_selected_key()
    {
        Show();
        var tab = Tab("a");
        var editor = Editor("a");
        var declaration = Probe.Strip!.Items[0];
        h.Focus(tab);
        h.Press("ArrowRight", KeyModifiers.Control | KeyModifiers.Shift);
        var request = Assert.Single(Probe.Reorders);
        Assert.Same(declaration, request.Item);
        Assert.Equal(0, request.OldIndex);
        Assert.Equal(1, request.NewIndex);
        Assert.Equal(new[] { "b", "a", "c" }, Probe.Documents.Select(document => document.Key));
        Assert.Equal(Probe.Documents.Select(document => document.Key), Probe.Strip.Items.Select(item => item.Key));
        Assert.Same(tab, Tab("a"));
        Assert.Same(editor, Editor("a"));
        Assert.Same(tab, h.Focused);
        Assert.Equal("a", Probe.SelectedKey);
        Assert.Equal(1, Probe.SelectedIndex);
    }

    [Fact]
    public void External_keyed_reordering_updates_declaration_order_and_preserves_the_selected_page()
    {
        Show();
        var tab = Tab("a");
        var editor = Editor("a");
        Probe.Documents.Reverse(); Probe.Refresh(); h.Frame(); h.Frame();
        Assert.Equal(new[] { "c", "b", "a" }, Probe.Strip!.Items.Select(item => item.Key));
        Assert.Same(tab, Tab("a"));
        Assert.Same(editor, Editor("a"));
        Assert.Equal("a", Probe.SelectedKey);
        Assert.Equal(2, Probe.SelectedIndex);
        Assert.Empty(Probe.Reorders);
    }

    [Fact]
    public void Closing_the_active_document_selects_and_focuses_the_nearest_enabled_page()
    {
        Show(probe => probe.Documents[1].Disabled = true);
        h.Focus(Tab("a"));
        h.Press("F4", KeyModifiers.Control);
        Assert.Equal(new[] { "a" }, Probe.Closed);
        Assert.Equal("c", Probe.SelectedKey);
        Assert.Equal(1, Probe.SelectedIndex);
        Assert.Same(Tab("c"), h.Focused);
    }

    [Fact]
    public void Removing_an_inactive_page_preserves_selection_and_does_not_steal_external_focus()
    {
        Show();
        h.Click(Tab("c"));
        h.Focus(h.Root.Elements().Single(element => element.Id == "outside"));
        Probe.Documents.RemoveAt(0); Probe.Refresh(); h.Frame(); h.Frame();
        Assert.Equal("c", Probe.SelectedKey);
        Assert.Equal(1, Probe.SelectedIndex);
        Assert.Equal("outside", h.Focused?.Id);
    }

    [Fact]
    public void Pointer_drop_requests_one_final_index_and_suppresses_the_release_click()
    {
        Show();
        var source = Tab("a");
        var end = At(Tab("c"), .95);
        Down(At(source)); Move(end);
        Assert.True(new ElementReference(source).IsPointerCaptured);
        Assert.True(Tab("c").Attributes.GetValueOrDefault("drop-target") is true);
        Up(end);
        var request = Assert.Single(Probe.Reorders);
        Assert.Equal(0, request.OldIndex);
        Assert.Equal(2, request.NewIndex);
        Assert.Equal("a", Probe.SelectedKey);
        Assert.False(new ElementReference(source).IsPointerCaptured);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Drag_out_uses_actual_header_bounds_and_never_also_reorders_or_mutates_the_owner(bool canReorder)
    {
        Show(probe => { probe.DragOutEnabled = true; probe.ReorderEnabled = canReorder; });
        var source = Tab("a");
        var header = h.Bounds(h.One("TabsHeader"));
        var end = new Point(header.X + header.Width / 2, header.Y + header.Height + 12);
        Down(At(source)); Move(end); Up(end);
        var request = Assert.Single(Probe.DragOuts);
        Assert.Equal("a", request.Tab.Key);
        Assert.Equal(end.X, request.X);
        Assert.Equal(end.Y, request.Y);
        Assert.Empty(Probe.Reorders);
        Assert.Equal(new[] { "a", "b", "c" }, Probe.Documents.Select(document => document.Key));
        Assert.False(new ElementReference(source).IsPointerCaptured);
    }

    [Theory]
    [InlineData("escape")]
    [InlineData("capture")]
    [InlineData("disabled")]
    [InlineData("removed")]
    [InlineData("key")]
    public void Canceling_or_invalidating_a_drag_releases_capture_without_a_request(string reason)
    {
        Show(probe => probe.DragOutEnabled = true);
        var source = Tab("a");
        var end = At(Tab("c"), .95);
        Down(At(source)); Move(end);
        switch (reason)
        {
            case "escape": h.Press("Escape"); break;
            case "capture": h.Input.PointerCanceled(clearHover: false); break;
            case "disabled": Probe.Documents[0].Disabled = true; Probe.Refresh(); h.Frame(); break;
            case "removed": Probe.Documents.RemoveAt(0); Probe.Refresh(); h.Frame(); break;
            case "key": Probe.Documents[0].Key = "changed"; Probe.Refresh(); h.Frame(); break;
        }
        Assert.False(new ElementReference(source).IsPointerCaptured);
        Up(end);
        Assert.Empty(Probe.Reorders);
        Assert.Empty(Probe.DragOuts);
    }

    [Fact]
    public void Disabling_a_source_before_crossing_the_drag_threshold_also_cancels_capture()
    {
        Show();
        var source = Tab("a");
        Down(At(source));
        Probe.Documents[0].Disabled = true; Probe.Refresh(); h.Frame();
        Assert.False(new ElementReference(source).IsPointerCaptured);
        var end = At(Tab("c"), .95); Move(end); Up(end);
        Assert.Empty(Probe.Reorders);
    }

    [Fact]
    public void Close_parts_keep_their_click_and_cannot_start_a_reorder()
    {
        Show();
        var source = Tab("a");
        var close = source.Elements().Single(element => element.Name == "TabClose");
        Down(At(close));
        Assert.False(new ElementReference(source).IsPointerCaptured);
        var end = At(Tab("c"), .95); Move(end); Up(end);
        Assert.Empty(Probe.Reorders);
        h.Click(close);
        Assert.Equal(new[] { "a" }, Probe.Closed);
    }

    [Fact]
    public void Nested_tab_keyboard_input_and_panel_editors_cannot_reorder_the_outer_strip()
    {
        Show(probe => probe.Nested = true);
        var inner = h.Root.Elements().Single(element => element.Id == "inner");
        var innerTab = inner.Elements().First(element => element.Name == "Tab");
        h.Focus(innerTab);
        h.Press("ArrowRight", KeyModifiers.Control | KeyModifiers.Shift);
        h.Press("t", KeyModifiers.Control);
        h.Press("F4", KeyModifiers.Control);
        Assert.Empty(Probe.Reorders);
        Assert.Equal(0, Probe.Adds);
        Assert.Empty(Probe.Closed);
        h.Focus(Editor("a").Elements().Single(element => element.Name == "TextFieldInput"));
        h.Press("ArrowRight", KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Empty(Probe.Reorders);
        Assert.Equal(new[] { "a", "b", "c" }, Probe.Strip!.Items.Select(tab => tab.Key));
    }

    [Fact]
    public void Declining_a_reorder_leaves_the_keyed_model_and_selection_untouched()
    {
        Show(probe => probe.ApplyReorder = false);
        h.Focus(Tab("a")); h.Press("ArrowRight", KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Single(Probe.Reorders);
        Assert.Equal(new[] { "a", "b", "c" }, Probe.Documents.Select(document => document.Key));
        Assert.Equal("a", Probe.SelectedKey);
        Assert.Equal(0, Probe.SelectedIndex);
    }

    [Fact]
    public void Compact_headers_keep_a_name_when_the_visual_label_is_hidden()
    {
        Show(probe => probe.WidthMode = TabWidthMode.Compact);
        Assert.False(Tab("a").Attributes.GetValueOrDefault("compact") is true);
        Assert.True(Tab("b").Attributes.GetValueOrDefault("compact") is true);
        Assert.Equal("Beta longer", Tab("b").Attributes["accessible-name"]);
        Assert.Equal("Beta longer", Tab("b").Attributes["tooltip"]);
    }
}
