using System.Globalization;
using Nazor.Themes.NazorLeaf;

namespace Nazor.DevTools;

internal static class Program
{
    internal static string? DiscoveryDirectory { get; private set; }
    internal static string? SessionId { get; private set; }
    internal static int VisualStudioProcessId { get; private set; }
    internal static string? IdeDescriptor { get; private set; }
    internal static bool FindDebugger { get; private set; }
    internal static bool Owned { get; private set; }
    internal static string? OwnerHandle { get; private set; }
    internal static event Action? Activated;
    private static int Main(string[] args)
    {
        var activate = true;
        string? expectedVersion = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--help")
            { Console.WriteLine("Nazor.DevTools [--discovery-directory <path>] [--no-activate] [--connect-session <id>] [--visual-studio <ide-pid>] [--ide-descriptor <path>]"); return 0; }
            if (args[i] == "--no-activate") activate = false;
            else if (args[i] == "--find-debugger") FindDebugger = true;
            else if (args[i] == "--owned") Owned = true;
            else if (args[i] == "--owner-handle" && i + 1 < args.Length && args[i + 1].Length <= 4096) OwnerHandle = args[++i];
            else if (args[i] == "--expected-version" && i + 1 < args.Length) expectedVersion = args[++i];
            else if (args[i] == "--connect-session" && i + 1 < args.Length) SessionId = args[++i];
            else if (args[i] == "--ide-descriptor" && i + 1 < args.Length) IdeDescriptor = Path.GetFullPath(args[++i]);
            else if (args[i] == "--visual-studio" && i + 1 < args.Length && int.TryParse(args[++i], out var ide) && ide > 0) VisualStudioProcessId = ide;
            else if (args[i] == "--discovery-directory" && i + 1 < args.Length) DiscoveryDirectory = Path.GetFullPath(args[++i]);
            else { Console.Error.WriteLine("Unknown or incomplete option: " + args[i]); return 2; }
        }
        try { Protocol.InspectorLauncher.Validate(Path.Combine(AppContext.BaseDirectory, "Nazor.DevTools.dll"), expectedVersion); }
        catch (Exception error) when (error is IOException or System.Text.Json.JsonException or InvalidOperationException)
        { Console.Error.WriteLine("Nazor DevTools: " + error.Message); return 4; }
        using var instance = SessionId is null ? null : new InspectorInstance(SessionId, DiscoveryDirectory ?? Protocol.LocalDevTools.DiscoveryDirectory);
        if (instance is not null && !instance.TryOwn())
        {
            try { instance.ActivateAsync(new(VisualStudioProcessId, IdeDescriptor, FindDebugger, Owned, OwnerHandle)).GetAwaiter().GetResult(); return 0; }
            catch (Exception error) when (error is IOException or OperationCanceledException) { Console.Error.WriteLine("The existing inspector could not be activated: " + error.Message); return 3; }
        }
        if (instance is not null) instance.Activated += activation =>
        {
            if (activation.VisualStudioProcessId > 0) VisualStudioProcessId = activation.VisualStudioProcessId;
            if (activation.IdeDescriptor is not null) IdeDescriptor = activation.IdeDescriptor;
            FindDebugger |= activation.FindDebugger;
            Owned |= activation.Owned;
            if (activation.OwnerHandle is not null) OwnerHandle = activation.OwnerHandle;
            Activated?.Invoke();
        };
        var builder = NazorApplication.CreateBuilder();
        builder.Fonts.AddNazorLeafFonts();
        builder.Application.Culture = CultureInfo.GetCultureInfo("en-US");
        builder.Application.UICulture = CultureInfo.GetCultureInfo("en-US");
        builder.Diagnostics.DevChannel = false;
        builder.Diagnostics.ConfigureWindow = window => _ = WatchOwnerAsync(window);
        builder.Application.UseMainWindow<App>(new() { Activate = activate });
        using var app = builder.Build();
        app.Appearance.Theme = InspectorTheme.Theme;
        app.Appearance.StyleSheet = InspectorTheme.StyleSheet;
        app.Appearance.ColorScheme = InspectorPreferences.Read(InspectorPreferences.FilePath);
        return app.Run();
    }

    private static async Task WatchOwnerAsync(NazorWindow window)
    {
        using var lifetime = new CancellationTokenSource();
        window.Closed += lifetime.Cancel;
        string? appliedOwner = null;
        Protocol.LocalDevToolsDescriptor? descriptor = null;
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                if (Owned && SessionId is not null)
                {
                    descriptor ??= Protocol.LocalDevTools.Discover(DiscoveryDirectory).FirstOrDefault(d => d.Identity.SessionId == SessionId);
                    var directory = DiscoveryDirectory ?? Protocol.LocalDevTools.DiscoveryDirectory;
                    if (descriptor is null || !File.Exists(Path.Combine(directory, SessionId + ".json")) || !Protocol.LocalDevTools.IsAlive(descriptor))
                    {
                        await window.Dispatcher.InvokeAsync(() => { if (!window.IsClosed) window.Close(); });
                        return;
                    }
                    if (OwnerHandle is { Length: > 0 } owner && owner != appliedOwner)
                    {
                        await window.Dispatcher.InvokeAsync(() =>
                        {
                            if (!window.IsClosed) window.PlatformWindow.SetOwner(owner);
                        });
                        appliedOwner = owner;
                    }
                }
                await Task.Delay(250, lifetime.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("Nazor DevTools: owner monitoring failed: " + error.Message);
            await window.Dispatcher.InvokeAsync(() => { if (!window.IsClosed && Owned) window.Close(); });
        }
        finally { window.Closed -= lifetime.Cancel; }
    }
}
