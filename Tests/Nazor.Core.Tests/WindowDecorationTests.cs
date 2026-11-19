namespace Nazor.Core.Tests;

public sealed class WindowDecorationTests
{
    private static WindowDecorationCapabilities Client => new() { NazorFrame = WindowSupportStatus.Supported };

    [Fact]
    public void Policies_capture_the_exact_authorized_order_without_implicit_user_content()
    {
        WindowDecorations[] fallbacks = [WindowDecorations.Nazor()];
        var policy = WindowDecorationPolicy.Prefer(WindowDecorations.Native(), fallbacks);
        fallbacks[0] = WindowDecorations.Nazor(WindowTitleBarContent.User, WindowCaptionButtons.User);
        Assert.Equal(WindowDecorations.Nazor(), policy.Options[1]);
        Assert.Throws<NotSupportedException>(() => ((IList<WindowDecorations>)policy.Options)[0] = fallbacks[0]);
        Assert.Equal(policy.Options, WindowDecorationPolicy.Default.Options);
        Assert.Single(WindowDecorationPolicy.Require(fallbacks[0]).Options);
    }

    [Fact]
    public void Preferences_append_desktop_defaults_after_explicit_options_without_duplicates()
    {
        var primary = WindowDecorations.Native(WindowTitleBarContent.User, WindowCaptionButtons.Native);
        var fallback = WindowDecorations.Nazor(WindowTitleBarContent.User);
        var policy = WindowDecorationPolicy.Prefer(primary, fallback, primary);
        Assert.Equal([primary, fallback, WindowDecorations.Native(), WindowDecorations.Nazor()], policy.Options);
        Assert.Equal([WindowDecorations.Nazor(), WindowDecorations.Native()],
            WindowDecorationPolicy.Prefer(WindowDecorations.Nazor()).Options);
    }

    [Fact]
    public void A_single_native_preference_falls_back_to_nazor_but_require_stays_strict()
    {
        Assert.Equal(new(WindowDecorations.Nazor(), 1),
            Client.Select(WindowDecorationPolicy.Prefer(WindowDecorations.Native()), false, out _));
        Assert.Null(Client.Select(WindowDecorationPolicy.Require(WindowDecorations.Native()), false, out _));
        Assert.Null(WindowDecorationCapabilities.None.Select(WindowDecorationPolicy.Prefer(WindowDecorations.Native()), false, out _));
    }

    [Fact]
    public void Invalid_shapes_fail_before_backend_selection()
    {
        Assert.Throws<ArgumentException>(() => WindowDecorations.Nazor(WindowTitleBarContent.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => WindowDecorations.Native((WindowTitleBarContent)99, WindowCaptionButtons.None));
        Assert.Throws<ArgumentNullException>(() => WindowDecorationPolicy.Require(null!));
        Assert.Throws<ArgumentException>(() => WindowDecorationPolicy.Prefer(WindowDecorations.Native(), [null!]));
        Assert.Equal(WindowCaptionButtons.None, WindowDecorations.Nazor(WindowTitleBarContent.None, WindowCaptionButtons.None).CaptionButtons);
    }

    [Fact]
    public void Unsupported_native_decorations_only_fall_back_when_authorized()
    {
        Assert.Null(Client.Select(WindowDecorationPolicy.Require(WindowDecorations.Native()), false, out var reason));
        Assert.Contains("native frame", reason);
        var result = Client.Select(WindowDecorationPolicy.Default, false, out _);
        Assert.Equal(new(WindowDecorations.Nazor(), 1), result);
        var user = WindowDecorations.Nazor(WindowTitleBarContent.User, WindowCaptionButtons.User);
        Assert.Equal(user, Client.Select(WindowDecorationPolicy.Require(user), false, out _)!.Configuration);
    }

    [Fact]
    public void Unknown_support_requires_negotiation_before_using_a_lower_priority_option()
    {
        var capabilities = Client with { NativeFrame = WindowSupportStatus.Unknown, NativeTitleBar = WindowSupportStatus.Unknown };
        Assert.Equal(WindowSupportStatus.Unknown, capabilities.GetSupport(WindowDecorations.Native()).Status);
        Assert.Equal(0, capabilities.Select(WindowDecorationPolicy.Default, false, out _)!.OptionIndex);
    }

    [Fact]
    public void Native_controls_cannot_silently_retain_a_native_frame()
    {
        var capabilities = Client with
        {
            NativeFrame = WindowSupportStatus.Supported, ClientTitleBarOnNativeFrame = WindowSupportStatus.Supported,
            IntegratedNativeCaptionButtons = WindowSupportStatus.Supported
        };
        Assert.Equal(WindowSupportStatus.Supported, capabilities.GetSupport(WindowDecorations.Native(WindowTitleBarContent.User, WindowCaptionButtons.Native)).Status);
        Assert.Equal(WindowSupportStatus.Unsupported, capabilities.GetSupport(WindowDecorations.Nazor(WindowTitleBarContent.User, WindowCaptionButtons.Native)).Status);
    }

    [Fact]
    public void Transition_support_is_distinct_from_opening_and_idempotent_requests()
    {
        var capabilities = Client with { Current = WindowDecorations.Nazor() };
        var user = WindowDecorations.Nazor(WindowTitleBarContent.User, WindowCaptionButtons.Nazor);
        Assert.Equal(WindowSupportStatus.Supported, capabilities.GetSupport(user).Status);
        Assert.Equal(WindowSupportStatus.Unsupported, capabilities.GetChangeSupport(user).Status);
        Assert.Equal(WindowSupportStatus.Supported, capabilities.GetChangeSupport(capabilities.Current).Status);
        Assert.Equal(WindowSupportStatus.Supported, (capabilities with { CaptionChanges = WindowSupportStatus.Supported }).GetChangeSupport(user).Status);
    }
}
