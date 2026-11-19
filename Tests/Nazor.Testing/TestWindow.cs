namespace Nazor.Testing;

public sealed class TestWindow : IWindow
{
    private string title = "Nazor";
    private string? rootClass, style;
    private IStyleSheet? styleSheet;
    private ImageSource? icon;
    private WindowDecorationPolicy? decorations;
    public WindowDecorationPolicy Decorations
    {
        get => decorations ?? WindowDecorationPolicy.Default;
        set { decorations = value; AppliedDecorations = new(value.Primary, 0); Changed?.Invoke(); }
    }
    public WindowDecorationResult? AppliedDecorations { get; private set; }
    public ValueTask<WindowDecorationResult> SetDecorationsAsync(WindowDecorationPolicy policy, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Decorations = policy;
        return new(AppliedDecorations!);
    }
    public string Title { get => title; set { title = value; Changed?.Invoke(); } }
    public string? Class { get => rootClass; set { rootClass = value; Changed?.Invoke(); } }
    public string? Style { get => style; set { style = value; Changed?.Invoke(); } }
    public IStyleSheet? StyleSheet { get => styleSheet; set { styleSheet = value; Changed?.Invoke(); } }
    public double Width { get; set; } = 800;
    public double Height { get; set; } = 600;
    public ImageSource? Icon { get => icon; set { icon = value; Changed?.Invoke(); } }
    public ImageSource? EffectiveIcon => icon;
    public BackdropSelection Backdrop { get; set; }
    public string? PlacementKey { get; set; }
    public double? MinWidth { get; set; }
    public double? MinHeight { get; set; }
    public double? MaxWidth { get; set; }
    public double? MaxHeight { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public bool IsMinimizable { get; set; } = true;
    public bool IsMaximizable { get; set; } = true;
    public bool IsResizable { get; set; } = true;
    public bool FullScreenEnabled { get; set; } = true;
    public bool IsAlwaysOnTop { get; set; }
    public bool IsVisibleInTaskbar { get; set; } = true;
    public WindowCapabilities Capabilities => new(IsMinimizable, IsMaximizable, IsResizable, FullScreenEnabled)
        { Decorations = DecorationSupport, CanSetAlwaysOnTop = true, CanSetTaskbarVisibility = true };
    public static WindowDecorationCapabilities DecorationSupport { get; } = new()
    {
        NativeFrame = WindowSupportStatus.Supported,
        NazorFrame = WindowSupportStatus.Supported,
        NativeTitleBar = WindowSupportStatus.Supported,
        ClientTitleBarOnNativeFrame = WindowSupportStatus.Supported,
        IntegratedNativeCaptionButtons = WindowSupportStatus.Supported,
        FrameChanges = WindowSupportStatus.Supported,
        CaptionChanges = WindowSupportStatus.Supported,
    };
    public bool IsClosed { get; private set; }
    public WindowState State { get; private set; }
    public event Action? Changed;
    public event Action? StateChanged;
    public event Action? Closed;
    public void Show(bool activate = true) { }
    public void Hide() { }
    public void Activate() { }
    public void Minimize() => SetState(WindowState.Minimized);
    public void Maximize() => SetState(WindowState.Maximized);
    public void Restore() => SetState(WindowState.Normal);
    public void EnterFullScreen() => SetState(WindowState.FullScreen);
    public void ExitFullScreen() => Restore();
    public void Close() { IsClosed = true; Closed?.Invoke(); }
    public void Close(object? result) => Close();
    private void SetState(WindowState state) { State = state; StateChanged?.Invoke(); Changed?.Invoke(); }
}
