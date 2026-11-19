using Nazor.Controls.Tests.Feedback.Fixtures;
using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.Controls.Tests.Feedback;

[Collection("Controls")]
public sealed class ProgressTests : IDisposable
{
    private readonly ControlHarness h = new(width: 640, height: 720);

    public void Dispose() => h.Dispose();

    private static ElementNode Part(ElementNode element, string name) => element.Elements().Single(e => e.Name == name);

    [Fact]
    public void A_progress_bar_exposes_its_range_label_value_and_fraction()
    {
        h.Render<ProgressProbe>();
        var bar = h.ByClass("bar");
        Assert.Equal("progressbar", bar.Attributes["role"]);
        Assert.Equal(0d, bar.Attributes["range-min"]);
        Assert.Equal(100d, bar.Attributes["range-max"]);
        Assert.Equal(40d, bar.Attributes["range-value"]);
        Assert.Equal("Uploading", bar.Attributes["accessible-name"]);
        Assert.Equal("accent", bar.Attributes["intent"]);
        Assert.Contains("--nz-progress-fraction: 0.4", (string)bar.Attributes["style"]!);
        Assert.Equal("Uploading", Part(bar, "ProgressBarLabel").Text());
        Assert.Equal(0.4.ToString("P0", System.Globalization.CultureInfo.CurrentCulture), Part(bar, "ProgressBarValue").Text());
        var track = Part(bar, "ProgressBarTrack");
        Assert.Single(track.Elements(), e => e.Name == "ProgressBarFill");
        Assert.DoesNotContain(track.Elements(), e => e.Name == "ProgressBarBuffer");
    }

    [Fact]
    public void The_fill_width_follows_the_value_in_the_basic_theme()
    {
        h.Render<ProgressProbe>();
        var track = Part(h.ByClass("bar"), "ProgressBarTrack");
        var fill = Part(track, "ProgressBarFill");
        var ratio = h.Bounds(fill).Width / (h.Bounds(track).Width - 2);
        Assert.InRange(ratio, 0.38, 0.42);
        Assert.Equal(SystemColors.Resolve().AccentColor, h.Style(fill).BackgroundColor);
        var probe = h.Component<ProgressProbe>();
        probe.Value = 100;
        probe.Refresh();
        h.Frame();
        track = Part(h.ByClass("bar"), "ProgressBarTrack");
        Assert.InRange(h.Bounds(Part(track, "ProgressBarFill")).Width, h.Bounds(track).Width - 3, h.Bounds(track).Width);
        Assert.Equal(true, h.ByClass("bar").Attributes["complete"]);
    }

    [Fact]
    public void An_indeterminate_bar_omits_the_value_and_is_hatched_without_motion()
    {
        h.Render<ProgressProbe>();
        var busy = h.ByClass("busy");
        Assert.Equal(true, busy.Attributes["indeterminate"]);
        Assert.False(busy.Attributes.ContainsKey("range-value"));
        Assert.True(busy.Attributes.ContainsKey("range-max"));
        Assert.Equal("", Part(busy, "ProgressBarValue").Text());
        var track = Part(busy, "ProgressBarTrack");
        var fill = Part(track, "ProgressBarFill");
        Assert.Equal(true, fill.Attributes["indeterminate"]);
        Assert.InRange(h.Bounds(fill).Width, h.Bounds(track).Width - 3, h.Bounds(track).Width);
        var image = h.Image();
        var bounds = h.Bounds(fill);
        var colors = Enumerable.Range(0, 12).Select(i => image.GetPixel((int)bounds.X + 2 + i, (int)(bounds.Y + bounds.Height / 2))).Distinct().Count();
        Assert.True(colors > 1, "The indeterminate fill should be striped.");
    }

    [Fact]
    public void A_buffer_sits_behind_the_fill()
    {
        h.Render<ProgressProbe>();
        var bar = h.ByClass("buffered");
        Assert.Contains("--nz-progress-buffer: 0.6", (string)bar.Attributes["style"]!);
        var track = Part(bar, "ProgressBarTrack");
        var buffer = Part(track, "ProgressBarBuffer");
        var fill = Part(track, "ProgressBarFill");
        Assert.True(h.Bounds(buffer).Width > h.Bounds(fill).Width * 1.8);
        Assert.Equal("large", bar.Attributes["size"]);
        Assert.Equal("Video", bar.Attributes["accessible-name"]);
        Assert.True(h.Bounds(track).Height > h.Bounds(Part(h.ByClass("bar"), "ProgressBarTrack")).Height);
    }

    [Fact]
    public void A_segmented_bar_fills_whole_blocks()
    {
        h.Render<ProgressProbe>();
        var segments = Part(h.ByClass("blocks"), "ProgressBarTrack").Elements().ToList();
        Assert.Equal(5, segments.Count);
        Assert.All(segments, s => Assert.Equal("ProgressBarSegment", s.Name));
        Assert.Equal([true, true, false, false, false], segments.Select(s => s.Attributes["filled"] is true));
        Assert.Equal(SystemColors.Resolve().AccentColor, h.Style(segments[0]).BackgroundColor);
        Assert.NotEqual(h.Style(segments[0]).BackgroundColor, h.Style(segments[4]).BackgroundColor);
        var busy = Part(h.ByClass("blocks-busy"), "ProgressBarTrack").Elements().ToList();
        Assert.Equal(4, busy.Count);
        Assert.All(busy, s => Assert.Equal(true, s.Attributes["indeterminate"]));
    }

