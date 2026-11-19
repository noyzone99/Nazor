using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Rendering;
using Nazor.Testing;
using Nazor.Controls;

namespace Nazor.Application.Tests;

public sealed class DecorationPolicyTests
{
    [Fact]
    public async Task Content_only_desktop_hosts_receive_automatic_chrome_without_replacing_the_content_root()
    {
        using var window = new NazorWindow(new ProbePlatform(false), new UIDispatcher(), typeof(PlainContent));
        var content = Assert.IsType<PlainContent>(window.Root.Component);
        Assert.Equal(WindowDecorations.Nazor(), window.AppliedDecorations!.Configuration);
        Assert.Single(window.Renderer.RootComponents.SelectMany(root => root.Nodes).OfType<ElementNode>());
        await window.SetDecorationsAsync(WindowDecorationPolicy.Require(
            WindowDecorations.Nazor(WindowTitleBarContent.None, WindowCaptionButtons.None)), TestContext.Current.CancellationToken);
        Assert.Same(content, window.Root.Component);
    }

    public sealed class PlainContent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Retained content");
    }

    [Fact]
    public async Task Inactive_user_declarations_mount_independently_and_survive_runtime_provider_changes()
    {
        var platform = new ProbePlatform(native: true);
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(DeclarationsWindow));
        var root = Assert.IsType<DeclarationsWindow>(window.Root.Component);
        Assert.Equal(0, root.TitleCreated);
        Assert.Equal(0, root.ButtonsCreated);
        await window.SetDecorationsAsync(WindowDecorationPolicy.Require(WindowDecorations.Nazor(
            WindowTitleBarContent.Nazor, WindowCaptionButtons.User)), TestContext.Current.CancellationToken);
        Assert.Equal(0, root.TitleCreated);
        Assert.Equal(2, root.ButtonsCreated);
        Assert.Equal("nazor", window.Root.Single("TitleBar").Attributes["source"]);
        await window.SetDecorationsAsync(WindowDecorationPolicy.Require(WindowDecorations.Nazor(
            WindowTitleBarContent.User, WindowCaptionButtons.Nazor)), TestContext.Current.CancellationToken);
        Assert.Equal(1, root.TitleCreated);
        Assert.Equal(2, root.ButtonsDisposed);
        Assert.Equal("user", window.Root.Single("TitleBar").Attributes["source"]);
        Assert.Equal(3, window.Root.Elements("CaptionButtonsButton").Count());
        await window.SetDecorationsAsync(WindowDecorationPolicy.Require(WindowDecorations.Native()), TestContext.Current.CancellationToken);
        Assert.Empty(window.Root.Elements("TitleBar"));
        Assert.Equal(1, root.TitleDisposed);
        Assert.Same(root, window.Root.Component);
    }

    [Fact]
    public void Missing_active_user_content_is_an_authoring_error_and_does_not_trigger_fallback()
    {
        var platform = new ProbePlatform(native: true);
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PolicyWindow));
        var policy = WindowDecorationPolicy.Prefer(WindowDecorations.Nazor(WindowTitleBarContent.User, WindowCaptionButtons.User), WindowDecorations.Native());
        var change = window.SetDecorationsAsync(policy, TestContext.Current.CancellationToken);
        Assert.Throws<InvalidOperationException>(() => change.GetAwaiter().GetResult());
        Assert.Equal(WindowDecorations.Native(), window.AppliedDecorations!.Configuration);
        Assert.Single(platform.Window!.Requests);
    }

    [Fact]
    public void Duplicate_user_declarations_fail_even_when_their_content_is_inactive()
    {
        Assert.Throws<InvalidOperationException>(() => new NazorWindow(new ProbePlatform(native: true), new UIDispatcher(), typeof(DuplicateDeclarationsWindow)));
    }

    [Fact]
    public async Task Theme_presenters_follow_the_selected_provider_without_changing_window_policy()
    {
        var platform = new ProbePlatform(native: true);
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PolicyWindow), new()
        {
            Theme = Nazor.Themes.NazorLeaf.NazorLeafTheme.Theme,
            Decorations = WindowDecorationPolicy.Require(WindowDecorations.Nazor()),
        });
        var policy = window.Decorations;
        Assert.Single(window.Root.Elements("LeafMark"));
        Assert.Equal(3, window.Root.Elements("LeafWindowButton").Count());
        Assert.Empty(window.Root.Elements("CaptionButtonsButton"));
        Assert.Equal(WindowCaptionButtons.Nazor, window.AppliedDecorations!.Configuration.CaptionButtons);
        window.UpdateAppearance(new ApplicationAppearance { Theme = Nazor.Themes.Themes.Basic });
        Assert.Empty(window.Root.Elements("LeafMark"));
        Assert.Equal(3, window.Root.Elements("CaptionButtonsButton").Count());
        Assert.Same(policy, window.Decorations);
        await window.SetDecorationsAsync(WindowDecorationPolicy.Require(WindowDecorations.Native()), TestContext.Current.CancellationToken);
        window.UpdateAppearance(new ApplicationAppearance { Theme = Nazor.Themes.NazorLeaf.NazorLeafTheme.Theme });
        Assert.Empty(window.Root.Elements("TitleBar"));
        Assert.True(window.AppliedDecorations.Configuration.HasNativeTitleBar);
    }

    [Fact]
    public void Desktop_default_selects_native_then_nazor_and_never_user()
    {
        var platform = new ProbePlatform(native: false);
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PolicyWindow));
        Assert.Equal(new(WindowDecorations.Nazor(), 1), window.AppliedDecorations);
        Assert.Equal(WindowDecorations.Nazor(), platform.Options!.Decorations);
        Assert.Single(platform.Window!.Requests);
        Assert.Equal(WindowDecorations.Nazor(), window.Capabilities.Decorations.Current);
    }

    [Fact]
    public void Pending_opening_is_not_reported_as_applied_before_native_acceptance()
    {
        var platform = new ProbePlatform(native: true) { HoldOpening = true };
        var dispatcher = new UIDispatcher();
        using var owned = new NazorWindow(platform, dispatcher, typeof(PolicyWindow));
        Assert.Null(owned.AppliedDecorations);
        platform.Window!.Pending!.SetResult(WindowDecorations.Native());
        Assert.True(dispatcher.RunUntil(() => owned.AppliedDecorations is not null, TimeSpan.FromSeconds(3)));
        Assert.Equal(0, owned.AppliedDecorations!.OptionIndex);
    }

    [Fact]
    public void Strict_unsupported_opening_fails_before_creating_the_native_window()
    {
        var platform = new ProbePlatform(native: false);
        Assert.Throws<PlatformNotSupportedException>(() => new NazorWindow(platform, new UIDispatcher(), typeof(PolicyWindow),
            new() { Decorations = WindowDecorationPolicy.Require(WindowDecorations.Native()) }));
        Assert.Null(platform.Window);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_single_unsupported_preference_uses_desktop_defaults_on_opening_and_runtime_changes(bool native)
    {
        var platform = new ProbePlatform(native);
        var policy = WindowDecorationPolicy.Prefer(
            WindowDecorations.Native(WindowTitleBarContent.Nazor, WindowCaptionButtons.Native));
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PolicyWindow), new() { Decorations = policy });
        var expected = new WindowDecorationResult(native ? WindowDecorations.Native() : WindowDecorations.Nazor(), native ? 1 : 2);
        Assert.Equal(expected, window.AppliedDecorations);
        Assert.Equal(expected.Configuration, platform.Options!.Decorations);
        var content = window.Root.Component;
        var change = await window.SetDecorationsAsync(policy, TestContext.Current.CancellationToken);
        Assert.Equal(expected, change);
        Assert.Same(policy, window.Decorations);
        Assert.Same(content, window.Root.Component);
        Assert.Equal([expected.Configuration, expected.Configuration], platform.Window!.Requests);
    }

    [Fact]
    public void Negotiated_rejection_uses_only_an_authorized_fallback()
    {
        var platform = new ProbePlatform(native: true, rejectNative: true);
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PolicyWindow),
            new() { Decorations = WindowDecorationPolicy.Prefer(WindowDecorations.Native()) });
        Assert.Equal(1, window.AppliedDecorations!.OptionIndex);
        Assert.Equal([WindowDecorations.Native(), WindowDecorations.Nazor()], platform.Window!.Requests);
    }

    [Fact]
    public void Runtime_rejection_retains_the_previous_policy_native_identity_and_content()
    {
        var platform = new ProbePlatform(native: true);
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PolicyWindow));
        var root = window.Root.Component;
        var previous = window.AppliedDecorations;
        var policy = window.Decorations;
        platform.Window!.RejectClient = true;
        var failure = window.SetDecorationsAsync(WindowDecorationPolicy.Require(WindowDecorations.Nazor()), TestContext.Current.CancellationToken);
        Assert.Throws<PlatformNotSupportedException>(() => failure.GetAwaiter().GetResult());
        Assert.Equal(previous, window.AppliedDecorations);
        Assert.Same(policy, window.Decorations);
        Assert.Same(root, window.Root.Component);
        Assert.False(platform.Window.IsClosed);
        Assert.Throws<InvalidOperationException>(() => window.Decorations = policy);
    }

    [Fact]
    public void Closing_a_window_cancels_pending_callers_without_requiring_another_dispatcher_iteration()
    {
        var platform = new ProbePlatform(native: true);
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PolicyWindow));
        platform.Window!.Pending = new();
        var change = window.SetDecorationsAsync(WindowDecorationPolicy.Require(WindowDecorations.Nazor()),
            TestContext.Current.CancellationToken).AsTask();
        Assert.False(change.IsCompleted);
        window.Dispose();
        Assert.True(change.IsCanceled);
    }

    [Fact]
    public void Authoring_failure_after_native_rejection_restores_the_previous_configuration()
    {
        var platform = new ProbePlatform(native: true);
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(PolicyWindow));
        platform.Window!.RejectClient = true;
        var previous = window.AppliedDecorations;
        var change = window.SetDecorationsAsync(WindowDecorationPolicy.Prefer(WindowDecorations.Nazor(),
            WindowDecorations.Nazor(WindowTitleBarContent.User)), TestContext.Current.CancellationToken);
        Assert.Throws<InvalidOperationException>(() => change.GetAwaiter().GetResult());
        Assert.Equal(previous, window.AppliedDecorations);
        Assert.Equal(WindowDecorations.Native(), platform.Window.Requests.Last());
    }

    [Fact]
    public async Task Serialized_runtime_changes_honor_cancellation_before_native_dispatch()
    {
        var platform = new ProbePlatform(native: true);
        var dispatcher = new UIDispatcher();
        using var window = new NazorWindow(platform, dispatcher, typeof(PolicyWindow));
        var pending = platform.Window!.Pending = new();
        using var cancelled = new CancellationTokenSource();
        var run = dispatcher.InvokeAsync(async () =>
        {
            var first = window.SetDecorationsAsync(WindowDecorationPolicy.Require(WindowDecorations.Nazor()), TestContext.Current.CancellationToken);
            var second = window.SetDecorationsAsync(WindowDecorationPolicy.Require(WindowDecorations.Native()), cancelled.Token);
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second.AsTask());
            pending.SetResult(WindowDecorations.Nazor());
            Assert.Equal(WindowDecorations.Nazor(), (await first).Configuration);
        });
        Assert.True(dispatcher.RunUntil(() => run.IsCompleted, TimeSpan.FromSeconds(3)));
        await run;
        Assert.Equal(2, platform.Window.Requests.Count);
    }

    public sealed class PolicyWindow : WindowComponentBase
    {
        protected override void BuildWindowContent(RenderTreeBuilder builder) => builder.AddContent(0, "Persistent content");
    }

    public sealed class DuplicateDeclarationsWindow : WindowComponentBase
    {
        protected override void BuildWindowContent(RenderTreeBuilder builder)
        {
            for (var index = 0; index < 2; index++)
            {
                builder.OpenComponent<UserTitleBar>(0, typeof(DuplicateDeclarationsWindow), this);
                builder.SetKey(index);
                builder.CloseComponent();
            }
        }
    }

    public sealed class DeclarationsWindow : WindowComponentBase
    {
        public int TitleCreated, TitleDisposed, ButtonsCreated, ButtonsDisposed;
        protected override void BuildWindowContent(RenderTreeBuilder builder)
        {
            builder.OpenComponent<UserTitleBar>(0, typeof(DeclarationsWindow), this);
            builder.AddComponentParameter(1, nameof(UserTitleBar.ChildContent), (RenderFragment)(inner => Probe(inner, true)));
            builder.CloseComponent();
            builder.OpenComponent<UserCaptionButtons>(2, typeof(DeclarationsWindow), this);
            builder.AddComponentParameter(3, nameof(UserCaptionButtons.Left), (RenderFragment)(inner => Probe(inner, false)));
            builder.AddComponentParameter(4, nameof(UserCaptionButtons.Right), (RenderFragment)(inner => Probe(inner, false)));
            builder.CloseComponent();
            builder.AddContent(5, "Persistent content");
        }
        private void Probe(RenderTreeBuilder builder, bool title)
        {
            builder.OpenComponent<ContentProbe>(0);
            builder.AddComponentParameter(1, nameof(ContentProbe.Created), (Action)(() => { if (title) TitleCreated++; else ButtonsCreated++; }));
            builder.AddComponentParameter(2, nameof(ContentProbe.Disposed), (Action)(() => { if (title) TitleDisposed++; else ButtonsDisposed++; }));
            builder.CloseComponent();
        }
    }

    public sealed class ContentProbe : ComponentBase, IDisposable
    {
        [Parameter] public Action? Created { get; set; }
        [Parameter] public Action? Disposed { get; set; }
        protected override void OnInitialized() => Created?.Invoke();
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "User content");
        public void Dispose() => Disposed?.Invoke();
    }

    private sealed class ProbePlatform(bool native, bool rejectNative = false) : IPlatform
    {
        public WindowOptions? Options;
        public ProbeWindow? Window;
        public bool HoldOpening;
        public WindowCapabilities Capabilities => new(true, true, true, true)
        {
            Decorations = new()
            {
                NativeFrame = native ? WindowSupportStatus.Supported : WindowSupportStatus.Unsupported,
                NativeTitleBar = native ? WindowSupportStatus.Supported : WindowSupportStatus.Unsupported,
                NazorFrame = WindowSupportStatus.Supported,
                FrameChanges = WindowSupportStatus.Supported,
                CaptionChanges = WindowSupportStatus.Supported,
            }
        };
        public IPlatformWindow CreateWindow(IWindowHost host, WindowOptions options)
        {
            Options = options;
            Window = new(Capabilities, rejectNative);
            if (HoldOpening) Window.Pending = new();
            return Window;
        }
        public int Run(UIDispatcher dispatcher) => 0;
        public void Quit(int exitCode = 0) { }
    }

    private sealed class ProbeWindow(WindowCapabilities capabilities, bool rejectNative) : IPlatformWindow
    {
        public List<WindowDecorations> Requests { get; } = [];
        public bool RejectClient;
        public TaskCompletionSource<WindowDecorations>? Pending;
        public ValueTask<WindowDecorations> SetDecorationsAsync(WindowDecorations configuration, CancellationToken cancellationToken = default)
        {
            Requests.Add(configuration);
            if (configuration.Frame == WindowFrameOwner.Native && rejectNative || configuration.Frame == WindowFrameOwner.Nazor && RejectClient)
                throw new PlatformNotSupportedException("The desktop rejected the requested owner.");
            return Pending is null ? new(configuration) : new(Pending.Task);
        }
        public WindowCapabilities Capabilities => capabilities;
        public nint Handle => 42;
        public float Scale => 1;
        public Size ClientSize => new(800, 600);
        public string Title { get; set; } = "Probe";
        public WindowState State => WindowState.Normal;
        public bool IsClosed { get; private set; }
        public bool PrefersReducedMotion => false;
        public bool PrefersDarkColorScheme => false;
        public IClipboard Clipboard { get; } = new MemoryClipboard();
        public void Show(bool activate = true) { }
        public void Close() => IsClosed = true;
        public void Invalidate() { }
        public void RequestAnimationFrame(TimeSpan delay) { }
        public void Resize(Size size) { }
        public void SetTextInputCaret(Rect? caret) { }
        public void Dispose() => IsClosed = true;
    }
}
