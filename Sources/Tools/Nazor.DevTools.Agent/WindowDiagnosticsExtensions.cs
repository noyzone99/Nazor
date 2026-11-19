using Nazor.Diagnostics;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.DevTools.Protocol;

namespace Nazor.DevTools;

public static class WindowDiagnosticsExtensions
{
    public static DevToolsSession? GetDiagnostics(this NazorWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return (window.Instrumentation as DiagnosticsAttachment)?.Session;
    }

    public static DevToolsSession EnableDiagnostics(this NazorWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.Dispatcher.AssertAccess();
        ObjectDisposedException.ThrowIf(window.IsClosed, window);
        if (window.Instrumentation is DiagnosticsAttachment current) return current.Session;
        if (window.Instrumentation is not null) throw new InvalidOperationException("The window already has an instrumentation integration.");
        var attachment = new DiagnosticsAttachment(window);
        try { window.Instrumentation = attachment; }
        catch { attachment.Dispose(); throw; }
        return attachment.Session;
    }

    public static DevToolsAgent EnableDevTools(this NazorWindow window, string? discoveryDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.Dispatcher.AssertAccess();
        if (window.GetDevTools() is { IsDisposed: false } current) return current;
        return window.EnableDevTools(new DevToolsOptions { DiscoveryDirectory = discoveryDirectory });
    }

    public static DevToolsAgent EnableDevTools(this NazorWindow window, DevToolsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        var previous = window.GetDiagnostics();
        window.EnableDiagnostics();
        var attachment = (DiagnosticsAttachment)window.Instrumentation!;
        if (attachment.Agent is { IsDisposed: false } current)
        {
            if (options.DiscoveryDirectory != attachment.Options?.DiscoveryDirectory) throw new InvalidOperationException("Disable DevTools before changing the discovery directory.");
            attachment.Configure(options);
            return current;
        }
        try
        {
            attachment.Agent = new DevToolsAgent(attachment.Session, options.DiscoveryDirectory);
            attachment.Agent.Disposed += attachment.OnAgentDisposed;
            attachment.Configure(options);
            return attachment.Agent;
        }
        catch { attachment.Agent?.Dispose(); attachment.Agent = null; if (previous is null) window.DisableDiagnostics(); throw; }
    }

    public static DevToolsAgent? GetDevTools(this NazorWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return (window.Instrumentation as DiagnosticsAttachment)?.Agent;
    }

    public static void DisableDevTools(this NazorWindow window) => window.DisableDiagnostics();

    public static Task OpenDevToolsAsync(this NazorWindow window, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.Dispatcher.AssertAccess();
        var agent = window.GetDevTools() ?? throw new InvalidOperationException("Enable UseDevTools() before opening the inspector.");
        return InspectorLauncher.OpenAsync(agent.Descriptor, cancellationToken, Path.GetDirectoryName(agent.DiscoveryFile),
            closeWithOwner: true, ownerHandle: window.PlatformWindow.ExportedHandle);
    }

    public static void DisableDiagnostics(this NazorWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.Dispatcher.AssertAccess();
        if (window.Instrumentation is DiagnosticsAttachment) window.Instrumentation = null;
    }

    private sealed class DiagnosticsAttachment : IWindowInstrumentation
    {
        private readonly NazorWindow window;
        private Task? launching;
        private string? consumedShortcut;
        private IDisposable? systemMenu;
        private IPlatformWindow? nativeWindow;

        internal DiagnosticsAttachment(NazorWindow window)
        {
            this.window = window;
            Session = new DevToolsSession(window.Renderer, window.Scene, window.Input);
            Session.OverlayChanged += OnOverlayChanged;
        }

        internal DevToolsSession Session { get; }
        internal DevToolsAgent? Agent { get; set; }
        internal DevToolsOptions? Options { get; private set; }
        internal void Configure(DevToolsOptions options)
        {
            if (Options == options) return;
            var menu = options.ShowInSystemMenu ? nativeWindow?.AddSystemMenuCommand(options.MenuText, Open) : null;
            var previous = systemMenu;
            systemMenu = menu;
            Options = options;
            previous?.Dispose();
        }
        public void NativeWindowCreated(IPlatformWindow platformWindow)
        {
            nativeWindow = platformWindow;
            if (Options is { ShowInSystemMenu: true } options && systemMenu is null)
                systemMenu = platformWindow.AddSystemMenuCommand(options.MenuText, Open);
        }
        private void Open()
        {
            if (Agent is { IsDisposed: false } && launching?.IsCompleted != false) launching = LaunchAsync();
        }
        internal void OnAgentDisposed()
        {
            systemMenu?.Dispose();
            systemMenu = null;
            Options = null;
        }
        public bool InterceptsPointer => Session.IsInspecting;
        public PlatformCursor? PointerCursor => Session.IsInspecting ? PlatformCursor.Cross : null;
        public bool PointerMoved(Point point) => Session.PointerMoved(point);
        public bool PointerPressed(Point point, PointerButton button) => Session.PointerPressed(point, button);
        public bool PointerReleased(Point point, PointerButton button) => Session.PointerReleased(point, button);
        public bool PointerLeft() => Session.PointerLeft();
        public void PointerCanceled() => Session.CancelInspection();
        public bool KeyDown(string key) => Session.KeyDown(key);
        public bool KeyDown(string key, KeyModifiers modifiers, bool repeat)
        {
            if (Agent is { IsDisposed: false } && Options?.ShortcutKey is { } shortcut &&
                string.Equals(key, shortcut, StringComparison.OrdinalIgnoreCase) && modifiers == Options.ShortcutModifiers)
            {
                consumedShortcut = key;
                if (!repeat) Open();
                return true;
            }
            return KeyDown(key);
        }
        private async Task LaunchAsync()
        {
            try { await window.OpenDevToolsAsync().ConfigureAwait(false); }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                Console.Error.WriteLine("Nazor DevTools: " + error.Message);
                System.Diagnostics.Trace.TraceError("Nazor DevTools: " + error.Message);
            }
        }
        public bool KeyUp(string key)
        {
            if (consumedShortcut is not null && string.Equals(key, consumedShortcut, StringComparison.OrdinalIgnoreCase)) { consumedShortcut = null; return true; }
            return Session.KeyUp(key);
        }
        public DisplayList Compose(DisplayList list) => Session.Compose(list);

        public void RecordFrame(WindowFrameMetrics frame) => Session.RecordFrame(frame.Number, (FrameCause)frame.Cause,
            frame.Total, frame.Styles, frame.Layout, frame.DisplayList, frame.Overlay, frame.Raster,
            frame.StylesRecomputed, frame.LayoutComputations, frame.Damage,
            frame.Rendering,frame.RenderWorkerFrame,frame.RenderWorkerDuration,frame.GpuFramesReplaced);

        private void OnOverlayChanged() => window.RequestFrame(WindowFrameCause.Overlay);

        public void Dispose()
        {
            try { systemMenu?.Dispose(); }
            finally
            {
                try { Agent?.Dispose(); }
                finally
                {
                    Session.OverlayChanged -= OnOverlayChanged;
                    Session.Dispose();
                }
            }
        }
    }
}
