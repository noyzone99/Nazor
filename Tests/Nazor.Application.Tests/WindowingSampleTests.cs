using Microsoft.Extensions.DependencyInjection;
using Nazor.Controls;
using Nazor.Input;
using Nazor.Samples.Windowing;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class WindowingSampleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sample_dialog_returns_a_value_and_Esc_cancels_with_the_owner_reenabled(bool custom)
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<Nazor.Samples.Windowing.App>(new() { Activate = false }));
        var main = harness.Window<Nazor.Samples.Windowing.App>();
        try
        {
            var label = custom ? "Dialog with a custom title bar and buttons" : "Dialog with a system title bar";
            var pending = Click(main, label);
            var dialog = harness.Window<NameDialog>();
            SaveSnapshot(dialog, custom ? "custom-dialog" : "system-dialog");
            ApplicationHarness.On(dialog, () =>
            {
                Assert.True(dialog.IsDialog);
                Assert.False(IsWindowEnabled(main.PlatformWindow.Handle));
                Assert.Equal(main.PlatformWindow.Handle, GetWindow(dialog.PlatformWindow.Handle, GW_OWNER));
                Assert.Equal(custom ? 1 : 0, dialog.Root.Elements("TitleBar").Count());
                Assert.False(dialog.Capabilities.CanMinimize);
                Assert.False(dialog.Capabilities.CanMaximize);
                Assert.False(dialog.Capabilities.CanEnterFullScreen);
                Assert.True(dialog.Root.Component.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(NameDialog.Name)] = "  New name  ",
                })).IsCompletedSuccessfully);
                return true;
            });
            await Click(dialog, "Confirm");
            await pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(ApplicationHarness.On(main, () => IsWindowEnabled(main.PlatformWindow.Handle)));
            Assert.Contains("Dialog confirmed: New name", ApplicationHarness.On(main, () => main.Root.Text()));
            Assert.Equal("New name", ApplicationHarness.On(main, () => main.Title));

            pending = Click(main, label);
            dialog = harness.Window<NameDialog>();
            ApplicationHarness.Key(dialog, 0x1B);
            await pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.Contains("Dialog canceled", ApplicationHarness.On(main, () => main.Root.Text()));
            Assert.True(ApplicationHarness.On(main, () => IsWindowEnabled(main.PlatformWindow.Handle)));
            Assert.Empty(ApplicationHarness.On(main, () => main.Exceptions));
        }
        finally { CloseMain(harness, main); }
    }

    [Fact]
    public async Task Tool_window_is_owned_modeless_and_preserves_notes_when_hidden()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<Nazor.Samples.Windowing.App>(new() { Activate = false }));
        var main = harness.Window<Nazor.Samples.Windowing.App>();
        try
        {
            ApplicationHarness.On(main, () => { ((Nazor.Samples.Windowing.App)main.Root.Component).ToggleSystemButtons(); return true; });
            SaveSnapshot(main, "main-custom");
            await Click(main, "Open / activate the custom tool window");
            var tool = harness.Window<ToolWindow>();
            SaveSnapshot(tool, "tool-window");
            ApplicationHarness.On(tool, () =>
            {
                Assert.False(tool.IsDialog);
                Assert.True(IsWindowEnabled(main.PlatformWindow.Handle));
                Assert.Equal(main.PlatformWindow.Handle, GetWindow(tool.PlatformWindow.Handle, GW_OWNER));
                Assert.Single(tool.Root.Elements("TitleBar"));
                var box = Assert.IsType<Nazor.Controls.TextArea>(tool.Root.Single("TextArea").Owner);
                Assert.True(box.ValueChanged.InvokeAsync("Preserved notes").IsCompletedSuccessfully);
                Assert.True(tool.Root.Component.SetParametersAsync(ParameterView.Empty).IsCompletedSuccessfully);
                return true;
            });
            await Click(main, "Hide / show the tool window");
            Assert.False(ApplicationHarness.On(tool, () => IsWindowVisible(tool.PlatformWindow.Handle)));
            await Click(main, "Open / activate the custom tool window");
            Assert.Same(tool, harness.Window<ToolWindow>());
            Assert.Equal("Preserved notes", ApplicationHarness.On(tool, () => Assert.IsType<Nazor.Controls.TextArea>(tool.Root.Single("TextArea").Owner).Value));
            var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            ApplicationHarness.On(tool, () => { tool.Closed += () => closed.TrySetResult(true); return true; });
            await Click(main, "Close the tool window from the main window");
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(tool.IsClosed);
            Assert.Empty(ApplicationHarness.On(main, () => main.Exceptions));
        }
        finally { CloseMain(harness, main); }
    }

    private static Task Click(NazorWindow window, string text) => ApplicationHarness.On(window, () =>
        window.Renderer.DispatchEventAsync(window.Root.Elements("Button").Single(b => b.Text() == text), "onclick", new PointerEventArgs()));

    [Fact]
    public async Task Splash_preview_is_borderless_topmost_taskbar_hidden_and_closes_from_its_content()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<Nazor.Samples.Windowing.App>(new() { Activate = false }));
        var main = harness.Window<Nazor.Samples.Windowing.App>();
        try
        {
            await Click(main, "Preview a borderless splash window");
            var splash = harness.Window<Nazor.Samples.Windowing.SplashWindow>();
            var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            ApplicationHarness.On(splash, () =>
            {
                splash.Closed += () => closed.TrySetResult(true);
                Assert.Equal(WindowFrameOwner.Nazor, splash.AppliedDecorations!.Configuration.Frame);
                Assert.True(splash.IsAlwaysOnTop);
                Assert.False(splash.IsVisibleInTaskbar);
                Assert.False(splash.Capabilities.CanResize);
                Assert.Empty(splash.Root.Elements("TitleBar"));
                return true;
            });
            Assert.True(SpinWait.SpinUntil(() => ApplicationHarness.On(splash, () => splash.FrameCount > 0), TimeSpan.FromSeconds(3)));
            SaveSnapshot(splash, "borderless-splash");
            await Click(splash, "Close preview");
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(splash.IsClosed);
        }
        finally { CloseMain(harness, main); }
    }

    private static unsafe void SaveSnapshot(NazorWindow window, string name)
    {
        var handle = ApplicationHarness.On(window, () =>
        {
            window.Activate();
            RedrawWindow(window.PlatformWindow.Handle, null, 0, 0x101);
            return window.PlatformWindow.Handle;
        });
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/windowing"));
        Directory.CreateDirectory(directory);
        using var graphics = new Nazor.Skia.SkiaRenderer();
        var image = ApplicationHarness.On(window, () => window.AppliedDecorations?.Configuration.Frame == WindowFrameOwner.Nazor) ? ScreenCapture.CapturePresented(handle) : ScreenCapture.Capture(handle);
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), graphics.EncodePng(image));
    }

    private static void CloseMain(ApplicationHarness harness, NazorWindow main)
    {
        ApplicationHarness.On(main, () => { main.Close(); return true; });
        Assert.True(harness.WaitForExit());
    }
}
