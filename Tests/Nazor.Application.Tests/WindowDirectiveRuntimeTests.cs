using Nazor.Controls;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Application.Tests;

[Collection("Windows")]
public sealed class WindowDirectiveRuntimeTests
{
    [Theory]
    [InlineData(WindowState.Maximized)]
    [InlineData(WindowState.FullScreen)]
    [InlineData(WindowState.Minimized)]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void Resizing_a_non_normal_window_preserves_state_and_changes_the_restored_client_size(WindowState state)
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<SwitchableWindow>(new() { Activate = false }));
        var window = harness.Window<SwitchableWindow>();
        ApplicationHarness.On(window, () =>
        {
            if (state == WindowState.Maximized) window.Maximize();
            if (state == WindowState.FullScreen) window.EnterFullScreen();
            if (state == WindowState.Minimized) window.Minimize();
            window.Width = 520;
            window.Height = 320;
            Assert.Equal(state, window.State);
            window.Restore();
            Assert.Equal(WindowState.Normal, window.State);
            Assert.InRange(window.PlatformWindow.ClientSize.Width, 519, 521);
            Assert.InRange(window.PlatformWindow.ClientSize.Height, 319, 321);
            Assert.Empty(window.Exceptions);
            window.Close();
            return true;
        });
        Assert.True(harness.WaitForExit());
    }

    [Theory]
    [InlineData(WindowState.Normal)]
    [InlineData(WindowState.Maximized)]
    [InlineData(WindowState.FullScreen)]
    [InlineData(WindowState.Minimized)]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void Caption_switching_preserves_native_identity_state_and_content(WindowState state)
    {
        using var harness = new ApplicationHarness(() => ApplicationTestStartup.Run<SwitchableWindow>(new() { Activate = false }));
        var window = harness.Window<SwitchableWindow>();
        ApplicationHarness.On(window, () =>
        {
            var handle = window.PlatformWindow.Handle;
            var content = window.Root.Single("TextField").Owner;
            var root = window.Root.Component;
            if (state == WindowState.Maximized) window.Maximize();
            if (state == WindowState.FullScreen) window.EnterFullScreen();
            if (state == WindowState.Minimized) window.Minimize();
            for (var index = 0; index < 3; index++)
            {
                window.ApplyDecorations((false) ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User) : WindowDecorations.Native());
                Assert.Empty(window.Root.Elements("TitleBar"));
                window.Title = "Updated while native";
                window.ApplyDecorations((true) ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User) : WindowDecorations.Native());
                Assert.Equal(state, window.State);
                Assert.Equal(handle, window.PlatformWindow.Handle);
                Assert.Same(root, window.Root.Component);
                Assert.Same(content, window.Root.Single("TextField").Owner);
                Assert.Equal("Updated while native", window.Title);
                if (state is not (WindowState.FullScreen or WindowState.Minimized))
                {
                    var bar = window.Root.Single("TitleBar");
                    window.Scene.Update();
                    Assert.Equal(0, window.Scene.GetBounds(bar).Y);
                    Assert.Equal(window.Scene.Viewport.Width, window.Scene.GetBounds(bar).Width);
                }
            }
            Assert.Empty(window.Exceptions);
            window.Close();
            return true;
        });
        Assert.True(harness.WaitForExit());
    }

    [Fact]
    public void Window_parameters_fail_before_construction_and_accept_explicit_zero()
    {
        RequiredWindow.Constructions = 0;
        using var host = new TestHost();
        Assert.Throws<InvalidOperationException>(() => host.Render<RequiredWindow>());
        Assert.Equal(0, RequiredWindow.Constructions);
        var root = host.Render<RequiredWindow>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = 0 }));
        Assert.Equal(0, ((RequiredWindow)root.Component).Id);
        Assert.Equal(1, RequiredWindow.Constructions);
        Assert.Throws<InvalidOperationException>(() => host.Render<RequiredWindow>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = "42" })));
        Assert.Throws<InvalidOperationException>(() => host.Render<RequiredWindow>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = null })));
    }

    [Fact]
    public void A_caption_is_optional_and_nested_window_types_are_rejected_at_runtime()
    {
        using var host = new TestHost();
        var root = host.Render<RequiredWindow>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Id"] = 1 }));
        Assert.Empty(root.Elements("TitleBar"));
        Assert.Throws<InvalidOperationException>(() => host.Render<NestedWindowComponent>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Duplicate_caption_declarations_fail_even_when_the_system_frame_is_selected(bool custom)
    {
        using var host = new TestHost();
        host.Window.ApplyDecorations((custom) ? DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User) : WindowDecorations.Native());
        Assert.Throws<InvalidOperationException>(() => host.Render<DuplicateCaptionWindow>());
    }

    public sealed class DuplicateCaptionWindow : WindowComponentBase
    {
        protected override void BuildWindowContent(RenderTreeBuilder builder)
        {
            for (var index = 0; index < 2; index++)
            {
                builder.OpenComponent<UserTitleBar>(0, typeof(DuplicateCaptionWindow), this);
                builder.CloseComponent();
            }
        }
    }

    public sealed class RequiredWindow : WindowComponentBase
    {
        public static int Constructions;
        public RequiredWindow() => Constructions++;
        [Parameter, EditorRequired] public int Id { get; set; }
    }

    public sealed class NestedWindowComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<SwitchableWindow>(0);
            builder.CloseComponent();
        }
    }
}

public sealed class SwitchableWindow : WindowComponentBase
{
    protected override void OnInitialized()
    {
        Window.Title = "Switchable";
        Window.Decorations = WindowDecorationPolicy.Require(DecorationTestExtensions.ClientTitleBar(WindowTitleBarContent.User));
    }
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<global::Nazor.Controls.TextField>(0, typeof(SwitchableWindow), this);
        builder.AddComponentParameter(1, "Value", "Retained");
        builder.CloseComponent();
        builder.OpenComponent<UserTitleBar>(2, typeof(SwitchableWindow), this);
        builder.AddComponentParameter(3, "Style", "height: max(var(--nz-window-controls-height, 40px), 40px)");
        builder.AddComponentParameter(4, "ChildContent", (RenderFragment)(content => content.AddContent(0, Window.Title)));
        builder.CloseComponent();
    }
}
