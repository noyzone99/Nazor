using Microsoft.Extensions.DependencyInjection;
using Nazor.Controls;
using Nazor.Rendering;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;
public sealed class OrderBook : WindowComponentBase
{
    [Inject]
    public IWindowService Windows { get; set; } = null!;

    [Inject]
    public IServiceProvider Services { get; set; } = null!;

    [Inject]
    public IFileDialogService Files { get; set; } = null!;

    [Inject]
    public IAnnouncer Announcer { get; set; } = null!;

    public Task<string?> OpenCsv(string folder, string name) => Files.OpenFileAsync(new OpenFileOptions { InitialDirectory = folder, FileName = name, Filters = [new FileFilter("Fogli CSV", "*.csv"), new FileFilter("Tutti i file", "*.*")], });
    public Task<string?> SaveCsv(string folder) => Files.SaveFileAsync(new SaveFileOptions { InitialDirectory = folder, FileName = "ordini", DefaultExtension = "csv" });
    public Task<string?> Rename(string name) => Windows.ShowDialogAsync<RenameDialog, string>(new Dictionary<string, object?> { ["Name"] = name }, new WindowOpenOptions { Activate = false });
    public Task<MessageBoxResult> Confirm(MessageBoxButtons buttons) => Windows.ShowMessageAsync("Salvare le modifiche all'ordine prima di chiudere?", "Ordini", buttons, MessageBoxIcon.Warning);
    public IWindow OpenDetail(int number, bool owned = true) => Windows.Open<OrderDetail>(new Dictionary<string, object?> { ["Number"] = number }, new WindowOpenOptions { Owner = owned ? Window : null, Activate = false });
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(b => b.AddContent(0, "Elenco")));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Ordini";
    }
}

public sealed class OrderDetail : WindowComponentBase
{
    [Parameter]
    public int Number { get; set; }

    [Inject]
    public IServiceProvider Services { get; set; } = null!;

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
    }

    protected override void OnInitialized()
    {
        Window.Title = $"Ordine {Number}";
        Window.Width = 300d;
        Window.Height = 200d;
    }
}

public sealed class RenameDialog : WindowComponentBase
{
    [Parameter]
    public string Name { get; set; } = "";

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(4, (RenderFragment)(window =>
        {
            window.OpenComponent<global::Nazor.Controls.TextField>(0, typeof(RenameDialog));
            window.AddComponentParameter(1, "Value", Name);
            window.AddComponentParameter(2, "ValueChanged", EventCallback.Factory.Create<string>(this, value => Name = value));
            window.CloseComponent();
            window.OpenComponent<Button>(3, typeof(RenameDialog));
            window.AddComponentParameter(4, "IsDefault", true);
            window.AddComponentParameter(5, "OnClick", EventCallback.Factory.Create<Nazor.Input.PointerEventArgs>(this, () => Window.Close(Name)));
            window.AddAttribute(6, "ChildContent", (RenderFragment)(b => b.AddContent(0, "OK")));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Rinomina";
        Window.Width = 320d;
        Window.Height = 160d;
    }
}

[Route("/")]
public sealed class HomeScreen : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Inizio");
}

[Route("/ordini/{Id:int}")]
public sealed class OrderScreen : ComponentBase
{
    [Parameter]
    public int Id { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, $"Ordine {Id}");
}

public sealed class Navigator : WindowComponentBase
{
    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<Frame>(0, typeof(Navigator));
            window.AddComponentParameter(1, "Assemblies", new[] { typeof(Navigator).Assembly });
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Navigazione";
    }
}

public sealed class PlacedWindow : WindowComponentBase
{
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
    }

    protected override void OnInitialized()
    {
        Window.Title = "Ordini";
        Window.PlacementKey = "ordini";
    }
}

public sealed class ChromeWindow : WindowComponentBase
{
    private bool nativeControls = true;
    private bool? canResize;
    public void SetIsResizable(bool? value)
    {
        canResize = value;
        Window.IsResizable = value ?? true;
        StateHasChanged();
    }

