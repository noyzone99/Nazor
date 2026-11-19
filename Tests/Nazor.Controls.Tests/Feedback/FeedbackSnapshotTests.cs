using Nazor.Controls.Tests.Feedback.Fixtures;

namespace Nazor.Controls.Tests.Feedback;

// Renders every feedback fixture with the Basic theme. Set NAZOR_SNAPSHOT_DIR to also save the images for inspection.
[Collection("Controls")]
public sealed class FeedbackSnapshotTests
{
    [Fact]
    public void Every_fixture_renders_with_the_basic_theme()
    {
        Snapshot<ProgressProbe>("progress");
        Snapshot<StatusProbe>("status");
        Snapshot<MessageProbe>("messages");
        ToastProbe.Clock = new FeedbackClock();
        Snapshot<ToastProbe>("toasts", h =>
        {
            ToastSender.Last!.Show(new Primitives.ToastOptions { Title = "Saved", Message = "The file was saved.", Severity = Severity.Success, ActionText = "Undo", Action = () => Task.CompletedTask });
            ToastSender.Last!.Show("Connection lost", Severity.Error);
            h.Frame();
        });
    }

    private static void Snapshot<T>(string name, Action<ControlHarness>? setup = null) where T : IComponent
    {
        using var h = new ControlHarness(width: 640, height: 900);
        h.Render<T>();
        setup?.Invoke(h);
        var image = h.Image();
        Assert.True(image.Width > 0);
        if (Environment.GetEnvironmentVariable("NAZOR_SNAPSHOT_DIR") is { Length: > 0 } directory)
            File.WriteAllBytes(Path.Combine(directory, "feedback-" + name + ".png"), h.Graphics.EncodePng(image));
    }
}
