using Microsoft.Extensions.DependencyInjection;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform.Windows;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class WindowHarness : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private readonly Thread thread;
    private readonly ManualResetEventSlim ready = new();
    private readonly ManualResetEventSlim exited = new();
    private UIDispatcher dispatcher = null!;
    private NazorWindow window = null!;
    private Exception? startupError;

    public WindowHarness(Type root, ApplicationRuntimeSettings? options = null)
    {
        thread = new Thread(() =>
        {
            Win32Platform? platform = null;
            try
            {
                NativeDesktop.KeepDisplayActive();
                dispatcher = new UIDispatcher();
                platform = new Win32Platform();
                using var services = options?.Services is null
                    ? new ServiceCollection().AddSingleton<KeyBindings>().BuildServiceProvider()
                    : null;
                var settings = services is null ? options : (options ?? new ApplicationRuntimeSettings()) with { Services = services };
                window = new NazorWindow(platform, dispatcher, root, settings);
                window.Closed += () => platform.Quit();
                window.Show(activate: false);
                Handle = window.PlatformWindow.Handle;
                ready.Set();
                platform.Run(dispatcher);
            }
            catch (Exception exception)
            {
                startupError = exception;
                ready.Set();
            }
            finally
            {
                try { platform?.Dispose(); }
                finally
                {
                    NativeDesktop.ReleaseDisplay();
                    exited.Set();
                }
            }
        })
        { IsBackground = true, Name = "Nazor UI (test)" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!ready.Wait(Timeout)) throw new TimeoutException("The window did not start.");
        if (startupError is not null) throw new InvalidOperationException("The window failed to start.", startupError);
    }

    public nint Handle { get; private set; }

    public bool HasExited => exited.IsSet;

    public T Read<T>(Func<NazorWindow, T> read)
    {
        var task = dispatcher.InvokeAsync(() => read(window));
        if (!task.Wait(Timeout)) throw new TimeoutException("The UI thread did not respond.");
        return task.Result;
    }

    public void WaitUntil(Func<NazorWindow, bool> condition, string description)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (Read(condition)) return;
            Thread.Sleep(10);
        }
        throw new TimeoutException($"Timed out waiting for: {description}");
    }

    public void Mouse(uint message, Point logical, nint keys = 0)
    {
        ReleaseModifiers();
        var scale = Read(w => w.PlatformWindow.Scale);
        var x = (int)MathF.Round(logical.X * scale);
        var y = (int)MathF.Round(logical.Y * scale);
        PostMessageW(Handle, message, keys, (y << 16) | (x & 0xFFFF));
    }

    public void Click(Point logical)
    {
        Mouse(WM_MOUSEMOVE, logical);
        Mouse(WM_LBUTTONDOWN, logical, 1);
        Mouse(WM_LBUTTONUP, logical);
    }

    public void Wheel(Point logical, int delta, bool horizontal = false)
    {
        ReleaseModifiers();
        var scale = Read(w => w.PlatformWindow.Scale);
        var point = new POINT { X = (int)MathF.Round(logical.X * scale), Y = (int)MathF.Round(logical.Y * scale) };
        ClientToScreen(Handle, ref point);
        PostMessageW(Handle, horizontal ? WM_MOUSEHWHEEL : WM_MOUSEWHEEL, delta << 16, (point.Y << 16) | (point.X & 0xFFFF));
    }

    public void Character(char c)
    {
        ReleaseModifiers();
        PostMessageW(Handle, WM_CHAR, c, 1);
    }

    public void Key(int virtualKey, bool down)
    {
        ReleaseModifiers();
        PostMessageW(Handle, down ? WM_KEYDOWN : WM_KEYUP, virtualKey, down ? 1 : unchecked((nint)0xC0000001));
    }

    private unsafe void ReleaseModifiers() => Read(_ =>
    {
        var state = stackalloc byte[256];
        return SetKeyboardState(state) != 0;
    });

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern unsafe int SetKeyboardState(byte* state);

    public void Close()
    {
        if (exited.IsSet) return;
        Read(w =>
        {
            w.Close();
            return true;
        });
        if (!exited.Wait(Timeout)) throw new TimeoutException("The window did not close.");
    }

    public void Dispose() => Close();

    public static (uint Gdi, uint User) GuiResources()
    {
        var process = GetCurrentProcess();
        return (GetGuiResources(process, GR_GDIOBJECTS), GetGuiResources(process, GR_USEROBJECTS));
    }
}
