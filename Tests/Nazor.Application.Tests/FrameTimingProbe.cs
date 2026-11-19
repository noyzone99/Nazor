using Nazor.Graphics;
using Nazor.Samples.Counter;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class FrameTimingProbe(ITestOutputHelper output)
{
    [Fact]
    public void Counter_frame_durations_are_recorded()
    {
        using var harness = new WindowHarness(typeof(Counter), new ApplicationRuntimeSettings { Width = 320, Height = 200 });
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        var first = harness.Read(w => w.LastFrameDuration);
        var durations = new List<TimeSpan>();
        for (var i = 0; i < 20; i++)
        {
            var frames = harness.Read(w => w.FrameCount);
            harness.Read(w =>
            {
                w.PlatformWindow.Resize(new Size(330 + i * 10, 205 + i * 5));
                return true;
            });
            harness.WaitUntil(w => w.FrameCount > frames, "frame after resize");
            durations.Add(harness.Read(w => w.LastFrameDuration));
        }
        var line = $"Counter frame: first {first.TotalMilliseconds:0.00} ms, after resize median {durations.Order().ElementAt(durations.Count / 2).TotalMilliseconds:0.00} ms, max {durations.Max().TotalMilliseconds:0.00} ms";
        output.WriteLine(line);
        Console.WriteLine(line);
    }
}
