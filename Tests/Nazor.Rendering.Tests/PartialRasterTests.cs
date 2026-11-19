using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Skia;

namespace Nazor.Rendering.Tests;

public sealed class PartialRasterTests
{
    public static TheoryData<float, float, byte, bool> ShadowHoverCases
    {
        get
        {
            var cases = new TheoryData<float, float, byte, bool>();
            foreach (var scale in new[] { 1f, 1.25f, 1.5f, 2f })
            foreach (var blur in new[] { 3f, 6f, 12f })
            foreach (var alpha in new byte[] { 64, 255 })
            foreach (var fractional in new[] { false, true }) cases.Add(scale, blur, alpha, fractional);
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(ShadowHoverCases))]
    public void Hover_repaint_of_one_card_preserves_adjacent_shadows(float scale, float blur, byte alpha, bool fractional)
    {
        using var renderer = new SkiaRenderer();
        var commands = new List<DrawCommand>();
        for (var column = 0; column < 6; column++)
        {
            var rect = new Rect((fractional ? 10.25f : 10) + column * 100, fractional ? 10.5f : 10, 92, 70);
            commands.Add(new DrawShadow(rect, 6, new(0, 1), blur, 0, new(0, 0, 0, alpha), false));
            commands.Add(new FillRectangle(rect, 6, Color.White));
        }
        var before = new DisplayList(new(620, 100), commands.ToArray());
        commands[1] = ((FillRectangle)commands[1]) with { Color = new(230, 240, 255, 255) };
        var after = new DisplayList(before.Size, commands.ToArray());
        var partial = renderer.RenderToImage(before, scale, Color.White);
        var expected = renderer.RenderToImage(after, scale, Color.White);
        var damage = DisplayListDamage.ToPixels(DisplayListDamage.Compute(before, after, scale)!, scale, expected.Width, expected.Height);
        var pinned = GCHandle.Alloc(partial.Pixels, GCHandleType.Pinned);
        try { renderer.Render(after, pinned.AddrOfPinnedObject(), expected.Width, expected.Height, expected.Stride, scale, Color.White, damage); }
        finally { pinned.Free(); }
        Assert.Equal(expected.Pixels, partial.Pixels);
        if (blur == 3) Assert.True(damage.Sum(rect => (long)rect.Width * rect.Height) < expected.Width * expected.Height / 3);
    }

