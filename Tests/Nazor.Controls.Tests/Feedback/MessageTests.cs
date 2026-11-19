using Nazor.Controls.Tests.Feedback.Fixtures;
using Nazor.Style;

namespace Nazor.Controls.Tests.Feedback;

[Collection("Controls")]
public sealed class MessageTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    private static ElementNode Part(ElementNode element, string name) =>
        element.Elements().SelectMany(e => e.Elements().Prepend(e)).First(e => e.Name == name);

    [Fact]
    public void An_inline_message_is_a_live_status_with_a_named_severity_mark()
    {
        h.Render<MessageProbe>();
        var info = h.ByClass("info");
        Assert.Equal("status", info.Attributes["role"]);
        Assert.Equal("polite", info.Attributes["live"]);
        Assert.Equal("info", info.Attributes["severity"]);
        Assert.Equal("compact", info.Attributes["layout"]);
        var mark = Part(info, "InlineMessageMark");
        Assert.Equal("image", mark.Attributes["role"]);
        Assert.Equal("Information", mark.Attributes["accessible-name"]);
        Assert.Equal("i", mark.Text());
        Assert.Equal("Heads up", Part(info, "InlineMessageTitle").Text());
        Assert.Equal("The report is generated nightly.", Part(info, "InlineMessageText").Text());
        Assert.DoesNotContain(info.Elements(), e => e.Name == "InlineMessageClose");
        var success = Part(h.ByClass("success"), "InlineMessageMark");
        Assert.Equal("check", success.Attributes["glyph"]);
        Assert.Equal("Success", success.Attributes["accessible-name"]);
    }

    [Fact]
    public void Errors_are_announced_assertively_and_tinted_with_the_danger_color()
    {
        h.Render<MessageProbe>();
        var error = h.ByClass("error");
        Assert.Equal("assertive", error.Attributes["live"]);
        Assert.Equal("expanded", error.Attributes["layout"]);
        Assert.Equal("Error", Part(error, "InlineMessageMark").Attributes["accessible-name"]);
        Assert.Equal(h.Style(Part(error, "InlineMessageMark")).BackgroundColor, h.Style(error).Get(StyleProperties.BorderTopColor));
        Assert.NotEqual(h.Style(Part(h.ByClass("info"), "InlineMessageMark")).BackgroundColor, h.Style(Part(error, "InlineMessageMark")).BackgroundColor);
        Assert.NotNull(Part(error, "InlineMessageActions"));
        Assert.True(h.Bounds(Part(error, "InlineMessageActions")).Y > h.Bounds(Part(error, "InlineMessageTitle")).Y, "Expanded actions go below the text.");
    }

    [Fact]
    public void The_close_button_hides_the_message_and_updates_the_binding()
    {
        h.Render<MessageProbe>();
        var probe = h.Component<MessageProbe>();
        var close = Part(h.ByClass("error"), "InlineMessageClose");
        Assert.Equal("button", close.Attributes["role"]);
        Assert.Equal("Close", close.Attributes["accessible-name"]);
        h.Click(close);
        Assert.False(probe.ErrorOpen);
        Assert.Equal(1, probe.Closed);
        Assert.DoesNotContain(h.Root.Elements(), e => e.Class == "error");
        probe.ErrorOpen = true;
        probe.Refresh();
        h.Frame();
        Assert.Single(h.Root.Elements(), e => e.Class == "error");
    }

    [Fact]
    public void A_banner_runs_its_action_and_can_be_dismissed_from_the_keyboard()
    {
        h.Render<MessageProbe>();
        var probe = h.Component<MessageProbe>();
        var banner = h.ByClass("banner");
        Assert.Equal("warning", banner.Attributes["severity"]);
        Assert.Equal("polite", banner.Attributes["live"]);
        Assert.Equal("Warning", Part(banner, "BannerMark").Attributes["accessible-name"]);
        Assert.Equal("You are offline.", Part(banner, "BannerMessage").Text());
        var action = Part(banner, "BannerAction");
        Assert.Equal("Reconnect", action.Text());
        h.Click(action);
        Assert.Equal(1, probe.Actions);
        h.Focus(Part(h.ByClass("banner"), "BannerClose"));
        h.Press("Enter");
        Assert.False(probe.BannerOpen);
        Assert.DoesNotContain(h.Root.Elements(), e => e.Class == "banner");
        var fixedBanner = h.ByClass("fixed");
        Assert.DoesNotContain(fixedBanner.Elements(), e => e.Name is "BannerClose" or "BannerAction");
        Assert.Equal("info", fixedBanner.Attributes["severity"]);
    }

    [Fact]
    public void Invalid_message_parameters_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProgressTests.Validate(new InlineMessage { Severity = (Severity)42 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProgressTests.Validate(new InlineMessage { Layout = (InlineMessageLayout)9 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProgressTests.Validate(new Banner { Severity = (Severity)(-1) }));
    }
}
