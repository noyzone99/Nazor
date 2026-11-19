using Nazor.Animation;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;
using Nazor.Themes.NazorLeaf;
using Color = Nazor.Graphics.Color;

namespace Nazor.Rendering.Tests;

public sealed class BorderRingPage : ComponentBase
{
    public float Size { get; set; } = 48;
    public double Fraction { get; set; } = 1;
    public bool BorderShape { get; set; } = true;
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Surface");
        builder.AddAttribute(1, "style", "width: 120px; height: 120px; background: white;");
        builder.OpenElement(2, "Ring");
        builder.AddAttribute(3, "glyph", "ring");
        builder.AddAttribute(4, "ring-fraction", 1d);
        builder.AddAttribute(5, "style", FormattableString.Invariant($"position: absolute; left: 10px; top: 10px; width: {Size}px; height: {Size}px; color: black; stroke-width: 9.375%; stroke-radius: 41.666667%; border-radius: 37.5% 8.333333% 37.5% 8.333333%; stroke-trim-end: {Fraction}; ring-shape: {(BorderShape ? "border-box" : "circle")};"));
        builder.CloseElement();
        builder.CloseElement();
    }
}

public sealed class ProgressRingPage : ComponentBase
{
    public double? Value { get; set; } = 42;
    public ControlSize Size { get; set; } = ControlSize.Medium;
    public Intent Intent { get; set; } = Intent.Accent;
    public bool LeafShape { get; set; }
    public bool Mini { get; set; }
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Surface");
        builder.AddAttribute(1, "style", "width: 160px; height: 160px; background: white; padding: 16px;");
        if (Mini)
        {
            builder.OpenComponent<Button>(2);
            builder.AddComponentParameter(3, nameof(Button.Appearance), ControlAppearance.Subtle);
            builder.AddComponentParameter(4, nameof(Button.Leading), (RenderFragment)Ring);
            builder.CloseComponent();
        }
        else Ring(builder);
        builder.CloseElement();
    }
    private void Ring(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Spinner>(10);
        builder.AddComponentParameter(11, nameof(Spinner.Value), Value);
        builder.AddComponentParameter(12, nameof(Spinner.Size), Size);
        builder.AddComponentParameter(13, nameof(Spinner.Intent), Intent);
        builder.AddComponentParameter(14, nameof(Spinner.Class), LeafShape ? "leaf-ring" : null);
        builder.CloseComponent();
    }
}

