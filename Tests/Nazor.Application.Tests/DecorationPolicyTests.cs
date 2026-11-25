using Microsoft.Extensions.DependencyInjection;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Rendering;
using Nazor.Testing;
using Nazor.Controls;
using Nazor.Style;

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
    public void Client_frame_styles_follow_the_theme_and_respect_forced_colors()
    {
        using var window = new NazorWindow(new ProbePlatform(native: true), new UIDispatcher(), typeof(PolicyWindow),
            new() { Decorations = WindowDecorationPolicy.Require(WindowDecorations.Nazor()) });
        var root = window.Root.Single("Window");
        Assert.Equal("nazor", root.Attributes["frame-owner"]);
        (StyleSheet Theme, int TopLeft, int BottomLeft, int Border)[] themes =
        [
            (Themes.Themes.Basic, 4, 4, 1),
            (Themes.Fluent.Themes.Fluent, 8, 8, 1),
            (Themes.NazorLeaf.NazorLeafTheme.Theme, 28, 8, 1),
            (Themes.Adwaita.AdwaitaTheme.Theme, 14, 14, 1),
            (Themes.Breeze.BreezeTheme.Theme, 4, 2, 1),
            (Themes.Windows9x.Windows9xTheme.Theme, 0, 0, 1),
            (Themes.WindowsXP.WindowsXPTheme.Theme, 8, 0, 3),
            (Themes.Windows7.Windows7Theme.Theme, 6, 6, 1),
            (Themes.Cde.CdeTheme.Theme, 0, 0, 3),
        ];
        foreach (var (theme, topLeft, bottomLeft, border) in themes)
        {
            window.UpdateAppearance(new ApplicationAppearance { Theme = theme });
            var style = window.Scene.GetStyle(root);
            Assert.Equal(SizeValue.Fixed(Length.Px(topLeft)), style.Get(StyleProperties.WindowFrameBorderTopLeftRadius));
            Assert.Equal(SizeValue.Fixed(Length.Px(bottomLeft)), style.Get(StyleProperties.WindowFrameBorderBottomLeftRadius));
            Assert.Equal(Length.Px(border), style.Get(StyleProperties.WindowFrameBorderWidth));
        }

        window.UpdateAppearance(new ApplicationAppearance { Theme = Themes.Cde.CdeTheme.Theme, HighContrast = true });
        var forced = window.Scene.GetStyle(root);
        Assert.Equal(SizeValue.Fixed(Length.Px(4)), forced.Get(StyleProperties.WindowFrameBorderTopLeftRadius));
        Assert.Equal(Length.Px(1), forced.Get(StyleProperties.WindowFrameBorderWidth));
        Assert.Empty(forced.Get(StyleProperties.WindowFrameBoxShadow).Shadows!.Items);
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

    [Theory]
    [InlineData(WindowSupportStatus.Supported, WindowSupportStatus.Supported)]
    [InlineData(WindowSupportStatus.Unsupported, WindowSupportStatus.Supported)]
    [InlineData(WindowSupportStatus.Unknown, WindowSupportStatus.Supported)]
    [InlineData(WindowSupportStatus.Supported, WindowSupportStatus.Unsupported)]
    public async Task Theming_sample_checks_the_pair_and_keeps_the_accepted_decorations_on_rejection(
        WindowSupportStatus mixed, WindowSupportStatus frameChanges)
    {
        var platform = new ProbePlatform(native: true, rejectNative: mixed == WindowSupportStatus.Unknown)
        {
            ClientTitleBarOnNativeFrame = mixed,
            FrameChanges = frameChanges,
        };
        using var services = new ServiceCollection().AddNazorServices().BuildServiceProvider();
        using var scope = services.CreateScope();
        using var window = new NazorWindow(platform, new UIDispatcher(), typeof(Nazor.Samples.Theming.App),
            new() { Services = scope.ServiceProvider });
        var root = window.Root;
        var args = new PointerEventArgs { Button = PointerButton.Primary };
        ElementNode ById(string id) => root.Elements().Single(element => Equals(element.Attributes.GetValueOrDefault("id"), id));
        Task Click(ElementNode element) => window.Renderer.DispatchEventAsync(element, "onclick", args);
        async Task Pick(string id, string label)
        {
            await Click(ById(id));
            await Click(root.Elements("SelectOption").Single(option => option.Text() == label));
        }

        await Pick("titlebar-owner", "System");
        Assert.True(ById("apply-decorations").IsDisabled);
        Assert.Contains("The system title bar requires the system frame.", root.Text());
        await Pick("titlebar-owner", "Theme");
        await Pick("frame-owner", "System");
        Assert.Equal(WindowDecorations.Nazor(), window.AppliedDecorations!.Configuration);
        Assert.Single(platform.Window!.Requests);

        if (frameChanges == WindowSupportStatus.Unsupported)
        {
            Assert.True(ById("apply-decorations").IsDisabled);
            Assert.Contains("The backend cannot change the requested decorations on an open window.", root.Text());
            return;
        }
        if (mixed == WindowSupportStatus.Unsupported)
        {
            Assert.True(ById("apply-decorations").IsDisabled);
            Assert.Contains("Client title-bar content on a native frame is unavailable.", root.Text());
            await Pick("titlebar-owner", "System");
        }
        Assert.False(ById("apply-decorations").IsDisabled);
        await Click(ById("apply-decorations"));

        var hybrid = WindowDecorations.Native(WindowTitleBarContent.Nazor, WindowCaptionButtons.Nazor);
        if (mixed == WindowSupportStatus.Unknown)
        {
            Assert.Equal(WindowDecorations.Nazor(), window.AppliedDecorations!.Configuration);
            Assert.Equal([WindowDecorations.Nazor(), hybrid, WindowDecorations.Nazor()], platform.Window.Requests);
            Assert.Contains("The desktop rejected the requested owner.", root.Text());
        }
        else
        {
            var applied = mixed == WindowSupportStatus.Supported ? hybrid : WindowDecorations.Native();
            Assert.Equal(applied, window.AppliedDecorations!.Configuration);
            Assert.Equal([applied], window.Decorations.Options);
        }
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
        public WindowSupportStatus ClientTitleBarOnNativeFrame = WindowSupportStatus.Unsupported;
        public WindowSupportStatus FrameChanges = WindowSupportStatus.Supported;
        public WindowCapabilities Capabilities => new(true, true, true, true)
        {
            Decorations = new()
            {
                NativeFrame = native ? WindowSupportStatus.Supported : WindowSupportStatus.Unsupported,
                NativeTitleBar = native ? WindowSupportStatus.Supported : WindowSupportStatus.Unsupported,
                NazorFrame = WindowSupportStatus.Supported,
                FrameChanges = FrameChanges,
                CaptionChanges = WindowSupportStatus.Supported,
                ClientTitleBarOnNativeFrame = ClientTitleBarOnNativeFrame,
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
        public WindowChrome Chrome { get; private set; }
        public async ValueTask<WindowDecorations> SetDecorationsAsync(WindowDecorations configuration, CancellationToken cancellationToken = default)
        {
            Requests.Add(configuration);
            if (configuration.Frame == WindowFrameOwner.Native && rejectNative || configuration.Frame == WindowFrameOwner.Nazor && RejectClient)
                throw new PlatformNotSupportedException("The desktop rejected the requested owner.");
            var applied = Pending is null ? configuration : await Pending.Task.ConfigureAwait(false);
            var frame = applied.Frame == WindowFrameOwner.Nazor ? WindowChromeOwner.Nazor : WindowChromeOwner.System;
            Chrome = new(frame, WindowChromeOwner.None, WindowChromeOwner.None)
            {
                TitleBar = applied.HasNativeTitleBar ? WindowChromeOwner.System : applied.TitleBar switch
                {
                    WindowTitleBarContent.Nazor => WindowChromeOwner.Nazor,
                    WindowTitleBarContent.User => WindowChromeOwner.User,
                    _ => WindowChromeOwner.None,
                },
            };
            return applied;
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