    public void ToggleSystemButtons()
    {
        nativeControls = !nativeControls;
        Window.ApplyDecorations(DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User, nativeControls));
        StateHasChanged();
    }

    public int States { get; private set; }

    protected override void OnInitialized()
    {
        Window.Title = "Ordini";
        Window.Width = 600.0;
        Window.Height = 400.0;
        Window.Decorations = WindowDecorationPolicy.Require(DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User, nativeControls));
        Window.IsResizable = canResize ?? Window.IsResizable;
        Window.StateChanged += () => States++;
    }

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(5, (RenderFragment)(window =>
        {
            window.OpenComponent<UserTitleBar>(0, typeof(ChromeWindow));
            window.AddComponentParameter(31, "Style", "height: 32px; gap: 8px; padding-left: 16px");
            window.AddAttribute(1, "ChildContent", (RenderFragment)(bar =>
            {
                bar.OpenComponent<TextBlock>(10, typeof(ChromeWindow));
                bar.AddComponentParameter(11, "Class", "brand");
                bar.AddComponentParameter(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Ordini")));
                bar.CloseComponent();
                bar.OpenComponent<Button>(0, typeof(ChromeWindow));
                bar.AddComponentParameter(1, "Class", "search");
                bar.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Cerca")));
                bar.CloseComponent();
            }));
            window.CloseComponent();
            window.OpenComponent<Button>(2, typeof(ChromeWindow));
            window.AddComponentParameter(3, "Class", "page");
            window.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Nuovo ordine")));
            window.CloseComponent();
        }));
    }
}

