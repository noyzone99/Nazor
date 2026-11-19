using System.Text.Json;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Nazor;
using Nazor.Input;
using Nazor.Platform;
#if NAZOR_DEVTOOLS
using Nazor.DevTools;
#endif

namespace DevToolsLaunch;

internal static class Program
{
    internal static string Gate = "";
    private static void Main(string[] args)
    {
        Gate = args[0];
        var builder = NazorApplication.CreateBuilder();
        builder.Diagnostics.DevChannel = false;
#if NAZOR_DEVTOOLS
        builder.UseDevTools(options: new()
        {
            ShortcutKey = args.Contains("--menu-only") ? null : args.Contains("--custom-shortcut") ? "D" : "F12",
            ShortcutModifiers = args.Contains("--custom-shortcut") ? KeyModifiers.Control | KeyModifiers.Shift : KeyModifiers.None,
        });
#endif
        builder.Application.UseMainWindow<LaunchPage>(new() { Activate = false });
        using var application = builder.Build();
        application.Run();
    }
}

public sealed class LaunchPage : WindowComponentBase, IDisposable
{
    [Inject] public IWindowService Windows { get; set; } = null!;
    [Parameter] public int Number { get; set; } = 1;
    private readonly CancellationTokenSource lifetime = new();
    private int f12Calls;
    private bool ownerVerified;
    private IDisposable? keyBinding;
    protected override void OnInitialized() => keyBinding = ((NazorWindow)Window).Input.KeyBindings.Register("F12", KeyModifiers.None,
        EventCallback.Factory.Create<KeyboardEventArgs>(this, () => f12Calls++));
    protected override void BuildWindowContent(Nazor.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "TextBlock"); builder.AddContent(1, "F12 fixture " + Number); builder.CloseElement();
    }
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return Task.CompletedTask;
        _ = CommandsAsync();
        return Task.CompletedTask;
    }
    private void Status(string command, string? error = null)
    {
        var window = (NazorWindow)Window;
#if NAZOR_DEVTOOLS
        var agent = window.GetDevTools();
        object? descriptor = agent?.Descriptor;
        var connected = agent?.IsConnected == true;
#else
        object? descriptor = null;
        var connected = false;
#endif
        var value = JsonSerializer.Serialize(new { command, error, processId = Environment.ProcessId, number = Number, window.FrameCount, connected, descriptor, f12Calls, ownerVerified });
        var file = Program.Gate + "." + Number + ".status";
        File.WriteAllText(file + ".tmp", value);
        File.Move(file + ".tmp", file, true);
    }
    private async Task CommandsAsync()
    {
        var previous = "";
        try
        {
            await Task.Delay(100, lifetime.Token);
            await InvokeAsync(() => Status("ready"));
            while (!lifetime.IsCancellationRequested)
            {
                await Task.Delay(50, lifetime.Token);
                var file = Program.Gate + "." + Number + ".command";
                if (!File.Exists(file)) continue;
                string text;
                try { text = await File.ReadAllTextAsync(file, lifetime.Token); }
                catch (IOException) { continue; }
                if (text.Length == 0 || text == previous) continue;
                previous = text;
                await InvokeAsync(async () =>
                {
                    try
                    {
                        var command = text.Split(' ')[0];
                        switch (command)
                        {
                            case "f12": ((IWindowHost)Window).OnKeyDown("F12", KeyModifiers.None, false); ((IWindowHost)Window).OnKeyUp("F12", KeyModifiers.None); break;
                            case "shortcut": ((IWindowHost)Window).OnKeyDown("D", KeyModifiers.Control | KeyModifiers.Shift, false); ((IWindowHost)Window).OnKeyUp("D", KeyModifiers.Control | KeyModifiers.Shift); break;
                            case "menu":
                                if (OperatingSystem.IsWindows()) MenuProbe.Invoke(((NazorWindow)Window).PlatformWindow.Handle);
                                else throw new PlatformNotSupportedException("The native system menu cannot be customized on this backend.");
                                break;
                            case "modified": ((IWindowHost)Window).OnKeyDown("F12", KeyModifiers.Control, false); break;
                            case "repeat": ((IWindowHost)Window).OnKeyDown("F12", KeyModifiers.None, true); break;
                            case "second": Windows.Open(typeof(LaunchPage), new Dictionary<string, object?> { [nameof(Number)] = 2 }); break;
                            case "open":
#if NAZOR_DEVTOOLS
                                await ((NazorWindow)Window).OpenDevToolsAsync();
#endif
                                break;
                            case "owner":
                                ownerVerified = OperatingSystem.IsWindows() && MenuProbe.HasOwner(int.Parse(text.Split(' ')[1]), Window.NativeHandle!.Value.Handle);
                                break;
                            case "close": Window.Close(); return;
                            case "exit": foreach (var window in Windows.Windows.ToArray()) window.Close(); break;
                        }
                        if (command != "exit") Status(text);
                    }
                    catch (Exception error) { Status(text, error.Message); }
                });
            }
        }
        catch (OperationCanceledException) { }
    }
    public void Dispose() { lifetime.Cancel(); keyBinding?.Dispose(); }
}

[SupportedOSPlatform("windows")]
internal static partial class MenuProbe
{
    [DllImport("user32.dll")] private static extern nint GetSystemMenu(nint window, [MarshalAs(UnmanagedType.Bool)] bool revert);
    [DllImport("user32.dll")] private static extern int GetMenuItemCount(nint menu);
    [DllImport("user32.dll")] private static extern uint GetMenuItemID(nint menu, int position);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetMenuStringW(nint menu, uint item, StringBuilder text, int maximum, uint flags);
    [DllImport("user32.dll")] private static extern nint SendMessageW(nint window, uint message, nint wParam, nint lParam);
    private delegate bool WindowCallback(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, nint parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out int processId);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    internal static bool HasOwner(int processId, nint expectedOwner)
    {
        var found = false;
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out var actual);
            if (actual == processId && GetWindow(window, 4) == expectedOwner) found = true;
            return true;
        }, 0);
        return found;
    }
    internal static void Invoke(nint window)
    {
        var menu = GetSystemMenu(window, false);
        for (var i = 0; i < GetMenuItemCount(menu); i++)
        {
            var text = new StringBuilder(256);
            GetMenuStringW(menu, (uint)i, text, text.Capacity, 0x400);
            if (!text.ToString().StartsWith("Open Nazor DevTools", StringComparison.Ordinal)) continue;
            SendMessageW(window, 0x0112, (nint)GetMenuItemID(menu, i), 0);
            return;
        }
        throw new InvalidOperationException("The DevTools system menu command was not registered.");
    }
}
