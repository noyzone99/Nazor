using Nazor.Controls.Tests.Feedback;
using Nazor.Controls.Tests.Foundation.Fixtures;
using Nazor.Graphics;
using Nazor.Input;

namespace Nazor.Controls.Tests.Foundation;

[Collection("Controls")]
public sealed class RepeatButtonTests : IDisposable
{
    private readonly ControlHarness h = new();
    private RepeatProbe Probe => h.Component<RepeatProbe>();
    private ElementNode Held => h.Root.Elements().Single(element => element.Id == "held");
    public void Dispose() => h.Dispose();
    private void Show(bool primitive = false, bool repeat = true)
    {
        h.Render<RepeatProbe>(); Probe.Primitive = primitive; Probe.Enabled = repeat; Probe.Refresh(); h.Frame();
    }
    private Point AtHeld()
    {
        var box = h.Bounds(Held); return new(box.X + box.Width / 2, box.Y + box.Height / 2);
    }
    private void Down(PointerButton button = PointerButton.Primary) { h.Input.PointerPressed(AtHeld(), button, KeyModifiers.None); h.Frame(); }
    private void Up() { h.Input.PointerReleased(AtHeld(), PointerButton.Primary, KeyModifiers.None); h.Frame(); }
    private void Advance(int milliseconds) { Probe.Clock.Advance(TimeSpan.FromMilliseconds(milliseconds)); h.Frame(); }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Opt_in_repeat_fires_on_press_then_after_delay_and_interval_without_a_release_action(bool primitive)
    {
        Show(primitive); Down();
        Assert.Equal(1, Probe.Actions); Assert.True(new ElementReference(Held).IsPointerCaptured);
        Advance(399); Assert.Equal(1, Probe.Actions);
        Advance(1); Assert.Equal(2, Probe.Actions);
        Advance(200); Assert.Equal(4, Probe.Actions);
        Up(); Assert.Equal(4, Probe.Actions); Assert.Equal(0, Probe.Clock.ActiveTimers);
        Advance(1000); Assert.Equal(4, Probe.Actions);
        h.Click(Held); Assert.Equal(5, Probe.Actions);
    }

    [Fact]
    public void Ordinary_button_stays_click_based_without_opt_in_repetition()
    {
        Show(repeat: false); Down(); Advance(1000);
        Assert.Equal(0, Probe.Actions); Assert.Equal(0, Probe.Clock.ActiveTimers);
        Up(); Assert.Equal(1, Probe.Actions);
    }

    [Theory]
    [InlineData(false, "escape")]
    [InlineData(false, "capture")]
    [InlineData(false, "leave")]
    [InlineData(false, "disabled")]
    [InlineData(false, "busy")]
    [InlineData(false, "ancestor")]
    [InlineData(false, "remove")]
    [InlineData(false, "clock")]
    [InlineData(true, "escape")]
    [InlineData(true, "capture")]
    [InlineData(true, "leave")]
    [InlineData(true, "disabled")]
    [InlineData(true, "ancestor")]
    [InlineData(true, "remove")]
    [InlineData(true, "clock")]
    public void Canceled_held_input_stops_timers_and_never_adds_a_release_action(bool primitive, string reason)
    {
        Show(primitive); var held = Held; var point = AtHeld(); var originalClock = Probe.Clock; Down();
        switch (reason)
        {
            case "escape": h.Press("Escape"); break;
            case "capture": h.Input.PointerCanceled(clearHover: false); break;
            case "leave": h.Input.PointerMoved(new Point(500, 400), KeyModifiers.None); h.Frame(); break;
            case "disabled": Probe.Disabled = true; Probe.Refresh(); h.Frame(); break;
            case "busy": Probe.Busy = true; Probe.Refresh(); h.Frame(); break;
            case "ancestor": Probe.ParentDisabled = true; Probe.Refresh(); h.Frame(); break;
            case "remove": Probe.Visible = false; Probe.Refresh(); h.Frame(); break;
            case "clock": Probe.Clock = new FeedbackClock(); Probe.Refresh(); h.Frame(); break;
        }
        originalClock.Advance(TimeSpan.FromSeconds(2)); h.Frame();
        Assert.Equal(1, Probe.Actions); Assert.Equal(0, originalClock.ActiveTimers);
        Assert.False(new ElementReference(held).IsPointerCaptured);
        h.Input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None); h.Frame();
        Assert.Equal(1, Probe.Actions);
    }

    [Theory]
    [InlineData(false, "Enter")]
    [InlineData(false, " ")]
    [InlineData(true, "Enter")]
    [InlineData(true, " ")]
    public void Keyboard_hold_uses_the_same_timer_and_ignores_OS_repeat_without_an_extra_keyup_action(bool primitive, string key)
    {
        Show(primitive); h.Focus(Held);
        h.Input.KeyDown(key, KeyModifiers.None, false); h.Frame(); Assert.Equal(1, Probe.Actions);
        Advance(400); Assert.Equal(2, Probe.Actions);
        h.Input.KeyDown(key, KeyModifiers.None, true); h.Frame(); Assert.Equal(2, Probe.Actions);
        h.Input.KeyUp(key, KeyModifiers.None); h.Frame(); Assert.Equal(2, Probe.Actions);
        Assert.Equal(0, Probe.Clock.ActiveTimers); Advance(1000); Assert.Equal(2, Probe.Actions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Secondary_pointers_do_not_start_repetition(bool primitive)
    {
        Show(primitive); Down(PointerButton.Secondary); Advance(1000);
        Assert.Equal(0, Probe.Actions); Assert.Equal(0, Probe.Clock.ActiveTimers);
        Assert.False(new ElementReference(Held).IsPointerCaptured);
    }

    [Fact]
    public void Leaving_keyboard_focus_cancels_held_repetition()
    {
        Show(); h.Focus(Held); h.Input.KeyDown(" ", KeyModifiers.None, false); h.Frame();
        h.Focus(h.Root.Elements().Single(element => element.Id == "outside"));
        Advance(1000); Assert.Equal(1, Probe.Actions); Assert.Equal(0, Probe.Clock.ActiveTimers);
    }

    [Fact]
    public void Queued_timer_work_cannot_run_after_capture_cancellation()
    {
        Show(); Down();
        var completed = 0;
        ThreadPool.QueueUserWorkItem(_ => { Probe.Clock.Advance(TimeSpan.FromMilliseconds(400)); Volatile.Write(ref completed, 1); });
        Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref completed) == 1, 2000));
        h.Input.PointerCanceled(clearHover: false);
        h.Frame();
        Assert.Equal(1, Probe.Actions); Assert.Equal(0, Probe.Clock.ActiveTimers);
    }

    [Fact]
    public void Long_press_remains_pointer_only_and_suppresses_its_release_click()
    {
        Show(primitive: true, repeat: false); Probe.LongPress = true; Probe.Refresh(); h.Frame();
        Down(); Advance(499); Assert.Equal(0, Probe.Actions); Assert.Equal(0, Probe.LongActions);
        Advance(1); Assert.Equal(1, Probe.LongActions);
        Up(); Assert.Equal(0, Probe.Actions); Assert.Equal(0, Probe.Clock.ActiveTimers);
    }

    [Fact]
    public void Escape_before_a_long_press_threshold_cancels_both_long_press_and_release_activation()
    {
        Show(primitive: true, repeat: false); Probe.LongPress = true; Probe.Refresh(); h.Frame();
        Down(); h.Press("Escape"); Advance(600); Up();
        Assert.Equal(0, Probe.Actions); Assert.Equal(0, Probe.LongActions);
        Assert.Equal(0, Probe.Clock.ActiveTimers);
    }
}