public sealed class MicaWindow : WindowComponentBase
{
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
    }

    protected override void OnInitialized()
    {
        Window.Title = "Mica";
        Window.Width = 300.0;
        Window.Height = 200.0;
        Window.Backdrop = (BackdropSelection)WindowsBackdrop.Mica;
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class ApplicationHarness : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private readonly ManualResetEventSlim exited = new();
    private Exception? error;
    public ApplicationHarness(Func<int> run)
    {
        var thread = new Thread(() =>
        {
            try
            {
                ExitCode = run();
            }
            catch (Exception exception)
            {
                error = exception;
            }
            finally
            {
                exited.Set();
            }
        })
        {
            IsBackground = true,
            Name = "Nazor UI (application test)"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    public int? ExitCode { get; private set; }
    public bool HasExited => exited.IsSet;

    public NazorWindow Window<T>(Func<T, bool>? match = null)
        where T : IComponent
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (error is not null)
                throw new InvalidOperationException("The application failed.", error);
            if (NazorWindow.OpenWindows.FirstOrDefault(w => w.Root.Component is T root && (match?.Invoke(root) ?? true))is { } window)
                return window;
            Thread.Sleep(10);
        }

        throw new TimeoutException($"No window of {typeof(T).Name}.");
    }

    public static T On<T>(NazorWindow window, Func<T> action)
    {
        var task = window.Dispatcher.InvokeAsync(action);
        if (!task.Wait(Timeout))
            throw new TimeoutException("The UI thread did not respond.");
        return task.Result;
    }

    public bool WaitForExit() => exited.Wait(Timeout);
    public static T Result<T>(Task<T> task) => task.Wait(Timeout) ? task.Result : throw new TimeoutException("The task did not complete.");
    public static NazorWindow WindowNamed(string name)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (NazorWindow.OpenWindows.FirstOrDefault(w => w.Root.Component.GetType().Name == name && !w.IsClosed)is { } window)
                return window;
            Thread.Sleep(10);
        }

        throw new TimeoutException($"No window of {name}.");
    }

    public static unsafe void Key(NazorWindow window, int virtualKey)
    {
        var handle = On(window, () =>
        {
            var state = stackalloc byte[256];
            new Span<byte>(state, 256).Clear();
            Assert.NotEqual(0, SetKeyboardState(state));
            return window.PlatformWindow.Handle;
        });
        PostMessageW(handle, WM_KEYDOWN, virtualKey, 1);
        PostMessageW(handle, WM_KEYUP, virtualKey, unchecked((nint)0xC0000001));
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern unsafe int SetKeyboardState(byte* state);
    public void Dispose()
    {
        if (exited.IsSet)
            return;
        var windows = NazorWindow.OpenWindows.Where(w => w.Root.Component is OrderBook or OrderDetail or RenameDialog or Navigator or PlacedWindow or ChromeWindow or MicaWindow).ToList();
        foreach (var window in windows)
        {
            try
            {
                window.Dispatcher.Post(window.Close);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        exited.Wait(Timeout);
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed partial class WindowServiceTests
{
    [System.Runtime.InteropServices.LibraryImport("user32.dll", StringMarshalling = System.Runtime.InteropServices.StringMarshalling.Utf16)]
    private static partial nint FindWindowExW(nint parent, nint after, string? className, string? title);
    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(nint window);
    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    private static partial nint SendMessageW(nint window, uint message, nint wParam, nint lParam);
    private static nint FindDialog(nint owner)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            for (var window = FindWindowExW(0, 0, "#32770", null); window != 0; window = FindWindowExW(0, window, "#32770", null))
                if (GetWindow(window, GW_OWNER) == owner && IsWindowVisible(window))
                    return window;
            Thread.Sleep(20);
        }

        throw new TimeoutException("The file dialog did not appear.");
    }

    private static ApplicationRuntimeSettings Options(ShutdownMode mode = ShutdownMode.OnMainWindowClose) => new()
    {
        Activate = false,
        ShutdownMode = mode
    };
    [Fact]
    public void A_component_opens_a_window_with_parameters_an_owner_and_its_own_scope()
    {
        var builder = WindowsApplication.CreateBuilder();
        builder.Application.UseMainWindow<OrderBook>(new() { Activate = false });
        builder.Services.AddScoped<ScopedResource>();
        using var app = builder.Build();
        using var harness = new ApplicationHarness(() => app.Run());
        var main = harness.Window<OrderBook>();
        var book = (OrderBook)main.Root.Component;
        var detail = ApplicationHarness.On(main, () => (NazorWindow)book.OpenDetail(42));
        var order = (OrderDetail)detail.Root.Component;
        Assert.Equal(42, order.Number);
        Assert.Equal("Ordine 42", ApplicationHarness.On(main, () => detail.Title));
        Assert.Same(detail, order.Window);
        Assert.Same(main, book.Window);
        Assert.Equal(2, ApplicationHarness.On(main, () => book.Windows.Windows.Count));
        Assert.Same(main, ApplicationHarness.On(main, () => book.Windows.MainWindow));
        Assert.Equal(main.PlatformWindow.Handle, ApplicationHarness.On(main, () => GetWindow(detail.PlatformWindow.Handle, GW_OWNER)));
        var(mine, theirs) = ApplicationHarness.On(main, () => (order.Services.GetRequiredService<ScopedResource>(), book.Services.GetRequiredService<ScopedResource>()));
        Assert.NotSame(mine, theirs);
        var closed = false;
        ApplicationHarness.On(main, () =>
        {
            order.Window.Closed += () => closed = true;
            order.Window.Close();
            return true;
        });
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!ApplicationHarness.On(main, () => closed) && DateTime.UtcNow < deadline)
            Thread.Sleep(10);
        Assert.True(closed);
        Assert.True(mine.Disposed);
        Assert.False(theirs.Disposed);
        Assert.Single(ApplicationHarness.On(main, () => book.Windows.Windows));
        Assert.False(harness.HasExited);
    }

    [Fact]
    public void The_announcer_of_a_component_speaks_through_its_window()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(Options()));
        var main = harness.Window<OrderBook>();
        var book = (OrderBook)main.Root.Component;
        var heard = ApplicationHarness.On(main, () =>
        {
            var said = new List<string>();
            main.Semantics.Announced += (source, message, priority) => said.Add($"{message} ({priority}) from {source.Role}");
            book.Announcer.Announce("Ordine salvato");
            book.Announcer.Announce("Connessione persa", Accessibility.AnnouncementPriority.Assertive);
            return said;
        });
        Assert.Equal(["Ordine salvato (Polite) from Window", "Connessione persa (Assertive) from Window"], heard);
    }

    [Fact]
    public void Closing_the_main_window_closes_the_others_and_ends_the_application()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(Options()));
        var main = harness.Window<OrderBook>();
        var book = (OrderBook)main.Root.Component;
        var detail = ApplicationHarness.On(main, () => book.OpenDetail(1, owned: false));
        ApplicationHarness.On(main, () =>
        {
            main.Close();
            return true;
        });
        Assert.True(harness.WaitForExit());
        Assert.Equal(0, harness.ExitCode);
        Assert.True(detail.IsClosed);
    }

    [Fact]
    public void With_the_last_window_rule_the_application_ends_when_every_window_closed()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(Options(ShutdownMode.OnLastWindowClose)));
        var main = harness.Window<OrderBook>();
        var book = (OrderBook)main.Root.Component;
        var detail = (NazorWindow)ApplicationHarness.On(main, () => book.OpenDetail(2, owned: false));
        ApplicationHarness.On(main, () =>
        {
            main.Close();
            return true;
        });
        Thread.Sleep(200);
        Assert.False(harness.HasExited);
        Assert.False(ApplicationHarness.On(detail, () => detail.IsClosed));
        ApplicationHarness.On(detail, () =>
        {
            detail.Close();
            return true;
        });
        Assert.True(harness.WaitForExit());
        Assert.Equal(0, harness.ExitCode);
    }

    [Fact]
    public void A_dialog_disables_its_owner_and_returns_the_result_of_its_default_button()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(Options()));
        var main = harness.Window<OrderBook>();
        var book = (OrderBook)main.Root.Component;
        var answer = ApplicationHarness.On(main, () => book.Rename("A-12"));
        var dialog = harness.Window<RenameDialog>();
        var(ownerEnabled, owner, centered) = ApplicationHarness.On(main, () =>
        {
            GetWindowRect(main.PlatformWindow.Handle, out var outer);
            GetWindowRect(dialog.PlatformWindow.Handle, out var inner);
            var(dx, dy) = ((inner.left + inner.right) / 2 - (outer.left + outer.right) / 2, (inner.top + inner.bottom) / 2 - (outer.top + outer.bottom) / 2);
            return (IsWindowEnabled(main.PlatformWindow.Handle), GetWindow(dialog.PlatformWindow.Handle, GW_OWNER), Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1);
        });
        Assert.False(ownerEnabled);
        Assert.Equal(ApplicationHarness.On(main, () => main.PlatformWindow.Handle), owner);
        Assert.True(centered);
        Assert.True(dialog.IsDialog);
        ApplicationHarness.On(dialog, () => ((RenameDialog)dialog.Root.Component).Name = "A-13");
        ApplicationHarness.Key(dialog, 0x0D);
        Assert.Equal("A-13", ApplicationHarness.Result(answer));
        Assert.True(ApplicationHarness.On(main, () => IsWindowEnabled(main.PlatformWindow.Handle) && dialog.IsClosed));
        answer = ApplicationHarness.On(main, () => book.Rename("A-13"));
        ApplicationHarness.Key(harness.Window<RenameDialog>(), 0x1B);
        Assert.Null(ApplicationHarness.Result(answer));
        Assert.False(harness.HasExited);
    }

    [Fact]
    public void A_message_box_shows_its_buttons_fits_its_text_and_answers_the_button_chosen()
    {
        using var harness = new ApplicationHarness(() =>
        {
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("it-IT");
            return ApplicationTestStartup.Run<OrderBook>(Options());
        });
        var main = harness.Window<OrderBook>();
        var book = (OrderBook)main.Root.Component;
        var answer = ApplicationHarness.On(main, () => book.Confirm(MessageBoxButtons.YesNoCancel));
        var box = ApplicationHarness.WindowNamed("MessageBoxView");
        var(title, labels, icon, fits) = ApplicationHarness.On(box, () =>
        {
            var buttons = box.Root.Elements().Where(e => e.Name == "Button").ToList();
            var band = box.Root.Elements().Single(e => e.Name == "MessageBoxButtons");
            return (box.Title, buttons.Select(b => b.Text()).ToList(), box.Root.Elements().Single(e => e.Name == "MessageBoxIcon").Attributes["icon"], Math.Abs(box.Scene.GetBounds(band).Bottom - box.PlatformWindow.ClientSize.Height) <= 1);
        });
        Assert.Equal("Ordini", title);
        Assert.Equal(["Sì", "No", "Annulla"], labels);
        Assert.Equal("warning", icon);
        Assert.True(fits);
        ApplicationHarness.On(box, () =>
        {
            box.Input.Focus(box.Root.Elements().Where(e => e.Name == "Button").ElementAt(1), visible: true);
            return true;
        });
        ApplicationHarness.Key(box, 0x0D);
        Assert.Equal(MessageBoxResult.No, ApplicationHarness.Result(answer));
        answer = ApplicationHarness.On(main, () => book.Confirm(MessageBoxButtons.YesNoCancel));
        ApplicationHarness.Key(ApplicationHarness.WindowNamed("MessageBoxView"), 0x1B);
        Assert.Equal(MessageBoxResult.Cancel, ApplicationHarness.Result(answer));
    }

    [Fact]
    public void A_file_dialog_of_the_system_returns_the_chosen_file_and_null_when_cancelled()
    {
        var folder = Directory.CreateTempSubdirectory("nazor-dialog");
        try
        {
            var file = Path.Combine(folder.FullName, "ordini.csv");
            File.WriteAllText(file, "numero;cliente");
            using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<OrderBook>(Options()));
            var main = harness.Window<OrderBook>();
            var book = (OrderBook)main.Root.Component;
            var handle = ApplicationHarness.On(main, () => main.PlatformWindow.Handle);
            var opened = ApplicationHarness.On(main, () => book.OpenCsv(folder.FullName, "ordini.csv"));
            var dialog = FindDialog(handle);
            Assert.True(ApplicationHarness.On(main, () => !main.IsClosed));
            PostMessageW(dialog, 0x0111, 1, 0);
            Assert.Equal(file, ApplicationHarness.Result(opened), ignoreCase: true);
            var saved = ApplicationHarness.On(main, () => book.SaveCsv(folder.FullName));
            PostMessageW(FindDialog(handle), WM_CLOSE, 0, 0);
            Assert.Null(ApplicationHarness.Result(saved));
            Assert.Empty(ApplicationHarness.On(main, () => main.Exceptions));
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public void The_side_buttons_of_the_mouse_go_back_and_forward_in_a_frame()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<Navigator>(Options()));
        var window = harness.Window<Navigator>();
        var navigator = (Navigator)window.Root.Component;
        string Text() => ApplicationHarness.On(window, () => window.Root.Elements().Single(e => e.Name == "Frame").Text());
        Assert.Equal("Inizio", Text());
        ApplicationHarness.Result(ApplicationHarness.On(window, () => navigator.Navigation.NavigateToAsync("/ordini/9")));
        Assert.Equal("Ordine 9", Text());
        var handle = ApplicationHarness.On(window, () => window.PlatformWindow.Handle);
        PostMessageW(handle, 0x020C, 1 << 16, 0);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (Text() != "Inizio" && DateTime.UtcNow < deadline)
            Thread.Sleep(10);
        Assert.Equal("Inizio", Text());
        PostMessageW(handle, (uint)WM_APPCOMMAND, handle, (nint)(APPCOMMAND_BROWSER_FORWARD << 16));
        deadline = DateTime.UtcNow.AddSeconds(10);
        while (Text() != "Ordine 9" && DateTime.UtcNow < deadline)
            Thread.Sleep(10);
        Assert.Equal("Ordine 9", Text());
    }

    [Fact]
    public void The_placement_store_keeps_the_windows_by_key_and_forgets_a_damaged_file()
    {
        var folder = Directory.CreateTempSubdirectory("nazor-placement");
        try
        {
            var store = new JsonWindowPlacementStore(Path.Combine(folder.FullName, "sub", "windows.json"));
            Assert.Null(store.Load("ordini"));
            store.Save("ordini", new Nazor.Platform.WindowPlacement(10, 20, 800, 600, false));
            store.Save("clienti", new Nazor.Platform.WindowPlacement(30, 40, 500, 400, true));
            var again = new JsonWindowPlacementStore(store.Path);
            Assert.Equal(new Nazor.Platform.WindowPlacement(10, 20, 800, 600, false), again.Load("ordini"));
            Assert.Equal(new Nazor.Platform.WindowPlacement(30, 40, 500, 400, true), again.Load("clienti"));
            File.WriteAllText(store.Path, "{ non è json");
            Assert.Null(again.Load("ordini"));
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public void A_window_with_a_placement_key_reopens_where_it_was_closed()
    {
        var folder = Directory.CreateTempSubdirectory("nazor-placement");
        try
        {
            var store = new JsonWindowPlacementStore(Path.Combine(folder.FullName, "windows.json"));
            int Run()
            {
                var builder = WindowsApplication.CreateBuilder();
                builder.Application.UseMainWindow<PlacedWindow>(new() { Activate = false });
                builder.Services.AddSingleton<IWindowPlacementStore>(store);
                using var app = builder.Build();
                return app.Run();
            }

            Nazor.Platform.WindowPlacement? moved;
            using (var harness = new ApplicationHarness(Run))
            {
                var window = harness.Window<PlacedWindow>();
                moved = ApplicationHarness.On(window, () =>
                {
                    SetWindowPos(window.PlatformWindow.Handle, 0, 150, 130, 640, 480, SWP_NOZORDER | SWP_NOACTIVATE);
                    return window.PlatformWindow.GetPlacement();
                });
                ApplicationHarness.On(window, () =>
                {
                    window.Close();
                    return true;
                });
                Assert.True(harness.WaitForExit());
            }

            Assert.NotNull(moved);
            Assert.Equal(moved, store.Load("ordini"));
            using (var harness = new ApplicationHarness(Run))
            {
                var window = harness.Window<PlacedWindow>();
                Assert.Equal(moved, ApplicationHarness.On(window, () => window.PlatformWindow.GetPlacement()));
            }
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public void A_window_with_its_content_in_the_title_bar_answers_the_hit_test_of_the_system()
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<ChromeWindow>(Options()));
        var window = harness.Window<ChromeWindow>();
        var handle = ApplicationHarness.On(window, () => window.PlatformWindow.Handle);
        var(top, clientTop, size) = ApplicationHarness.On(window, () =>
        {
            GetWindowRect(handle, out var outer);
            var origin = new POINT();
            ClientToScreen(handle, ref origin);
            return (outer.top, origin.Y, window.PlatformWindow.ClientSize);
        });
        Assert.Equal(top, clientTop);
        Assert.Equal((600f, 400f), (size.Width, size.Height));
        nint Hit(Func<NazorWindow, Nazor.Graphics.Point> at)
        {
            var(point, scale) = ApplicationHarness.On(window, () => (at(window), window.PlatformWindow.Scale));
            var screen = new POINT
            {
                X = (int)MathF.Round(point.X * scale),
                Y = (int)MathF.Round(point.Y * scale)
            };
            ClientToScreen(handle, ref screen);
            return SendMessageW(handle, 0x0084, 0, (screen.Y << 16) | (screen.X & 0xFFFF));
        }

        Nazor.Graphics.Point Center(NazorWindow w, Func<ElementNode, bool> match)
        {
            var bounds = w.Scene.GetBounds(w.Root.Elements().Single(match));
            return new Nazor.Graphics.Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        }

        Assert.Equal(2, Hit(w => Center(w, e => e.Class == "brand")));
        Assert.Equal(9, Hit(w => new Nazor.Graphics.Point(w.PlatformWindow.ClientSize.Width - w.SystemButtonsSize.Width / 2, w.SystemButtonsSize.Height / 2)));
        Assert.Equal(20, Hit(w => new Nazor.Graphics.Point(w.PlatformWindow.ClientSize.Width - w.SystemButtonsSize.Width / 6, w.SystemButtonsSize.Height / 2)));
        Assert.Equal(1, Hit(w => Center(w, e => e.Class == "search")));
        Assert.Equal(1, Hit(w => Center(w, e => e.Class == "page")));
        Assert.Equal(12, Hit(w => new Nazor.Graphics.Point(300, 1)));
        ApplicationHarness.On(window, () => ShowWindow(handle, 7));
        Assert.Equal(WindowState.Minimized, ApplicationHarness.On(window, () => window.State));
        ApplicationHarness.On(window, () => ShowWindow(handle, SW_SHOWNOACTIVATE));
        Assert.Equal(WindowState.Normal, ApplicationHarness.On(window, () => window.State));
        Assert.True(ApplicationHarness.On(window, () => ((ChromeWindow)window.Root.Component).States) >= 2);
        Assert.Empty(ApplicationHarness.On(window, () => window.Exceptions));
    }
}