    [Theory]
    [InlineData(0.75f, false)]
    [InlineData(1f, false)]
    [InlineData(1.25f, false)]
    [InlineData(1.5f, false)]
    [InlineData(2f, false)]
    [InlineData(1f, true)]
    [InlineData(1.25f, true)]
    [InlineData(1.5f, true)]
    public void Interior_and_straight_edge_damage_does_not_spread_through_adjacent_rounded_cards(float scale, bool edge)
    {
        using var renderer = new SkiaRenderer();
        var cards = new List<DrawCommand>();
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 6; column++)
        {
            var rect = new Rect(10 + column * 100, 10 + row * 80, 92, 70);
            cards.Add(new DrawShadow(rect, 6, new(0, 1), 3, 0, new(0, 0, 0, 64), false));
            cards.Add(new FillRectangle(rect, 6, Color.White));
            cards.Add(new DrawBorder(rect, 6, new(1, 1, 1, 1), Color.Black));
        }
        var changed = new Rect(edge ? 8 : 36, 38, 20, 8);
        var before = new DisplayList(new(620, 340), [..cards, new FillRectangle(changed, 0, Color.Black)]);
        var after = new DisplayList(before.Size, [..cards, new FillRectangle(changed, 0, Color.White)]);
        var partial = renderer.RenderToImage(before, scale, Color.White);
        var expected = renderer.RenderToImage(after, scale, Color.White);
        var damage = DisplayListDamage.ToPixels(DisplayListDamage.Compute(before, after, scale)!, scale, expected.Width, expected.Height);
        var pinned = GCHandle.Alloc(partial.Pixels, GCHandleType.Pinned);
        try { renderer.Render(after, pinned.AddrOfPinnedObject(), expected.Width, expected.Height, expected.Stride, scale, Color.White, damage); }
        finally { pinned.Free(); }
        Assert.Equal(expected.Pixels, partial.Pixels);
        Assert.True(damage.Sum(rect => (long)rect.Width * rect.Height) < expected.Width * expected.Height / 10);
    }

    public static TheoryData<string, float, float, bool> Cases
    {
        get
        {
            var cases = new TheoryData<string, float, float, bool>();
            foreach (var kind in new[] { "fill", "border", "both", "circle", "circle-gradient", "circle-both" })
                foreach (var scale in new[] { 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f })
                    foreach (var bottom in new[] { 42.5f, 44f })
                    {
                        cases.Add(kind, scale, bottom, false);
                        if (kind.StartsWith("circle", StringComparison.Ordinal)) cases.Add(kind, scale, bottom, true);
                    }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Damage_crossing_rounded_corners_preserves_the_pixels_of_a_full_repaint(string kind, float scale, float bottom, bool fractional)
    {
        using var renderer = new SkiaRenderer();
        var circle = kind.StartsWith("circle", StringComparison.Ordinal);
        var rect = new Rect(fractional ? 16.15f : 16, fractional ? 43.05f : 43, circle ? 32 : 288, 32);
        DrawCommand[] commands = kind switch
        {
            "circle-both" => [new FillRectangle(rect.Deflate(new Thickness(1, 1, 1, 1)), 15, new Color(178, 178, 178, 160)),
                new DrawBorder(rect, 16, new Thickness(1, 1, 1, 1), new Color(178, 178, 178, 160))],
            "circle" => [new DrawBorder(rect, 16, new Thickness(1, 1, 1, 1), new Color(178, 178, 178, 160))],
            "circle-gradient" => [new DrawBorder(rect, 16, new Thickness(1, 1, 1, 1), Color.Transparent)
            {
                Brush = new LinearGradientBrush(new Point(16, 43), new Point(16, 75), [new GradientStop(0, new Color(200, 100, 50, 160)), new GradientStop(1, Color.Transparent)]),
            }],
            "fill" => [new FillRectangle(rect, 6, new Color(0, 120, 212, 255))],
            "border" => [new DrawBorder(rect, 6, new Thickness(1, 1, 1, 1), new Color(178, 178, 178, 255))],
            _ => [new FillRectangle(rect, 6, new Color(0, 120, 212, 255)), new DrawBorder(rect, 6, new Thickness(1, 1, 1, 1), new Color(178, 178, 178, 255))],
        };
        var size = new Size(320, 240);
        var changed = new Rect(6, 6, 308, bottom - 6);
        var first = new DisplayList(size, [new FillRectangle(changed, 0, Color.Black), ..commands]);
        var list = new DisplayList(size, [new FillRectangle(changed, 0, Color.White), ..commands]);
        var background = Color.White;
        var expected = renderer.RenderToImage(list, scale, background);
        var partial = renderer.RenderToImage(first, scale, background).Pixels;
        var damage = DisplayListDamage.ToPixels(DisplayListDamage.Compute(first, list, scale)!, scale, expected.Width, expected.Height);
        var pinned = GCHandle.Alloc(partial, GCHandleType.Pinned);
        try
        {
            renderer.Render(list, pinned.AddrOfPinnedObject(), expected.Width, expected.Height, expected.Stride, scale, background,
                damage);
        }
        finally
        {
            pinned.Free();
        }
        if (!expected.Pixels.SequenceEqual(partial))
            Assert.Fail($"Partial raster differs: {string.Join(", ", damage)}; bytes: " + string.Join(", ", Enumerable.Range(0, partial.Length)
                .Where(i => expected.Pixels[i] != partial[i]).Take(12).Select(i => $"({i / 4 % expected.Width},{i / expected.Stride}):{expected.Pixels[i]}/{partial[i]}")));
        Assert.True(damage.Sum(r => r.Width * r.Height) < expected.Width * expected.Height / 2);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void Matching_circular_fill_and_border_have_no_translucent_seam(float scale)
    {
        using var renderer = new SkiaRenderer();
        var color = new Color(0, 0, 0, 128);
        var list = new DisplayList(new Size(32, 32),
        [
            new FillRectangle(new Rect(5, 5, 18, 18), 9, color),
            new DrawBorder(new Rect(4, 4, 20, 20), 10, new Thickness(1, 1, 1, 1), color),
        ]);
        var image = renderer.RenderToImage(list, scale, Color.Transparent);
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
        {
            var pixel = image.GetPixel(x, y);
            Assert.InRange(pixel.A, (byte)0, (byte)128);
            var dx = (x + 0.5f) / scale - 14;
            var dy = (y + 0.5f) / scale - 14;
            if (dx * dx + dy * dy <= 9.2f * 9.2f) Assert.Equal(128, pixel.A);
        }
    }

    [Fact]
    public void Damage_expansion_follows_overlapping_curves_without_repainting_distant_drawings()
    {
        var upper = new Rect(20, 20, 40, 40);
        var lower = new Rect(56, 56, 40, 40);
        var distant = new Rect(200, 20, 40, 40);
        var marker = new Rect(54, 52, 2, 3);
        DrawCommand[] curves = [new FillRectangle(upper, 6, Color.Black), new FillRectangle(lower, 6, Color.Black), new FillRectangle(distant, 6, Color.Black)];
        var first = new DisplayList(new Size(320, 240), [..curves, new FillRectangle(marker, 0, Color.Black)]);
        var second = new DisplayList(first.Size, [..curves, new FillRectangle(marker, 0, Color.White)]);
        var damage = DisplayListDamage.Compute(first, second)!;
        Assert.Contains(damage, r => r.Intersect(new Rect(54, 54, 6, 6)) == new Rect(54, 54, 6, 6));
        Assert.Contains(damage, r => r.Intersect(new Rect(56, 56, 6, 6)) == new Rect(56, 56, 6, 6));
        Assert.All(damage, r => Assert.True(r.Intersect(distant).IsEmpty));
    }

    [Fact]
    public void Scale_changes_require_a_full_repaint_and_invalid_scales_are_rejected()
    {
        var list = new DisplayList(new Size(320, 240), []);
        var damage = new DisplayListDamage();
        Assert.Null(damage.Next(list, 1));
        Assert.Empty(damage.Next(list, 1)!);
        Assert.Null(damage.Next(list, 1.5f));
        Assert.Empty(damage.Next(list, 1.5f)!);
        foreach (var scale in new[] { 0, -1, float.NaN, float.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => damage.Next(list, scale));
    }
}
