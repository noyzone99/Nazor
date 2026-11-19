using Nazor.Controls.Tests.Foundation.Fixtures;
using Nazor.Input;
using Nazor.Style;

namespace Nazor.Controls.Tests.Foundation;

[Collection("Controls")]
public sealed class ActionTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    [Fact]
    public void A_click_runs_the_action_and_inactive_buttons_ignore_it()
    {
        h.Render<ButtonProbe>();
        var probe = h.Component<ButtonProbe>();
        h.Click(h.ByClass("save"));
        h.Click(h.ByClass("off"));
        h.Click(h.ByClass("busy"));
        Assert.Equal(1, probe.Saved);
        Assert.True(h.ByClass("off").IsDisabled);
        Assert.Equal(true, h.ByClass("busy").Attributes["busy"]);
        Assert.Single(h.ByClass("busy").Elements(), e => e.Name == "ButtonSpinner");
    }

    [Fact]
    public void The_vocabulary_is_written_as_attributes_for_themes()
    {
        h.Render<ButtonProbe>();
        var danger = h.ByClass("danger");
        Assert.Equal("accent", danger.Attributes["appearance"]);
        Assert.Equal("danger", danger.Attributes["intent"]);
        Assert.Equal("large", danger.Attributes["size"]);
        Assert.Equal("pill", danger.Attributes["shape"]);
        Assert.Equal(true, h.ByClass("icon").Attributes["icon-only"]);
        Assert.Equal("standard", h.ByClass("save").Attributes["appearance"]);
    }

    [Fact]
    public void A_command_supplies_text_tooltip_shortcut_and_enabled_state()
    {
        h.Render<ButtonProbe>();
        var probe = h.Component<ButtonProbe>();
        var button = h.ByClass("command");
        Assert.Equal("Refresh", button.Elements().Single(e => e.Name == "ButtonLabel").Text());
        Assert.Contains("F5", (string)button.Attributes["tooltip"]!);
        h.Click(button);
        Assert.Equal(1, probe.Refreshed);
        probe.RefreshCommand.IsEnabled = false;
        h.Frame();
        Assert.True(h.ByClass("command").IsDisabled);
        h.Click(h.ByClass("command"));
        Assert.Equal(1, probe.Refreshed);
    }

    [Fact]
    public void Enter_presses_the_default_button_and_submit_buttons_reach_their_form()
    {
        h.Render<ButtonProbe>();
        var probe = h.Component<ButtonProbe>();
        h.Press("Enter");
        Assert.Equal(1, probe.Saved);
        h.Click(h.ByClass("submit"));
        Assert.Equal(1, probe.Submitted);
    }

    [Fact]
    public void A_toggle_button_keeps_its_pressed_state()
    {
        h.Render<ButtonProbe>();
        var probe = h.Component<ButtonProbe>();
        h.Click(h.ByClass("bold"));
        Assert.True(probe.Bold);
        Assert.Equal(true, h.ByClass("bold").Attributes["pressed"]);
        Assert.Equal("togglebutton", h.ByClass("bold").Attributes["role"]);
        h.Focus(h.ByClass("bold"));
        h.Press(" ");
        Assert.False(probe.Bold);
    }

    [Fact]
    public void The_basic_theme_draws_the_default_button_in_the_accent_color()
    {
        h.Render<ButtonProbe>();
        Assert.Equal(SystemColors.Resolve().AccentColor, h.Style(h.ByClass("save")).BackgroundColor);
        Assert.NotEqual(h.Style(h.ByClass("save")).BackgroundColor, h.Style(h.ByClass("off")).BackgroundColor);
    }

    [Fact]
    public void A_check_box_cycles_from_mixed_to_checked_and_reports_both_changes()
    {
        h.Render<CheckProbe>();
        var probe = h.Component<CheckProbe>();
        var box = h.ByClass("mixed");
        Assert.Equal(true, box.Attributes["indeterminate"]);
        Assert.Equal(1d, h.Style(box.Elements().Single(e => e.Name == "CheckBoxMixed")).Get(StyleProperties.StrokeTrimEnd));
        Assert.Equal(0d, h.Style(box.Elements().Single(e => e.Name == "CheckBoxCheck")).Get(StyleProperties.StrokeTrimEnd));
        h.Click(box);
        Assert.True(probe.Checked);
        Assert.False(probe.Mixed);
        Assert.Equal(1d, h.Style(h.ByClass("mixed").Elements().Single(e => e.Name == "CheckBoxCheck")).Get(StyleProperties.StrokeTrimEnd));
        Assert.Equal(0d, h.Style(h.ByClass("mixed").Elements().Single(e => e.Name == "CheckBoxMixed")).Get(StyleProperties.StrokeTrimEnd));
        h.Click(h.ByClass("mixed"));
        Assert.False(probe.Checked);
        h.Click(h.ByClass("off"));
        Assert.True(h.ByClass("off").IsDisabled);
    }

    [Fact]
    public void A_field_names_describes_and_validates_its_control()
    {
        h.Render<CheckProbe>();
        var check = h.ByClass("field-check");
        var label = h.One("Label");
        Assert.Equal(label.Id, check.Attributes["labeled-by"]);
        Assert.Contains(h.One("FieldDescription").Id!, (string)check.Attributes["described-by"]!);
        Assert.Contains(h.One("FieldError").Id!, (string)check.Attributes["described-by"]!);
        Assert.Equal(true, check.Attributes["required"]);
        Assert.Equal(true, check.Attributes["invalid"]);
        h.Click(label);
        Assert.True(h.Component<CheckProbe>().Notify);
    }
}

[CollectionDefinition("Controls", DisableParallelization = true)]
public sealed class ControlsCollection;
