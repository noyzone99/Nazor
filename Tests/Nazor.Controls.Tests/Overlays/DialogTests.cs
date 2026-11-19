using Nazor.Controls.Primitives;
using Nazor.Controls.Tests.Overlays.Fixtures;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;

namespace Nazor.Controls.Tests.Overlays;

[Collection("Controls")]
public sealed class DialogTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    private DialogProbe Render()
    {
        h.Render<DialogProbe>();
        return h.Component<DialogProbe>();
    }

    private void Refresh(DialogProbe probe)
    {
        h.Host.Renderer.Dispatcher.InvokeAsync(probe.Refresh);
        h.Frame();
    }

    private static ElementNode ButtonOf(ElementNode wrapper) => wrapper.Elements().Single(e => e.Name == "Button");

    private DialogProbe OpenDialog()
    {
        var probe = Render();
        h.Focus(h.ByClass("opener"));
        h.Press("Enter");
        Assert.True(probe.DialogOpen);
        return probe;
    }

    [Fact]
    public void A_dialog_is_named_by_its_title_described_by_its_subtitle_and_shows_every_part()
    {
        OpenDialog();
        var dialog = h.One("Dialog");
        Assert.Equal("dialog", dialog.Attributes["role"]);
        Assert.Equal(true, dialog.Attributes["modal"]);
        Assert.Equal("medium", dialog.Attributes["size"]);
        var title = h.One("DialogTitle");
        Assert.Equal(title.Id, dialog.Attributes["labeled-by"]);
        Assert.Equal("heading", title.Attributes["role"]);
        Assert.Equal(h.One("DialogSubtitle").Id, dialog.Attributes["described-by"]);
        Assert.Single(h.All("DialogScrim"));
        foreach (var part in new[] { "DialogHeader", "DialogHeading", "DialogClose", "DialogBody", "DialogFooter" }) Assert.Single(h.All(part));
        Assert.Equal("Close", h.One("DialogClose").Attributes["accessible-name"]);
        Assert.Equal("button", h.One("DialogClose").Attributes["role"]);
    }

    [Fact]
    public void Opening_focuses_the_first_control_of_the_body_and_closing_returns_the_focus()
    {
        var probe = OpenDialog();
        Assert.Equal("name", h.Focused?.ParentElement?.Class);
        h.Focus(h.ByClass("outside"));
        Assert.NotEqual("outside", h.Focused?.Class);
        h.Click(ButtonOf(h.ByClass("secondary")));
        Assert.False(probe.DialogOpen);
        Assert.Empty(h.All("Dialog"));
        Assert.Equal(["Secondary"], probe.Log);
        Assert.Equal("opener", h.Focused?.Class);
    }

    [Fact]
    public void Escape_runs_the_cancel_action()
    {
        var probe = OpenDialog();
        h.Press("Escape");
        Assert.False(probe.DialogOpen);
        Assert.Equal(["cancel", "Cancel"], probe.Log);
    }

    [Fact]
    public void The_close_button_runs_the_cancel_action()
    {
        var probe = OpenDialog();
        h.Click(h.One("DialogClose"));
        Assert.False(probe.DialogOpen);
        Assert.Equal(["cancel", "Cancel"], probe.Log);
    }

    [Fact]
    public void Without_a_cancel_action_escape_dismisses_the_dialog()
    {
        var probe = Render();
        probe.WithButtons = false;
        probe.DialogOpen = true;
        Refresh(probe);
        h.Press("Escape");
        Assert.False(probe.DialogOpen);
        Assert.Equal(["dismiss"], probe.Log);
    }

    [Fact]
    public void An_explicit_primary_default_button_reports_its_role()
    {
        var probe = OpenDialog();
        var primary = ButtonOf(h.ByClass("primary"));
        Assert.Equal(true, primary.Attributes["default"]);
        Assert.Equal("accent", primary.Attributes["appearance"]);
        Assert.Equal("primary", h.ByClass("primary").Attributes["action-role"]);
        Assert.Equal(true, h.ByClass("cancel").Attributes["dialog-cancel"]);
        Assert.Equal("Cancel", ButtonOf(h.ByClass("cancel")).Text());
        Assert.Equal("Help", ButtonOf(h.ByClass("help")).Text());
        h.Press("Enter");
        Assert.False(probe.DialogOpen);
        Assert.Equal(["export", "Primary"], probe.Log);
    }

    [Fact]
    public void The_help_action_does_not_close_the_dialog()
    {
        var probe = OpenDialog();
        h.Click(ButtonOf(h.ByClass("help")));
        Assert.True(probe.DialogOpen);
        Assert.Equal(["help"], probe.Log);
    }

    [Fact]
    public void The_basic_theme_orders_help_first_and_the_primary_action_last()
    {
        OpenDialog();
        var help = h.Bounds(h.ByClass("help"));
        var cancel = h.Bounds(h.ByClass("cancel"));
        var secondary = h.Bounds(h.ByClass("secondary"));
        var primary = h.Bounds(h.ByClass("primary"));
        Assert.True(help.X < cancel.X);
        Assert.True(cancel.X < secondary.X);
        Assert.True(secondary.X < primary.X);
        Assert.Equal(h.Bounds(h.One("DialogFooter")).Right, primary.Right, 1);
    }

    [Theory]
    [InlineData(DialogSize.Small, 320)]
    [InlineData(DialogSize.Medium, 480)]
    [InlineData(DialogSize.Large, 576)]
    public void The_size_sets_the_width(DialogSize size, double width)
    {
        var probe = Render();
        probe.Size = size;
        probe.DialogOpen = true;
        Refresh(probe);
        Assert.Equal(width, h.Bounds(h.One("Dialog")).Width, 1);
    }

    [Fact]
    public void A_full_screen_dialog_covers_the_window()
    {
        var probe = Render();
        probe.Size = DialogSize.FullScreen;
        probe.DialogOpen = true;
        Refresh(probe);
        var bounds = h.Bounds(h.One("Dialog"));
        Assert.Equal("fullscreen", h.One("Dialog").Attributes["dialog-placement"]);
        Assert.Equal(new Rect(0, 0, 640, 480), bounds);
    }

    [Fact]
    public void A_non_modal_dialog_has_no_scrim()
    {
        var probe = Render();
        probe.Modal = false;
        probe.DialogOpen = true;
        Refresh(probe);
        Assert.Empty(h.All("DialogScrim"));
        Assert.Equal(false, h.One("Dialog").Attributes["modal"]);
    }

    [Fact]
    public void An_alert_dialog_prefers_cancel_and_ignores_presses_outside()
    {
        var probe = Render();
        probe.AlertOpen = true;
        Refresh(probe);
        var alert = h.One("AlertDialog");
        Assert.Equal("alertdialog", alert.Attributes["role"]);
        Assert.Equal(h.One("AlertDialogTitle").Id, alert.Attributes["labeled-by"]);
        Assert.Equal(h.One("AlertDialogMessage").Id, alert.Attributes["described-by"]);
        Assert.Equal("warning", h.One("AlertDialogSeverity").Attributes["severity"]);
        Assert.Equal("Warning", h.One("AlertDialogSeverity").Attributes["accessible-name"]);
        Assert.Same(ButtonOf(h.ByClass("alert-cancel")), h.Focused);
        Assert.Equal("danger", ButtonOf(h.ByClass("delete")).Attributes["intent"]);
        var scrim = h.Bounds(h.One("AlertDialogScrim"));
        h.Input.PointerPressed(new Point(scrim.X + 2, scrim.Y + 2), PointerButton.Primary, KeyModifiers.None);
        h.Input.PointerReleased(new Point(scrim.X + 2, scrim.Y + 2), PointerButton.Primary, KeyModifiers.None);
        h.Frame();
        Assert.True(probe.AlertOpen);
        h.Press("Escape");
        Assert.False(probe.AlertOpen);
        Assert.Equal(["Cancel"], probe.Log);
    }

    [Fact]
    public void An_alert_dialog_without_preference_focuses_its_default_action()
    {
        var probe = Render();
        probe.PreferCancel = false;
        probe.AckOpen = true;
        Refresh(probe);
        Assert.Same(ButtonOf(h.ByClass("ok")), h.Focused);
        Assert.Equal("OK", ButtonOf(h.ByClass("ok")).Text());
    }

    [Fact]
    public void An_acknowledgement_without_cancel_stays_open_on_escape()
    {
        var probe = Render();
        probe.AckOpen = true;
        Refresh(probe);
        h.Press("Escape");
        Assert.True(probe.AckOpen);
        h.Press("Enter");
        Assert.False(probe.AckOpen);
        Assert.Equal(["Primary"], probe.Log);
    }

    [Fact]
    public void Alert_details_expand_and_the_suppress_box_is_bound()
    {
        var probe = Render();
        probe.AlertOpen = true;
        Refresh(probe);
        Assert.Empty(h.All("AlertDialogDetailsContent"));
        var toggle = h.One("AlertDialogDetailsToggle");
        Assert.Equal(false, toggle.Attributes["expanded"]);
        Assert.Equal("Show details", toggle.Text());
        h.Click(toggle);
        Assert.Equal(true, h.One("AlertDialogDetailsToggle").Attributes["expanded"]);
        Assert.Equal("Hide details", h.One("AlertDialogDetailsToggle").Text());
        Assert.Equal("Stack trace", h.One("AlertDialogDetailsText").Text());
        var box = h.One("AlertDialogSuppress").Elements().Single(e => e.Name == "CheckBox");
        Assert.Equal("Don't ask again", box.Elements().Single(e => e.Name == "CheckBoxLabel").Text());
        h.Click(box);
        Assert.True(probe.Suppressed);
        Assert.True(probe.AlertOpen);
    }

    [Fact]
    public void A_drawer_attaches_to_its_edge_with_its_size_and_closes_on_the_scrim()
    {
        var probe = Render();
        probe.DrawerSize = 400;
        probe.DrawerOpen = true;
        Refresh(probe);
        var drawer = h.One("Drawer");
        Assert.Equal("end", drawer.Attributes["side"]);
        Assert.Equal(h.One("DrawerTitle").Id, drawer.Attributes["labeled-by"]);
        Assert.Empty(h.All("DrawerHandle"));
        var bounds = h.Bounds(drawer);
        Assert.Equal(new Rect(240, 0, 400, 480), bounds);
        Assert.Equal("drawer-check", h.Focused?.Class);
        h.Input.PointerPressed(new Point(20, 200), PointerButton.Primary, KeyModifiers.None);
        h.Input.PointerReleased(new Point(20, 200), PointerButton.Primary, KeyModifiers.None);
        h.Frame();
        Assert.False(probe.DrawerOpen);
        Assert.Equal(["drawer-dismiss"], probe.Log);
    }

    [Fact]
    public void A_bottom_sheet_shows_a_handle_and_takes_its_height()
    {
        var probe = Render();
        probe.DrawerPlacement = DrawerPlacement.Bottom;
        probe.DrawerSize = 200;
        probe.DrawerOpen = true;
        Refresh(probe);
        Assert.Single(h.All("DrawerHandle"));
        var bounds = h.Bounds(h.One("Drawer"));
        Assert.Equal(new Rect(0, 280, 640, 200), bounds);
        h.Click(ButtonOf(h.ByClass("drawer-save")));
        Assert.False(probe.DrawerOpen);
        Assert.Equal(["Primary"], probe.Log);
    }

    [Fact]
    public void A_drawer_close_button_dismisses_it()
    {
        var probe = Render();
        probe.DrawerOpen = true;
        Refresh(probe);
        h.Click(h.One("DrawerClose"));
        Assert.False(probe.DrawerOpen);
        Assert.Equal(["drawer-dismiss"], probe.Log);
    }

    [Fact]
    public void Dialogs_reject_invalid_parameters()
    {
        var probe = Render();
        probe.DrawerSize = -5;
        Assert.ThrowsAny<ArgumentException>(() =>
        {
            h.Host.Renderer.Dispatcher.InvokeAsync(probe.Refresh).GetAwaiter().GetResult();
            h.Frame();
        });
    }
}
