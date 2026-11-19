using Nazor.Graphics;
using Nazor.DevTools;
using Nazor.Diagnostics;
using Nazor.Samples.Counter;
using Nazor.Testing;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class PartialPresentTests
{
    [Fact]
    public void A_click_redraws_the_count_and_a_resize_redraws_everything()
    {
        using var harness = new WindowHarness(typeof(Counter), new ApplicationRuntimeSettings { Width = 320, Height = 200 });
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        Assert.Null(harness.Read(w => w.LastFrameDamage));

        var (frames, button, size) = harness.Read(w => (w.FrameCount, w.Scene.GetBounds(w.Root.Single("Button")), w.PlatformWindow.ClientSize));
        harness.Click(new Point(button.X + button.Width / 2, button.Y + button.Height / 2));
        harness.WaitUntil(w => w.FrameCount > frames && w.Root.Single("TextBlock").Text() == "Count: 1", "the frame of the new count");
        harness.WaitUntil(w => w.LastFrameDamage is { Count: > 0 } damage && damage.Sum(r => r.Width * r.Height) < size.Width * size.Height * w.PlatformWindow.Scale * w.PlatformWindow.Scale / 2,
            "a frame that draws part of the window");

        harness.WaitUntil(w => w.Scene.Transitions.TimeUntilNextFrame is null, "the end of the transitions");
        frames = harness.Read(w => w.FrameCount);
        harness.Read(w =>
        {
            w.EnableDiagnostics();
            w.PlatformWindow.Resize(new Size(340, 210));
            return true;
        });
        harness.WaitUntil(w => w.GetDiagnostics()!.Frames.Any(frame => frame.Number > frames && frame.Cause.HasFlag(FrameCause.Resize) && frame.Damage is null), "a frame that draws the new buffer");
        Assert.Empty(harness.Read(w => w.Exceptions));
    }
}
