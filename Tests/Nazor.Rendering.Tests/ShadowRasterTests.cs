using Nazor.Graphics;
using Nazor.Skia;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace Nazor.Rendering.Tests;

public sealed class ShadowRasterTests
{
    public static TheoryData<float, float, bool, bool, bool, float, bool> PartialCases
    {
        get
        {
            var cases = new TheoryData<float, float, bool, bool, bool, float, bool>();
            foreach (var scale in new[] { .75f, 1f, 1.25f, 1.5f, 2f })
            foreach (var blur in new[] { 0f, 3f, 6f, 12f })
            foreach (var inset in new[] { false, true })
            foreach (var opacity in new[] { false, true })
            foreach (var translate in new[] { false, true })
            foreach (var radius in new[] { 0f, 10f })
            foreach (var halo in new[] { false, true }) cases.Add(scale, blur, inset, opacity, translate, radius, halo);
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(PartialCases))]
    public void Damage_crossing_shadow_corners_preserves_full_repaint_pixels(float scale, float blur, bool inset, bool opacity, bool translate, float radius, bool halo)
    {
        using var renderer = new SkiaRenderer();
        var rect = new Rect(20.25f, 20.5f, 60, 50);
        var commands = new List<DrawCommand>();
        if (translate) commands.Add(new PushTranslate(.15f, .35f));
        if (opacity) commands.Add(new PushOpacity(.6f, new Rect(0, 0, 120, 100)));
        commands.Add(new DrawShadow(rect, radius, new(2, 3), blur, 1, new(10, 30, 50, 160), inset));
        if (opacity) commands.Add(new Pop());
        if (translate) commands.Add(new Pop());
        var changed = halo ? new Rect(5, 5, 24, 14.5f) : new Rect(5, 5, 29.25f, 20);
        var before = new DisplayList(new Size(120, 100), [new FillRectangle(changed, 0, Color.Black), ..commands]);
        var after = new DisplayList(before.Size, [new FillRectangle(changed, 0, Color.White), ..commands]);
        var expected = renderer.RenderToImage(after, scale, Color.White);
        var actual = renderer.RenderToImage(before, scale, Color.White).Pixels;
        var logical = DisplayListDamage.Compute(before, after, scale);
        var damage = logical is null ? null : DisplayListDamage.ToPixels(logical, scale, expected.Width, expected.Height);
        var pinned = GCHandle.Alloc(actual, GCHandleType.Pinned);
        try { renderer.Render(after, pinned.AddrOfPinnedObject(), expected.Width, expected.Height, expected.Stride, scale, Color.White, damage); }
        finally { pinned.Free(); }
        Assert.Equal(expected.Pixels, actual);
    }

    public static TheoryData<float, float, bool> Cases
    {
        get
        {
            var cases = new TheoryData<float, float, bool>();
            foreach (var scale in new[] { .75f, 1f, 1.25f, 1.5f, 2f })
            foreach (var blur in new[] { 0f, 3f, 6f })
            foreach (var fractional in new[] { false, true }) cases.Add(scale, blur, fractional);
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Full_shadow_raster_preserves_the_existing_appearance(float scale, float blur, bool fractional)
    {
        var rect = new Rect(fractional ? 20.25f : 20, fractional ? 20.5f : 20, 60, 50);
        var shadow = new DrawShadow(rect, 6, new(0, 2), blur, 1, new(10, 30, 50, 160), false);
        using var renderer = new SkiaRenderer();
        var actual = renderer.RenderToImage(new(new Size(120, 100), [shadow]), scale, Color.White);
        using var expected = new SKBitmap(new SKImageInfo(actual.Width, actual.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(expected);
        canvas.Clear(SKColors.White);
        canvas.Scale(scale);
        using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(10, 30, 50, 160) };
        if (blur > 0) paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blur / 2);
        using var box = new SKRoundRect(new SKRect(rect.X, rect.Y, rect.Right, rect.Bottom), 6, 6);
        var outside = rect.Offset(0, 2).Inflate(1);
        using var shape = new SKRoundRect(new SKRect(outside.X, outside.Y, outside.Right, outside.Bottom), 7, 7);
        canvas.ClipRoundRect(box, SKClipOperation.Difference, antialias: true);
        canvas.DrawRoundRect(shape, paint);
        Assert.Equal(expected.Bytes, actual.Pixels);
    }
}
