using Nazor.Graphics;
using Nazor.Samples.Counter;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class PresentationWindowTests
{
    private static readonly ApplicationRuntimeSettings Options = new()
    {
        Width = 320, Height = 240, ReducedMotion = true, ColorScheme = Style.ColorScheme.Light,
        Rendering = RenderingMode.Cpu,
    };

    [Fact]
    public void Typing_presents_the_text_without_a_resize_or_capture_repaint()
    {
        using var harness = new WindowHarness(typeof(Form), Options);
        Prepare(harness);
        var box = harness.Read(w => w.Scene.GetContentBounds(w.Root.Elements("TextFieldInput").Single(e => e.Id == "free-input")));
        harness.Click(new Point(box.X + 4, box.Y + box.Height / 2));
        harness.WaitUntil(w => w.Input.FocusedTextBox?.Id == "free-input", "text box focus");
        var frames = harness.Read(w => w.FrameCount);
        foreach (var c in "TEST") harness.Character(c);
        harness.WaitUntil(w => w.Input.FocusedTextBox is { } focused &&
            focused.Attributes.GetValueOrDefault("value") is "TEST" && w.Scene.GetEditableLayout(focused)?.Text == "TEST" &&
            w.FrameCount > frames, "typed text and its frame");

        Assert.NotNull(harness.Read(w => w.LastFrameDamage));
        AssertPresented(harness);
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void A_click_presents_the_updated_counter_without_a_capture_repaint()
    {
        using var harness = new WindowHarness(typeof(Counter), Options);
        Prepare(harness);
        var button = harness.Read(w => w.Scene.GetBounds(w.Root.Single("Button")));
        var frames = harness.Read(w => w.FrameCount);
        harness.Click(new Point(button.X + button.Width / 2, button.Y + button.Height / 2));
        harness.WaitUntil(w => w.FrameCount > frames && w.Scene.BuildDisplayList().Commands.OfType<DrawText>()
            .Any(text => text.Layout.Text == "Count: 1"), "updated counter and its frame");

        Assert.NotNull(harness.Read(w => w.LastFrameDamage));
        AssertPresented(harness);
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    private static void Prepare(WindowHarness harness)
    {
        Assert.True(SetWindowPos(harness.Handle, 0, 30, 30, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE));
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        AssertPresented(harness);
    }

    private static void AssertPresented(WindowHarness harness)
    {
        var differingPixels = harness.Read(w =>
        {
            using var graphics = new Skia.SkiaRenderer();
            var expected = graphics.RenderToImage(w.Scene.BuildDisplayList(), w.Scene.Scale, w.Scene.Background);
            var actual = ScreenCapture.CapturePresented(harness.Handle);
            Assert.Equal((expected.Width, expected.Height), (actual.Width, actual.Height));
            var different = 0;
            var firstDifference = "";
            for (var i = 0; i < expected.Pixels.Length; i += 4)
                if (Math.Abs(expected.Pixels[i] - actual.Pixels[i]) > 1 ||
                    Math.Abs(expected.Pixels[i + 1] - actual.Pixels[i + 1]) > 1 ||
                    Math.Abs(expected.Pixels[i + 2] - actual.Pixels[i + 2]) > 1)
                {
                    different++;
                    if (firstDifference.Length == 0) firstDifference = $"pixel ({i / 4 % expected.Width}, {i / 4 / expected.Width}), expected {expected.GetPixel(i / 4 % expected.Width, i / 4 / expected.Width)}, actual {actual.GetPixel(i / 4 % expected.Width, i / 4 / expected.Width)}";
                }
            return (different, firstDifference);
        });
        Assert.True(differingPixels.different == 0, $"{differingPixels.different} differing pixels; {differingPixels.firstDifference}");
    }
}
