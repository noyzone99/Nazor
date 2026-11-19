using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class NativeDecorationTests
{
    [Theory]
    [InlineData(WindowState.Maximized)]
    [InlineData(WindowState.Minimized)]
    [InlineData(WindowState.FullScreen)]
    public void Decoration_changes_preserve_non_normal_state_and_the_restored_client_size(WindowState state)
    {
        using var harness = new WindowHarness(typeof(FrameWindow), new() { Activate = false });
        harness.Read(window =>
        {
            var handle = window.PlatformWindow.Handle;
            var size = window.PlatformWindow.ClientSize;
            if (state == WindowState.Maximized) window.Maximize();
            if (state == WindowState.Minimized) window.Minimize();
            if (state == WindowState.FullScreen) window.EnterFullScreen();
            foreach (var configuration in new[] { WindowDecorations.Nazor(),
                WindowDecorations.Native(WindowTitleBarContent.User, WindowCaptionButtons.Native), WindowDecorations.Native() })
            {
                window.ApplyDecorations(configuration);
                Assert.Equal(state, window.State);
                Assert.Equal(handle, window.PlatformWindow.Handle);
            }
            window.Restore();
            Assert.InRange(Math.Abs(window.PlatformWindow.ClientSize.Width - size.Width), 0, 1);
            Assert.InRange(Math.Abs(window.PlatformWindow.ClientSize.Height - size.Height), 0, 1);
            Assert.Empty(window.Exceptions);
            return true;
        });
    }

    [Fact]
    public void Every_frame_title_and_button_combination_is_rejected_or_applied_as_reported()
    {
        using var harness = new WindowHarness(typeof(FrameWindow), new() { Activate = false });
        harness.Read(window =>
        {
            var handle = window.PlatformWindow.Handle;
            var root = window.Root.Component;
            foreach (var frame in Enum.GetValues<WindowFrameOwner>())
            foreach (var title in Enum.GetValues<WindowTitleBarContent>())
            foreach (var buttons in Enum.GetValues<WindowCaptionButtons>())
            {
                WindowDecorations Configuration() => frame == WindowFrameOwner.Native
                    ? WindowDecorations.Native(title, buttons) : WindowDecorations.Nazor(title, buttons);
                if (title == WindowTitleBarContent.None && buttons != WindowCaptionButtons.None)
                {
                    Assert.Throws<ArgumentException>(Configuration);
                    continue;
                }
                var configuration = Configuration();
                var previous = window.AppliedDecorations;
                if (window.Capabilities.Decorations.GetChangeSupport(configuration).Status == WindowSupportStatus.Unsupported)
                {
                    Assert.Throws<PlatformNotSupportedException>(() => window.ApplyDecorations(configuration));
                    Assert.Equal(previous, window.AppliedDecorations);
                    continue;
                }
                window.ApplyDecorations(configuration);
                Assert.Equal(configuration, window.AppliedDecorations!.Configuration);
                Assert.Equal(handle, window.PlatformWindow.Handle);
                Assert.Same(root, window.Root.Component);
                Assert.Equal(frame == WindowFrameOwner.Native ? WindowChromeOwner.System : WindowChromeOwner.Nazor, window.Chrome.Frame);
                Assert.Equal(title == WindowTitleBarContent.None ? 0 : 1, window.Root.Elements("TitleBar").Count());
                Assert.Equal(buttons == WindowCaptionButtons.User ? 1 : 0,
                    window.Root.Elements("Button").Count(element => element.Id == "user-close"));
                Assert.Equal(buttons == WindowCaptionButtons.Nazor ? 3 : 0, window.Root.Elements("CaptionButtonsButton").Count());
                Assert.Empty(window.Exceptions);
            }
            window.ApplyDecorations(WindowDecorations.Native());
            Assert.Equal(WindowChromeOwner.System, window.Chrome.TitleBar);
            Assert.Empty(window.Root.Elements("TitleBar"));
            return true;
        });
    }

    [Fact]
    public void Frame_ownership_changes_preserve_the_native_window_size_and_resize_support()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Requires the Windows backend.");
        using var harness = new WindowHarness(typeof(FrameWindow), new() { Activate = false });
        harness.Read(window =>
        {
            var native = window.PlatformWindow;
            var handle = native.Handle;
            var size = native.ClientSize;
            var client = WindowDecorations.Nazor();
            Assert.Equal(client, native.SetDecorationsAsync(client, TestContext.Current.CancellationToken).GetAwaiter().GetResult());
            Assert.Equal(handle, native.Handle);
            Assert.True(native.Capabilities.CanResize);
            Assert.Equal(WindowChromeOwner.Nazor, native.Chrome.Frame);
            Assert.Equal(WindowChromeOwner.Nazor, native.Chrome.TitleBar);
            Assert.InRange(Math.Abs(native.ClientSize.Width - size.Width), 0, 1);
            Assert.InRange(Math.Abs(native.ClientSize.Height - size.Height), 0, 1);
            Assert.Throws<PlatformNotSupportedException>(() => native.SetDecorationsAsync(
                WindowDecorations.Nazor(WindowTitleBarContent.Nazor, WindowCaptionButtons.Native), TestContext.Current.CancellationToken).GetAwaiter().GetResult());
            Assert.Equal(WindowChromeOwner.Nazor, native.Chrome.Frame);
            Assert.Equal(WindowDecorations.Native(), native.SetDecorationsAsync(WindowDecorations.Native(), TestContext.Current.CancellationToken).GetAwaiter().GetResult());
            Assert.Equal(handle, native.Handle);
            Assert.Equal(WindowChromeOwner.System, native.Chrome.Frame);
            Assert.InRange(Math.Abs(native.ClientSize.Width - size.Width), 0, 1);
            Assert.InRange(Math.Abs(native.ClientSize.Height - size.Height), 0, 1);
            return true;
        });
    }

    public sealed class FrameWindow : WindowComponentBase
    {
        protected override void BuildWindowContent(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Frame content");
            builder.OpenComponent<Nazor.Controls.UserTitleBar>(1);
            builder.AddComponentParameter(2, "Title", "User caption");
            builder.CloseComponent();
            builder.OpenComponent<Nazor.Controls.UserCaptionButtons>(3);
            builder.AddComponentParameter(4, "ChildContent", (RenderFragment)(buttons =>
            {
                buttons.OpenComponent<Nazor.Controls.Button>(0);
                buttons.AddComponentParameter(1, "Id", "user-close");
                buttons.AddComponentParameter(2, "ChildContent", (RenderFragment)(text => text.AddContent(0, "Close")));
                buttons.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
