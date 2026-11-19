using System.Globalization;
using Nazor.Animation;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;

namespace Nazor.Rendering.Tests;

public sealed class ProgressRingMotionTests
{
    [Theory]
    [InlineData(1f, 0)]
    [InlineData(1.5f, 0)]
    [InlineData(2f, 0)]
    [InlineData(1f, 1)]
    [InlineData(1.5f, 1)]
    [InlineData(2f, 1)]
    public void Theme_motion_coordinates_real_arcs_text_completion_and_actual_accessibility(float scale, int percentPositivePattern)
    {
        using var culture = new PercentCulture(percentPositivePattern);
        var percentSeparator = percentPositivePattern == 0 ? " " : "";
        var clock = new ManualAnimationClock();
        using (var harness = new RenderHarness(clock: clock))
        {
            harness.Scene.Styles.Theme = NazorLeafTheme.Theme;
            harness.Scene.Transitions.ReducedMotion = false;
            harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: false);
            var root = harness.Host.Render<ProgressRingPage>();
            var page = (ProgressRingPage)root.Component;
            Frame(harness, scale);
            var indicator = root.Single("SpinnerIndicator");
            var presenter = Assert.IsType<NumberPresenter>(indicator.Owner);
            var ring = root.Single("SpinnerRing");
            Assert.Equal(42d, root.Single("Spinner").Attributes["range-value"]);
            Assert.Equal("0" + percentSeparator + "%", root.Single("SpinnerValue").Text());
            clock.Advance(50);
            Frame(harness, scale);
            Assert.Equal(0.15285648, presenter.Presentation.Value, 10);
            Assert.Equal("15" + percentSeparator + "%", root.Single("SpinnerValue").Text());
            Assert.Equal(presenter.Presentation.Value, harness.Scene.GetEffectiveValue(ring, StyleProperties.StrokeTrimEnd), 10);
            page.Value = 100;
            page.Refresh();
            harness.Host.RunPending();
            Frame(harness, scale);
            Assert.Equal(100d, root.Single("Spinner").Attributes["range-value"]);
            Assert.False(presenter.Presentation.AtEnd);
            Assert.Equal(0d, harness.Scene.GetEffectiveValue(root.Single("SpinnerCheck"), StyleProperties.Opacity));
            for (var index = 0; index < 26; index++)
            {
                clock.Advance(50);
                harness.Host.RunPending();
                harness.Scene.Update();
                harness.Scene.BuildDisplayList();
            }
            var completed = Frame(harness, scale);
            var check = root.Single("SpinnerCheck");
            Assert.Equal(1d, presenter.Presentation.Value);
            Assert.True(presenter.Presentation.AtEnd);
            Assert.Equal("100" + percentSeparator + "%", root.Single("SpinnerValue").Text());
            Assert.Equal(1d, harness.Scene.GetEffectiveValue(check, StyleProperties.Opacity));
            var content = harness.Scene.GetContentBounds(check);
            var knee = new Point(content.X + content.Width * 0.42f, content.Y + content.Height * 0.76f);
            Assert.NotEqual(new Graphics.Color(255, 255, 255, 255), completed.GetPixel((int)MathF.Floor(knee.X * scale), (int)MathF.Floor(knee.Y * scale)));
            Assert.Same(ring, root.Single("SpinnerRing"));
            Assert.Null(harness.Scene.TimeUntilNextFrame);
        }

        clock = new ManualAnimationClock();
        using (var harness = new RenderHarness(clock: clock))
        {
            harness.Scene.Styles.Theme = Nazor.Themes.Fluent.Themes.Fluent;
            harness.Scene.Transitions.ReducedMotion = false;
            harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: false);
            var root = harness.Host.Render<ProgressRingPage>();
            var page = (ProgressRingPage)root.Component;
            Frame(harness, scale);
            var presenter = Assert.IsType<NumberPresenter>(root.Single("SpinnerIndicator").Owner);
            clock.Advance(500);
            Frame(harness, scale);
            Assert.Equal(0.25, presenter.Presentation.Value, 10);
            var stroke = harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>().Last();
            var bounds = harness.Scene.GetBounds(root.Single("SpinnerIndicator"));
            Assert.Equal(bounds.X + bounds.Width / 2 + bounds.Width * 0.4425f, stroke.Points[^1].X, 0.001f);
            Assert.Equal(bounds.Y + bounds.Height / 2, stroke.Points[^1].Y, 0.001f);
            Assert.Equal(bounds.Width * 0.08296875f, stroke.Thickness, 4);
            Assert.Equal(0, harness.Scene.GetStyle(root.Single("SpinnerTrack")).Color.A);
            clock.Advance(100);
            page.Value = 80;
            page.Refresh();
            harness.Host.RunPending();
            Frame(harness, scale);
            Assert.Equal(0.3, presenter.Presentation.Value, 10);
            clock.Advance(500);
            Frame(harness, scale);
            Assert.Equal(0.55, presenter.Presentation.Value, 10);
            Assert.Equal(80d, root.Single("Spinner").Attributes["range-value"]);
            page.Value = 20;
            page.Refresh();
            harness.Host.RunPending();
            Frame(harness, scale);
            Assert.Equal(0.2, presenter.Presentation.Value);
            Assert.Equal(NativeTrim(0.2), harness.Scene.GetEffectiveValue(root.Single("SpinnerRing"), StyleProperties.StrokeTrimEnd), 8);
            page.Value = 100;
            page.Refresh();
            harness.Host.RunPending();
            Frame(harness, scale);
            harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
            Frame(harness, scale);
            Assert.Equal(1d, presenter.Presentation.Value);
            Assert.Equal(1d, harness.Scene.GetEffectiveValue(root.Single("SpinnerRing"), StyleProperties.StrokeTrimEnd));
            Assert.Null(harness.Scene.TimeUntilNextFrame);
        }
    }

    private sealed class PercentCulture : IDisposable
    {
        private readonly CultureInfo previous = CultureInfo.CurrentCulture;

        public PercentCulture(int percentPositivePattern)
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.PercentPositivePattern = percentPositivePattern;
            CultureInfo.CurrentCulture = culture;
        }

        public void Dispose() => CultureInfo.CurrentCulture = previous;
    }

    private static double NativeTrim(double value)
    {
        (double Fraction, double Trim)[] keys = [(0.00833333377, 0.0000999999975), (0.25, 0.25), (0.5, 0.5), (0.75, 0.75), (0.983333349, 0.96666666), (0.991666675, 1), (1, 1)];
        if (value <= keys[0].Fraction) return keys[0].Trim;
        for (var index = 1; index < keys.Length; index++)
        {
            if (value > keys[index].Fraction) continue;
            var from = keys[index - 1];
            var to = keys[index];
            return from.Trim + (to.Trim - from.Trim) * (value - from.Fraction) / (to.Fraction - from.Fraction);
        }
        return 1;
    }
    private static RenderedImage Frame(RenderHarness harness, float scale)
    {
        harness.Host.RunPending();
        harness.Render(160, 160, scale);
        harness.Host.RunPending();
        return harness.Render(160, 160, scale);
    }
}
