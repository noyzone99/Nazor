using System.Runtime.InteropServices;
using Nazor.Controls;
using Nazor.Rendering;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;
[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed partial class WindowOperationsTests
{
    [LibraryImport("user32.dll")]
    private static partial nint SendMessageW(nint hwnd, uint message, nint wParam, nint lParam);
    [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint FindWindowExW(nint parent, nint after, string? className, string? title);
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(nint hwnd);
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, true, false)]
    [InlineData(true, true, true, true)]
    public void Operations_are_independent_and_native_commands_obey_them(bool minimize, bool maximize, bool resize, bool fullScreen)
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<ChromeWindow>(new() { Activate = false, IsMinimizable = minimize, IsMaximizable = maximize, IsResizable = resize, FullScreenEnabled = fullScreen, }));
        var window = harness.Window<ChromeWindow>();
        ApplicationHarness.On(window, () =>
        {
            var caps = window.Capabilities;
            Assert.Equal(minimize, caps.CanMinimize);
            Assert.Equal(maximize, caps.CanMaximize);
            Assert.Equal(resize, caps.CanResize);
            Assert.Equal(fullScreen, caps.CanEnterFullScreen);
            var native = window.PlatformWindow;
            var style = (long)GetWindowLongPtrW(native.Handle, -16);
            Assert.Equal(minimize, (style & 0x20000) != 0);
            Assert.Equal(maximize, (style & 0x10000) != 0);
            Assert.Equal(resize, (style & 0x40000) != 0);
            if (!minimize)
            {
                window.Minimize();
                SendMessageW(native.Handle, WM_SYSCOMMAND, 0xF020, 0);
                Assert.Equal(WindowState.Normal, window.State);
            }

            if (!maximize)
            {
                window.Maximize();
                SendMessageW(native.Handle, WM_SYSCOMMAND, 0xF030, 0);
                Assert.Equal(WindowState.Normal, window.State);
            }

            window.EnterFullScreen();
            Assert.Equal(fullScreen ? WindowState.FullScreen : WindowState.Normal, window.State);
            window.FullScreenEnabled = false;
            window.ExitFullScreen();
            Assert.Equal(WindowState.Normal, window.State);
            var size = native.ClientSize;
            window.IsResizable = !resize;
            Assert.Equal(!resize, window.Capabilities.CanResize);
            Assert.Equal(size, native.ClientSize);
            Assert.False(IsWindowVisible(FindWindowExW(native.Handle, 0, "SCROLLBAR", null)));
            return true;
        });
    }

    [Fact]
    public void Dialog_operations_are_declared_in_the_window_and_do_not_inherit_main_window_defaults()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(new() { Activate = false, FullScreenEnabled = false }));
        var main = harness.Window<OrderBook>();
        foreach (var allowFullScreen in new[]
        {
            false,
            true
        }

        )
        {
            var result = ApplicationHarness.On(main, () =>
            {
                var service = ((OrderBook)main.Root.Component).Windows;
                var completion = service.ShowDialogAsync(typeof(OperationWindow), new Dictionary<string, object?> { ["FullScreen"] = allowFullScreen, ["Maximize"] = allowFullScreen });
                var dialog = NazorWindow.OpenWindows.Single(w => w.IsDialog && w.Root.Component is OperationWindow);
                Assert.Equal(allowFullScreen, dialog.FullScreenEnabled);
                Assert.Equal(allowFullScreen, dialog.Capabilities.CanEnterFullScreen);
                Assert.Equal(allowFullScreen, dialog.IsMaximizable);
                Assert.False(dialog.IsResizable);
                Assert.False(dialog.IsMinimizable);
                dialog.Close();
                return completion;
            });
            Assert.Null(ApplicationHarness.Result(result));
        }
    }

    [Fact]
    public void Root_operations_are_independent_of_main_window_defaults_and_unrelated_renders_preserve_setters()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(new() { Activate = false, IsResizable = false }));
        var main = harness.Window<OrderBook>();
        ApplicationHarness.On(main, () =>
        {
            var service = ((OrderBook)main.Root.Component).Windows;
            var inherited = (NazorWindow)service.Open<OrderDetail>();
            Assert.True(inherited.IsResizable);
            Assert.True(inherited.IsMaximizable);
            var child = (NazorWindow)service.Open<OperationWindow>(new Dictionary<string, object?> { ["Resize"] = false, ["Minimize"] = false });
            var root = (OperationWindow)child.Root.Component;
            Assert.False(child.IsResizable);
            Assert.False(child.IsMinimizable);
            root.SetResize(null);
            Assert.True(child.IsResizable);
            child.IsResizable = false;
            root.ChangeTitle();
            Assert.False(child.IsResizable);
            root.SetResize(true);
            Assert.True(child.IsResizable);
            child.EnterFullScreen();
            root.SetResize(false);
            child.FullScreenEnabled = false;
            child.ExitFullScreen();
            Assert.False(child.Capabilities.CanResize);
            Assert.False(child.Capabilities.CanMinimize);
            inherited.Close();
            child.Close();
            return true;
        });
    }
}

public sealed class OperationWindow : WindowComponentBase
{
    [Parameter]
    public bool? Resize { get; set; }

    [Parameter]
    public bool? Minimize { get; set; }

    [Parameter]
    public bool? FullScreen { get; set; }

    [Parameter]
    public bool? Maximize { get; set; }

    private string title = "Operations";
    public void SetResize(bool? value)
    {
        Resize = value;
        Window.IsResizable = value ?? true;
        StateHasChanged();
    }

    public void ChangeTitle()
    {
        title += "!";
        Window.Title = title;
        StateHasChanged();
    }

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
    }

    protected override void OnInitialized()
    {
        Window.Title = title;
        Window.IsResizable = Resize ?? Window.IsResizable;
        Window.IsMinimizable = Minimize ?? Window.IsMinimizable;
        Window.FullScreenEnabled = FullScreen ?? Window.FullScreenEnabled;
        Window.IsMaximizable = Maximize ?? Window.IsMaximizable;
    }
}
