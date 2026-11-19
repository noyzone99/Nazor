using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Nazor.Graphics;

namespace Nazor.Platform.Windows;

internal sealed unsafe partial class WindowsWindowing : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ActivationContext
    {
        public uint Size, Flags;
        public char* Source;
        public ushort Architecture, Language;
        public char* Directory;
        public nint Resource, Name, Module;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)] private static partial nint CreateActCtxW(in ActivationContext context);
    [LibraryImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool ActivateActCtx(nint context, out nuint cookie);
    [LibraryImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool DeactivateActCtx(uint flags, nuint cookie);
    [LibraryImport("kernel32.dll")] private static partial void ReleaseActCtx(nint context);
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial nint LoadLibraryExW(string path, nint file, uint flags);
    [LibraryImport("kernel32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static partial bool FreeLibrary(nint module);
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleExW", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PinModule(uint flags, nint address, out nint module);

    private readonly nint context;
    private readonly nuint cookie;
    private readonly DispatcherQueueController? controller;
    private readonly List<nint> modules;

    private WindowsWindowing(nint context, nuint cookie, DispatcherQueueController? controller, List<nint> modules) =>
        (this.context, this.cookie, this.controller, this.modules) = (context, cookie, controller, modules);

    private static string? ManifestPath()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Windowing.manifest");
        if (File.Exists(path)) return path;
        path = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-" + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(), "native", "Windowing.manifest");
        return File.Exists(path) ? path : null;
    }

    public static bool IsAvailable => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) && ManifestPath() is not null;

    public static void SetAlwaysOnTop(nint hwnd, bool value)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) throw new PlatformNotSupportedException("Modern windowing requires Windows 11.");
        var window = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));
        if (window.Presenter is not OverlappedPresenter presenter)
            throw new InvalidOperationException("This window does not have an overlapped presenter.");
        presenter.IsAlwaysOnTop = value;
    }

    public static WindowsWindowing? TryInitialize()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return null;
        if (ManifestPath() is not { } path) return null;
        var directory = Path.GetDirectoryName(path)!;
        nint context;
        fixed (char* source = path)
        fixed (char* assemblyDirectory = directory)
            context = CreateActCtxW(new ActivationContext { Size = (uint)sizeof(ActivationContext), Flags = 4,
                Source = source, Directory = assemblyDirectory });
        if (context == -1) throw new Win32Exception(Marshal.GetLastPInvokeError(), "Cannot activate Windows windowing components.");
        if (!ActivateActCtx(context, out var cookie)) { ReleaseActCtx(context); throw new Win32Exception(Marshal.GetLastPInvokeError()); }
        var modules = new List<nint>();
        try
        {

            foreach (var name in new[] { "Microsoft.Internal.FrameworkUdk.dll", "CoreMessagingXP.dll", "dcompi.dll", "Microsoft.UI.dll", "Microsoft.UI.Windowing.Core.dll", "Microsoft.UI.Windowing.dll", "Microsoft.UI.Input.dll" })
            {
                var module = LoadLibraryExW(Path.Combine(directory, name), 0, 0x1100  );
                if (module == 0) throw new Win32Exception(Marshal.GetLastPInvokeError(), "Cannot load " + name);
                modules.Add(module);
            }
            var controller = DispatcherQueue.GetForCurrentThread() is null ? DispatcherQueueController.CreateOnCurrentThread() : null;
            foreach (var module in modules)
                if (!PinModule(5, module, out _)) throw new Win32Exception(Marshal.GetLastPInvokeError());
            return new(context, cookie, controller, modules);
        }
        catch
        {
            foreach (var module in modules.AsEnumerable().Reverse()) FreeLibrary(module);
            DeactivateActCtx(0, cookie); ReleaseActCtx(context); throw;
        }
    }

    public void Dispose()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) controller?.ShutdownQueue();
        DeactivateActCtx(0, cookie);
        ReleaseActCtx(context);
        foreach (var module in modules.AsEnumerable().Reverse()) FreeLibrary(module);
    }
}

