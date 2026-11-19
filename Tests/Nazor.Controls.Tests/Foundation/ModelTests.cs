using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;

namespace Nazor.Controls.Tests.Foundation;

public sealed class ModelTests
{
    [Fact]
    public void Controllable_state_adopts_changed_parameters_and_keeps_local_choices()
    {
        var state = new ControllableState<int>(0);
        Assert.True(state.Sync(5));
        Assert.True(state.Set(7));
        Assert.False(state.Sync(5));
        Assert.Equal(7, state.Value);
        Assert.True(state.Sync(6));
        Assert.Equal(6, state.Value);
        Assert.False(state.Set(6));
    }

    [Fact]
    public void Selection_supports_ranges_toggles_and_modes()
    {
        var selection = new SelectionModel<string>(SelectionMode.Multiple);
        selection.SetOrder(["a", "b", "c", "d"]);
        selection.Select("b");
        selection.Extend("d");
        Assert.Equal(["b", "c", "d"], selection.SelectedKeys);
        selection.Toggle("c");
        Assert.Equal(["b", "d"], selection.SelectedKeys);
        Assert.True(selection.SelectAll());
        Assert.Equal(4, selection.Count);
        selection.Mode = SelectionMode.Single;
        Assert.Equal(["a"], selection.SelectedKeys);
        selection.AllowEmpty = false;
        Assert.False(selection.Toggle("a"));
        selection.SetOrder(["x"]);
        Assert.True(selection.Prune());
        Assert.Equal(0, selection.Count);
    }

    [Fact]
    public void A_calendar_month_follows_the_culture_week_and_limits()
    {
        var calendar = new CalendarModel { Culture = CultureInfo.GetCultureInfo("it-IT"), Minimum = new DateOnly(2026, 11, 3) };
        var month = calendar.Month(new DateOnly(2026, 11, 15));
        Assert.Equal(6, month.Count);
        Assert.Equal(DayOfWeek.Monday, calendar.WeekStart);
        Assert.Equal(new DateOnly(2026, 10, 26), month[0].Days[0].Date);
        Assert.False(month[0].Days[0].InMonth);
        Assert.False(month[1].Days[0].Available);
        Assert.True(month[1].Days[1].Available);
        Assert.Equal(new DateOnly(2026, 11, 3), calendar.Move(new DateOnly(2026, 11, 5), CalendarStep.Week, -1));
        Assert.Equal("Novembre 2026", calendar.MonthTitle(new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public void Toasts_queue_pause_and_expire()
    {
        var time = new ManualTime();
        var queue = new ToastQueue(time) { MaxVisible = 2 };
        var first = queue.Show(new ToastOptions { Message = "one", Duration = TimeSpan.FromSeconds(5) });
        var second = queue.Show("two");
        var third = queue.Show("three");
        Assert.Equal([first, second], queue.Visible);
        Assert.Equal(1, queue.PendingCount);
        queue.Pause(first);
        time.Advance(TimeSpan.FromSeconds(6));
        Assert.True(queue.Expire());
        Assert.Equal([first, third], queue.Visible);
        queue.Resume(first);
        time.Advance(TimeSpan.FromSeconds(6));
        queue.Expire();
        Assert.Empty(queue.Visible);
    }

    [Fact]
    public void A_tree_flattens_expanded_rows_and_checks_in_cascade()
    {
        var children = new Dictionary<string, IReadOnlyList<string>> { ["root"] = ["a", "b"], ["a"] = ["a1", "a2"] };
        var tree = new TreeModel<string> { Roots = ["root"], Children = item => children.GetValueOrDefault(item) };
        Assert.Single(tree.VisibleRows());
        Assert.True(tree.Reveal("a1"));
        var rows = tree.VisibleRows();
        Assert.Equal(["root", "a", "a1", "a2", "b"], rows.Select(r => r.Item));
        Assert.Equal(3, rows[2].Level);
        Assert.Equal(2, rows[3].PositionInSet);
        tree.SetChecked("a1", true);
        Assert.Equal(TreeCheckState.Mixed, tree.CheckState("a"));
        tree.SetChecked("root", true);
        Assert.Equal(TreeCheckState.Checked, tree.CheckState("root"));
    }

    [Fact]
    public void Gestures_parse_and_display_in_the_platform_notation()
    {
        var gesture = KeyGesture.Parse("Ctrl+Shift+S");
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Shift, gesture.Modifiers);
        Assert.True(gesture.Matches(new KeyboardEventArgs { Key = "s", Modifiers = KeyModifiers.Control | KeyModifiers.Shift }));
        Assert.Equal(KeyGesture.PrimaryModifier, KeyGesture.Parse("Mod+K").Modifiers);
        Assert.Equal("+", KeyGesture.Parse("Ctrl++").Key);
        Assert.Throws<FormatException>(() => KeyGesture.Parse("Hyper+X"));
        if (!OperatingSystem.IsMacOS()) Assert.Equal("Ctrl+Shift+S", gesture.ToDisplayString());
    }
}

internal sealed class ManualTime : TimeProvider
{
    private DateTimeOffset now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => now;

    public void Advance(TimeSpan by) => now += by;
}
