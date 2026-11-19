using Nazor.Application.Tests.Fixtures;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Application.Tests;

public sealed class LeafShowcaseFeedbackTests : IDisposable
{
    private readonly GalleryShowcaseClock clock = new();
    private readonly TestHost host = new(captureExceptions: true);
    private readonly SkiaRenderer graphics = new(new FontOptions { UseSystemFonts = true });
    private readonly Scene scene;
    private readonly InputManager input;
    private RootComponent root = null!;
    private LeafFeedbackProbe Probe => (LeafFeedbackProbe)root.Component;

    public LeafShowcaseFeedbackTests()
    {
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(1100, 1200) };
        scene.Styles.Theme = NazorLeafTheme.Theme;
        input = new InputManager(host.Renderer, scene);
    }

    public void Dispose() { input.Dispose(); scene.Dispose(); host.Dispose(); graphics.Dispose(); }

    private void Show(bool reduced = true)
    {
        scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: reduced);
        scene.Transitions.ReducedMotion = reduced;
        root = host.Render<LeafFeedbackProbe>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(LeafFeedbackProbe.Time)] = clock }));
        Frame();
    }

    private void Frame()
    {
        for (var frame = 0; frame < 3; frame++) { host.RunPending(); scene.Update(); scene.BuildDisplayList(); }
        Assert.Empty(host.Exceptions);
    }

    private ElementNode ById(string id) => root.Elements().Single(element => element.Id == id);
    private double Download => Assert.IsType<double>(ById("showcase-download-progress").Attributes["range-value"]);
    private double Ring => Assert.IsType<double>(root.Single("Slider").Attributes["range-value"]);
    private void Activate(ElementNode element) { Assert.True(input.Activate(element)); Frame(); }
    private void Advance(int milliseconds) { clock.Advance(TimeSpan.FromMilliseconds(milliseconds)); Frame(); }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Restart_runs_a_real_bounded_download_and_a_new_run_cancels_the_previous_timer(bool reduced)
    {
        Show(reduced);
        Assert.Equal(64, Download);
        Activate(ById("showcase-restart-download"));
        Assert.Equal(0, Download);
        Advance(260);
        Assert.Equal(10, Download);
        Activate(ById("showcase-restart-download"));
        Advance(260);
        Assert.Equal(10, Download);
        Advance(2340);
        Assert.Equal(100, Download);
        Assert.Equal("Simboli scaricati", Probe.Notifications.Visible.Single().Options.Message);
        Advance(1000);
        Assert.Equal(100, Download);
        Assert.Single(Probe.Notifications.Visible);
    }

    [Fact]
    public void Ring_runs_restart_stop_at_completion_and_yield_to_manual_value_changes()
    {
        Show();
        Activate(ById("showcase-simulate-ring"));
        Assert.Equal(0, Ring);
        Advance(320);
        Assert.Equal(12, Ring);
        Activate(ById("showcase-ring-action"));
        Advance(320);
        Assert.Equal(12, Ring);
        Assert.True(input.SetRangeValue(root.Single("Slider"), 75));
        Frame();
        Advance(3200);
        Assert.Equal(75, Ring);
        Assert.Contains("Esportazione 75%", ById("showcase-ring-action").Text());
        Activate(ById("showcase-simulate-ring"));
        Advance(2880);
        Assert.Equal(100, Ring);
        Assert.Contains("Esportato", ById("showcase-ring-action").Text());
        Assert.Equal(0, clock.ActiveTimers);
    }

    [Fact]
    public void Notifications_expire_at_4200ms_and_the_action_reports_undo()
    {
        Show();
        Activate(ById("showcase-show-notification"));
        var entry = Probe.Notifications.Visible.Single();
        Assert.Equal(TimeSpan.FromMilliseconds(4200), entry.Options.Duration);
        Assert.Equal("Annulla", entry.Options.ActionText);
        Advance(4199);
        Assert.Single(Probe.Notifications.Visible);
        Advance(1);
        Assert.Empty(Probe.Notifications.Visible);
        Activate(ById("showcase-show-notification"));
        Activate(root.Single("ToastAction"));
        Assert.Equal("Modifiche annullate", Probe.Notifications.Visible.Single().Options.Message);
        Advance(4200);
        Assert.Empty(Probe.Notifications.Visible);
    }

    [Fact]
    public void The_destructive_dialog_has_the_trash_mark_cancel_focus_and_session_suppression_with_restore()
    {
        Show();
        Activate(ById("showcase-delete-dialog"));
        var dialog = root.Single("AlertDialog");
        Assert.Equal("alertdialog", dialog.Attributes["role"]);
        Assert.Equal("Eliminare il tema “Vassoio scuro”?", dialog.Attributes["accessible-name"]);
        var mark = dialog.Elements().Single(element => element.Class == "demo-delete-mark");
        Assert.True(mark.Attributes.GetValueOrDefault("semantic-hidden") is true);
        Assert.Equal(LeafSymbol.Trash.Glyph, mark.Elements().Single(element => element.Name == "Icon").Text());
        var cancel = dialog.Elements().Single(element => element.Name == "DialogButton" && element.Attributes.GetValueOrDefault("action-role") is "cancel").Elements().Single(element => element.Name == "Button");
        Assert.Same(cancel, input.Focused);
        Activate(dialog.Elements().Single(element => element.Name == "CheckBox"));
        Activate(cancel);
        Assert.DoesNotContain(root.Elements(), element => element.Name == "AlertDialog");
        Assert.Empty(Probe.Notifications.Visible);
        Activate(ById("showcase-delete-dialog"));
        Assert.False(root.Single("CheckBox").Attributes.GetValueOrDefault("checked") is true);
        Activate(root.Single("CheckBox"));
        Activate(root.Single("AlertDialog").Elements().Single(element => element.Name == "DialogButton" && element.Attributes.GetValueOrDefault("action-role") is "destructive").Elements().Single(element => element.Name == "Button"));
        Assert.DoesNotContain(root.Elements(), element => element.Name == "AlertDialog");
        Assert.True(ById("showcase-delete-dialog").IsDisabled);
        Assert.Equal("Tema nel cestino", ById("showcase-theme-status").Text());
        Assert.Equal("Tema eliminato", Probe.Notifications.Visible.Single().Options.Message);
        Activate(root.Single("ToastAction"));
        Assert.False(ById("showcase-delete-dialog").IsDisabled);
        Activate(ById("showcase-delete-dialog"));
        Assert.DoesNotContain(root.Elements(), element => element.Name == "AlertDialog");
        Assert.True(ById("showcase-delete-dialog").IsDisabled);
        Activate(ById("showcase-reset-confirmation"));
        Activate(ById("showcase-restore-theme"));
        Activate(ById("showcase-delete-dialog"));
        Assert.Single(root.Elements("AlertDialog"));
    }

    [Fact]
    public void Disposing_feedback_cancels_simulation_timers_and_ignores_work_already_queued_from_another_thread()
    {
        Show();
        Activate(ById("showcase-restart-download"));
        Activate(ById("showcase-simulate-ring"));
        Assert.Equal(2, clock.ActiveTimers);
        Exception? fault = null;
        var worker = new Thread(() =>
        {
            try { clock.Advance(TimeSpan.FromMilliseconds(320)); }
            catch (Exception exception) { fault = exception; }
        }) { IsBackground = true };
        worker.Start();
        Assert.True(worker.Join(TimeSpan.FromSeconds(2)));
        Assert.Null(fault);
        Probe.HideFeedback();
        Frame();
        Assert.Equal(0, clock.ActiveTimers);
        Advance(10000);
        Assert.DoesNotContain(root.Elements(), element => element.Id == "showcase-download-progress");
        Assert.Empty(Probe.Notifications.Visible);
    }
}
