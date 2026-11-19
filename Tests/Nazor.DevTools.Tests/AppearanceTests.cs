using Nazor.Style;

namespace Nazor.DevTools.Tests;

public sealed class AppearanceTests
{
    [Fact]
    public void Preference_defaults_to_system_and_round_trips_all_schemes_without_partial_files()
    {
        Assert.NotEqual(Path.GetFullPath(Nazor.DevTools.Protocol.LocalDevTools.DiscoveryDirectory),
            Path.GetFullPath(Path.GetDirectoryName(InspectorPreferences.FilePath)!));
        var directory = Path.Combine(Path.GetTempPath(), "nazor-appearance-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "appearance.txt");
        try
        {
            Assert.Equal(ColorScheme.System, InspectorPreferences.Read(path));
            foreach (var scheme in Enum.GetValues<ColorScheme>())
            {
                Assert.True(InspectorPreferences.Save(path, scheme));
                Assert.Equal(scheme, InspectorPreferences.Read(path));
                Assert.Single(Directory.GetFiles(directory));
            }
            foreach (var invalid in new[] { "broken", "999", "" })
            {
                File.WriteAllText(path, invalid);
                Assert.Equal(ColorScheme.System, InspectorPreferences.Read(path));
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => InspectorPreferences.Save(path, (ColorScheme)99));
            Assert.False(InspectorPreferences.Save(Path.Combine(path, "blocked.txt"), ColorScheme.Dark));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void Window_title_follows_target_and_keeps_app_name_after_exit_until_cleared_or_replaced()
    {
        var store = new InspectionStore();
        Assert.Equal("Nazor DevTools", store.WindowTitle);
        var target = CapturedData.Target();
        store.Connecting(target.Identity, " My Workspace "); store.Bind(target);
        Assert.Equal("Nazor DevTools - My Workspace", store.WindowTitle);
        store.Disconnected("Target exited");
        Assert.Equal("Nazor DevTools - My Workspace", store.WindowTitle);
        store.Connecting(CapturedData.Target(session: "other").Identity, "Other App");
        Assert.Equal("Nazor DevTools - Other App", store.WindowTitle);
        store.Connecting(target.Identity);
        Assert.Equal("Nazor DevTools - Process 42", store.WindowTitle);
        store.Clear();
        Assert.Equal("Nazor DevTools", store.WindowTitle);
    }
}