internal sealed class WindowsTitleBar
{
    private readonly AppWindow window;
    private readonly AppWindowTitleBar bar;
    private readonly OverlappedPresenter? presenter;
    private (bool Custom, bool Visible, WindowsTitleBarHeight Height, WindowsTitleBarColors Colors, bool Dark)? appearance;
    private InputNonClientPointerSource? input;
    private (int Left, int Width, int Height)? inputRegion;
    [SupportedOSPlatform("windows10.0.18362")]
    private WindowsTitleBar(AppWindow window)
    {
        this.window = window;
        bar = window.TitleBar;
        presenter = window.Presenter as OverlappedPresenter;
    }
    public void Reset()
    {
        appearance = null;
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            UpdateInputRegion();
            bar.ResetToDefault();
        }
        input = null;
        inputRegion = null;
    }

    public static WindowsTitleBar? Create(nint hwnd, bool custom = true)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return null;
        var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var window = AppWindow.GetFromWindowId(id);
        window.TitleBar.ExtendsContentIntoTitleBar = custom;
        return new(window);
    }

    public WindowButtonsMetrics Size(float scale)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return default;
        UpdateInputRegion();
        return new(bar.LeftInset / scale, bar.RightInset / scale, bar.Height / scale);
    }

    public void SetOperations(WindowOperations operations)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        if (presenter is null) return;
        presenter.IsMinimizable = operations.IsMinimizable;
        presenter.IsMaximizable = operations.IsMaximizable;
        presenter.IsResizable = operations.IsResizable;
    }

    public void Apply(bool custom, bool visible, WindowsTitleBarHeight height, WindowsTitleBarColors colors, bool dark)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        var next = (custom, visible, height, colors, dark);
        if (appearance == next) return;
        appearance = next;
        bar.PreferredTheme = custom ? dark ? TitleBarTheme.Dark : TitleBarTheme.Light : TitleBarTheme.Legacy;
        if (custom) bar.PreferredHeightOption = !visible ? TitleBarHeightOption.Collapsed : height == WindowsTitleBarHeight.Tall ? TitleBarHeightOption.Tall : TitleBarHeightOption.Standard;
        static global::Windows.UI.Color? Native(Color? color) => color is { } c
            ? new global::Windows.UI.Color { A = c.A, R = c.R, G = c.G, B = c.B } : null;
        bar.BackgroundColor = custom ? null : Native(colors.BackgroundColor);
        bar.ForegroundColor = custom ? null : Native(colors.ForegroundColor);
        bar.InactiveBackgroundColor = custom ? null : Native(colors.InactiveBackgroundColor);
        bar.InactiveForegroundColor = custom ? null : Native(colors.InactiveForegroundColor);
        bar.ButtonForegroundColor = Native(colors.ButtonForegroundColor);
        bar.ButtonHoverForegroundColor = Native(colors.ButtonHoverForegroundColor);
        bar.ButtonPressedForegroundColor = Native(colors.ButtonPressedForegroundColor);
        bar.ButtonInactiveForegroundColor = Native(colors.ButtonInactiveForegroundColor);
        bar.ButtonBackgroundColor = Native(colors.ButtonBackgroundColor);
        bar.ButtonInactiveBackgroundColor = Native(colors.ButtonInactiveBackgroundColor);
        bar.ButtonHoverBackgroundColor = Native(colors.ButtonHoverBackgroundColor);
        bar.ButtonPressedBackgroundColor = Native(colors.ButtonPressedBackgroundColor);
        UpdateInputRegion();
    }

    [SupportedOSPlatform("windows10.0.22000")]
    private void UpdateInputRegion()
    {
        var next = appearance is { Custom: true }
            ? (Left: bar.LeftInset, Width: Math.Max(0, window.ClientSize.Width - bar.LeftInset - bar.RightInset), Height: bar.Height)
            : default;
        if (inputRegion == next) return;
        try
        {
            if (next.Width > 0 && next.Height > 0)
            {
                input ??= InputNonClientPointerSource.GetForWindowId(window.Id);
                input.SetRegionRects(NonClientRegionKind.Passthrough, [new(next.Left, 0, next.Width, next.Height)]);
            }
            else input?.ClearRegionRects(NonClientRegionKind.Passthrough);
            inputRegion = next;
        }
        catch (UnauthorizedAccessException)
        {
            input = null;
            inputRegion = null;
        }
    }
}