public sealed class ProgressRingGeometryTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Border_box_paths_match_the_reference_leaf_and_trim_by_cumulative_length(float scale)
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<BorderRingPage>();
        var page = (BorderRingPage)root.Component;
        foreach (var size in new[] { 18f, 40f, 56f, 84f })
        {
            page.Size = size;
            page.Fraction = 1;
            page.BorderShape = true;
            page.Refresh();
            harness.Host.RunPending();
            var leaf = harness.Render(120, 120, scale);
            var stroke = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>());
            var full = stroke.Points;
            foreach (var (x, y) in new[] { (24f, 4f), (40, 4), (44, 8), (44, 26), (26, 44), (8, 44), (4, 40), (4, 22), (22, 4) })
                Assert.Contains(full, point => Math.Abs(point.X - (10 + x * size / 48)) < 0.001 && Math.Abs(point.Y - (10 + y * size / 48)) < 0.001);
            Assert.Equal(full[0], full[^1]);
            Assert.Equal(9.375f * size / 100, stroke.Thickness, 4);
            var marker = new Point(10 + 44 * size / 48, 10 + 10 * size / 48);
            Assert.Equal(Color.Black, Pixel(leaf, marker, scale));
            foreach (var fraction in new[] { 0.25, 0.42, 0.75 })
            {
                page.Fraction = fraction;
                page.Refresh();
                harness.Host.RunPending();
                harness.Render(120, 120, scale);
                var trimmed = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>());
                Assert.Equal(fraction, Length(trimmed.Points) / Length(full), 4);
                Assert.Equal(full[0], trimmed.Points[0]);
            }
            page.Fraction = 0;
            page.Refresh();
            harness.Host.RunPending();
            harness.Render(120, 120, scale);
            Assert.Empty(harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>());
            page.Fraction = 1;
            page.BorderShape = false;
            page.Refresh();
            harness.Host.RunPending();
            var circle = harness.Render(120, 120, scale);
            Assert.Equal(Color.White, Pixel(circle, marker, scale));
        }
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Basic_keeps_a_static_quarter_while_the_Fluent_timeline_can_reach_a_half_ring(float scale)
    {
        using var harness = new RenderHarness();
        var root = harness.Host.Render<ProgressRingPage>();
        var page = (ProgressRingPage)root.Component;
        page.Value = null;
        page.Refresh();
        harness.Host.RunPending();
        harness.Render(160, 160, scale);
        var ring = root.Single("SpinnerRing");
        Assert.Equal(1d, ring.Attributes["ring-fraction"]);
        Assert.Equal(0.25, harness.Scene.GetEffectiveValue(ring, StyleProperties.StrokeTrimEnd));
        var quarter = harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>().Last();
        Assert.InRange(AngleSweep(quarter.Points), Math.PI / 2 - 0.001, Math.PI / 2 + 0.001);

        var clock = new ManualAnimationClock();
        using var animated = new RenderHarness(clock: clock);
        animated.Scene.Styles.Theme = Nazor.Themes.Fluent.Themes.Fluent;
        animated.Scene.Transitions.ReducedMotion = false;
        animated.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: false);
        var animatedRoot = animated.Host.Render<ProgressRingPage>();
        var animatedPage = (ProgressRingPage)animatedRoot.Component;
        animatedPage.Value = null;
        animatedPage.Refresh();
        animated.Host.RunPending();
        animated.Render(160, 160, scale);
        clock.Advance(1000);
        animated.Render(160, 160, scale);
        var half = animated.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>().Last();
        Assert.InRange(AngleSweep(half.Points), Math.PI - 0.001, Math.PI + 0.001);
        var bounds = animated.Scene.GetBounds(animatedRoot.Single("SpinnerIndicator"));
        Assert.Equal(bounds.Width * 0.09375f, half.Thickness, 4);
    }

    [Theory]
    [InlineData(ColorScheme.Light, false, 1f)]
    [InlineData(ColorScheme.Light, false, 1.5f)]
    [InlineData(ColorScheme.Light, false, 2f)]
    [InlineData(ColorScheme.Dark, false, 1f)]
    [InlineData(ColorScheme.Dark, false, 1.5f)]
    [InlineData(ColorScheme.Dark, false, 2f)]
    [InlineData(ColorScheme.Light, true, 1f)]
    [InlineData(ColorScheme.Light, true, 1.5f)]
    [InlineData(ColorScheme.Light, true, 2f)]
    public void Leaf_tracks_leave_real_gaps_and_completion_uses_a_styleable_vector(ColorScheme scheme, bool forced, float scale)
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Theme = NazorLeafTheme.Theme;
        harness.Scene.Styles.Preferences = new MediaPreferences(scheme, ReducedMotion: true, ForcedColors: forced);
        var root = harness.Host.Render<ProgressRingPage>();
        var page = (ProgressRingPage)root.Component;
        foreach (var (size, mini) in new[] { (ControlSize.Small, false), (ControlSize.Medium, false), (ControlSize.Large, false), (ControlSize.Medium, true) })
        foreach (var shape in new[] { false, true })
        {
            page.Size = size;
            page.Mini = mini;
            page.LeafShape = shape;
            page.Value = 42;
            page.Refresh();
            harness.Host.RunPending();
            var image = harness.Render(160, 160, scale);
            var ring = root.Single("SpinnerRing");
            var track = root.Single("SpinnerTrack");
            var expectedSize = mini ? 18 : size == ControlSize.Small ? 40 : size == ControlSize.Large ? 84 : 56;
            var bounds = harness.Scene.GetBounds(root.Single("SpinnerIndicator"));
            Assert.Equal(expectedSize, bounds.Width);
            Assert.Equal(0.42, harness.Scene.GetEffectiveValue(ring, StyleProperties.StrokeTrimEnd), 5);
            Assert.Equal(0.49, harness.Scene.GetEffectiveValue(track, StyleProperties.StrokeTrimStart), 5);
            Assert.Equal(0.93, harness.Scene.GetEffectiveValue(track, StyleProperties.StrokeTrimEnd), 5);
            var strokes = harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>().ToArray();
            Assert.Equal(2, strokes.Length);
            if (!shape && !mini)
            {
                var radius = bounds.Width * 20 / 48;
                foreach (var fraction in new[] { 0.455, 0.965 })
                {
                    var angle = -Math.PI / 2 + Math.Tau * fraction;
                    var point = new Point(bounds.X + bounds.Width / 2 + radius * (float)Math.Cos(angle), bounds.Y + bounds.Height / 2 + radius * (float)Math.Sin(angle));
                    Assert.Equal(Color.White, Pixel(image, point, scale));
                }
            }
            page.Value = 0;
            page.Refresh();
            harness.Host.RunPending();
            harness.Render(160, 160, scale);
            Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>());
            Assert.Equal(0d, harness.Scene.GetEffectiveValue(root.Single("SpinnerTrack"), StyleProperties.StrokeTrimStart));
            Assert.Equal(1d, harness.Scene.GetEffectiveValue(root.Single("SpinnerTrack"), StyleProperties.StrokeTrimEnd));
            page.Value = 100;
            page.Refresh();
            harness.Host.RunPending();
            harness.Render(160, 160, scale);
            var check = root.Single("SpinnerCheck");
            Assert.Equal("check", check.Attributes["glyph"]);
            Assert.Equal(mini ? Display.None : Display.Block, harness.Scene.GetStyle(check).Display);
            Assert.Equal(mini ? 1 : 2, harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>().Count());
            Assert.DoesNotContain(harness.Scene.BuildDisplayList().Commands.OfType<DrawText>(), text => text.Layout.Text.Contains('\uE004'));
        }
    }

    [Theory]
    [InlineData(Intent.Accent)]
    [InlineData(Intent.Success)]
    [InlineData(Intent.Warning)]
    [InlineData(Intent.Danger)]
    public void Leaf_value_and_completion_vector_follow_the_selected_intent(Intent intent)
    {
        using var harness = new RenderHarness();
        harness.Scene.Styles.Theme = NazorLeafTheme.Theme;
        harness.Scene.Styles.Preferences = new MediaPreferences(ColorScheme.Light, ReducedMotion: true);
        var root = harness.Host.Render<ProgressRingPage>();
        var page = (ProgressRingPage)root.Component;
        page.Intent = intent;
        page.Refresh();
        harness.Host.RunPending();
        harness.Render(160, 160, 1);
        var ringColor = harness.Scene.GetStyle(root.Single("SpinnerRing")).Color;
        var drawn = harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>().Last();
        Assert.Equal(new Color(ringColor.R, ringColor.G, ringColor.B, ringColor.A), drawn.Color);
        page.Value = 100;
        page.Refresh();
        harness.Host.RunPending();
        harness.Render(160, 160, 1);
        var checkColor = harness.Scene.GetStyle(root.Single("SpinnerCheck")).Color;
        Assert.Equal(ringColor, checkColor);
        var content = harness.Scene.GetContentBounds(root.Single("SpinnerCheck"));
        Assert.InRange(Math.Abs(content.Width - 56 * 0.42f), 0, 1);
        Assert.Equal(2.4f / 24 * Math.Min(content.Width, content.Height), harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>().Last().Thickness, 4);
    }

    private static double Length(IReadOnlyList<Point> points) => Enumerable.Range(1, points.Count - 1).Sum(index => Math.Sqrt(Math.Pow(points[index].X - points[index - 1].X, 2) + Math.Pow(points[index].Y - points[index - 1].Y, 2)));
    private static double AngleSweep(IReadOnlyList<Point> points)
    {
        var radius = (points[0].X - points[^1].X) == 0 && (points[0].Y - points[^1].Y) == 0 ? Length(points) / Math.Tau : 0;
        if (radius > 0) return Math.Tau;
        var middle = points[points.Count / 2];
        var first = points[0];
        var last = points[^1];
        var a = Math.Sqrt(Math.Pow(first.X - middle.X, 2) + Math.Pow(first.Y - middle.Y, 2));
        var b = Math.Sqrt(Math.Pow(last.X - middle.X, 2) + Math.Pow(last.Y - middle.Y, 2));
        var c = Math.Sqrt(Math.Pow(first.X - last.X, 2) + Math.Pow(first.Y - last.Y, 2));
        return Math.Tau - 2 * Math.Acos(Math.Clamp((a * a + b * b - c * c) / (2 * a * b), -1, 1));
    }
    private static Color Pixel(RenderedImage image, Point point, float scale) => image.GetPixel((int)MathF.Floor(point.X * scale), (int)MathF.Floor(point.Y * scale));
}