    [Fact]
    public void Custom_value_text_and_intent_are_written()
    {
        h.Render<ProgressProbe>();
        var bar = h.ByClass("custom");
        Assert.Equal("3 of 7 files", Part(bar, "ProgressBarValue").Text());
        Assert.Equal("danger", bar.Attributes["intent"]);
        Assert.Equal(7d, bar.Attributes["range-max"]);
    }

    [Fact]
    public void A_spinner_draws_a_ring_fraction_and_names_itself()
    {
        h.Render<ProgressProbe>();
        var spin = h.ByClass("spin");
        Assert.Equal("progressbar", spin.Attributes["role"]);
        Assert.Equal(true, spin.Attributes["indeterminate"]);
        Assert.False(spin.Attributes.ContainsKey("range-value"));
        Assert.Equal("Loading", spin.Attributes["accessible-name"]);
        var indicator = Part(spin, "SpinnerIndicator");
        var ring = Part(indicator, "SpinnerRing");
        Assert.Equal("ring", ring.Attributes["glyph"]);
        Assert.Equal(1d, ring.Attributes["ring-fraction"]);
        Assert.Equal(0.25, h.Style(ring).Get(StyleProperties.StrokeTrimEnd));
        Assert.Equal(1d, Part(indicator, "SpinnerTrack").Attributes["ring-fraction"]);
        var value = h.ByClass("spin-value");
        Assert.Equal(75d, value.Attributes["range-value"]);
        var valueRing = Part(Part(value, "SpinnerIndicator"), "SpinnerRing");
        Assert.Equal(1d, valueRing.Attributes["ring-fraction"]);
        Assert.Equal(0.75, h.Style(valueRing).Get(StyleProperties.StrokeTrimEnd));
        Assert.Equal("Syncing", value.Attributes["accessible-name"]);
        Assert.Equal("Syncing", Part(value, "SpinnerLabel").Text());
        Assert.True(h.Bounds(Part(value, "SpinnerIndicator")).Width > h.Bounds(indicator).Width);
    }

    [Fact]
    public void A_meter_reports_its_quality_from_the_thresholds()
    {
        h.Render<ProgressProbe>();
        var probe = h.Component<ProgressProbe>();
        var disk = h.ByClass("disk");
        Assert.Equal("meter", disk.Attributes["role"]);
        Assert.Equal(0.5, disk.Attributes["range-value"]);
        Assert.Equal("optimal", disk.Attributes["quality"]);
        Assert.Equal(0.6, disk.Attributes["meter-low"]);
        Assert.Equal(0.85, disk.Attributes["meter-high"]);
        Assert.Equal("Disk", disk.Attributes["accessible-name"]);
        Assert.Contains("--nz-meter-fraction: 0.5", (string)disk.Attributes["style"]!);
        var fill = Part(Part(disk, "MeterTrack"), "MeterFill");
        Assert.NotEqual(SystemColors.Resolve().AccentColor, h.Style(fill).BackgroundColor);
        var optimal = h.Style(fill).BackgroundColor;
        probe.Disk = 0.7;
        probe.Refresh();
        h.Frame();
        Assert.Equal("suboptimal", h.ByClass("disk").Attributes["quality"]);
        probe.Disk = 0.95;
        probe.Refresh();
        h.Frame();
        Assert.Equal("worst", h.ByClass("disk").Attributes["quality"]);
        Assert.NotEqual(optimal, h.Style(Part(Part(h.ByClass("disk"), "MeterTrack"), "MeterFill")).BackgroundColor);
    }

    [Theory]
    [InlineData(0.5, null, null, null, MeterQuality.Optimal)]
    [InlineData(0.1, 0.3, 0.7, 0.5, MeterQuality.Suboptimal)]
    [InlineData(0.9, 0.3, 0.7, 0.9, MeterQuality.Optimal)]
    [InlineData(0.5, 0.3, 0.7, 0.9, MeterQuality.Suboptimal)]
    [InlineData(0.1, 0.3, 0.7, 0.9, MeterQuality.Worst)]
    [InlineData(0.1, 0.3, 0.7, 0.1, MeterQuality.Optimal)]
    [InlineData(0.9, 0.3, 0.7, 0.1, MeterQuality.Worst)]
    public void Meter_quality_follows_the_html_rules(double value, double? low, double? high, double? optimum, MeterQuality expected)
    {
        var meter = new Meter { Value = value, Low = low, High = high, Optimum = optimum };
        Assert.Equal(expected, meter.Quality);
    }

    [Fact]
    public void A_vertical_segmented_meter_fills_from_the_bottom()
    {
        h.Render<ProgressProbe>();
        var signal = h.ByClass("signal");
        Assert.Equal("vertical", signal.Attributes["orientation"]);
        var segments = Part(signal, "MeterTrack").Elements().ToList();
        Assert.Equal(5, segments.Count);
        Assert.Equal(3, segments.Count(s => s.Attributes["filled"] is true));
        Assert.True(h.Bounds(segments[0]).Y > h.Bounds(segments[4]).Y, "The first segment is at the bottom.");
        Assert.True(h.Bounds(Part(signal, "MeterTrack")).Height > h.Bounds(Part(signal, "MeterTrack")).Width);
    }

    [Fact]
    public void Invalid_ranges_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Validate(new ProgressBar { Minimum = 10, Maximum = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Validate(new ProgressBar { Value = double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Validate(new ProgressBar { Segments = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Validate(new Spinner { Value = double.PositiveInfinity }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Validate(new Meter { Low = double.NaN }));
    }

    internal static void Validate(object component)
    {
        try
        {
            component.GetType().GetMethod("OnParametersSet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(component, null);
        }
        catch (System.Reflection.TargetInvocationException e) when (e.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(e.InnerException);
        }
    }
}
