using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform.Linux;
using Nazor.Platform;
using Nazor.Rendering;

namespace Nazor.Linux.Tests;

[Collection("Linux desktop")]
public sealed class PopupTests
{
    public static bool HasDesktop => DesktopLifecycleTests.HasDesktop;

    [Fact(SkipUnless = nameof(HasDesktop), Skip = "Requires an isolated Wayland or X11 display.")]
    public async Task Persistent_popovers_rehost_in_the_owner_when_inactive_and_restore_their_external_surface()
    {
        var backend = X11Tests.HasDisplay ? LinuxDesktopBackend.X11 : LinuxDesktopBackend.Wayland;
        var builder = NazorApplication.CreateBuilder(platformHost: new LinuxPlatformHost(backend));
        builder.Application.UseMainWindow<PersistentPopupWindow>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        builder.Diagnostics.ConfigureWindow = window =>
        {
            var stage = 0;
            window.FramePresented += frame =>
            {
                if (completion.Task.IsCompleted) return;
                try
                {
                    Assert.SkipUnless(window.Capabilities.CanShowPopupsOutsideWindow, "Requires transparent native popups.");
                    var root = (PersistentPopupWindow)window.Root.Component;
                    var popup = window.Scene.Overlays.FirstOrDefault();
                    if (stage == 0)
                    {
                        stage = 1;
                        ((IWindowHost)window).OnActivationChanged(true);
                        _ = root.Popover!.OpenAsync();
                    }
                    else if (stage == 1 && popup is not null && window.Scene.GetNativePopupBounds(popup) is not null)
                    {
                        stage = 2;
                        ((IWindowHost)window).OnActivationChanged(true);
                        ((IWindowHost)window).OnActivationChanged(false);
                    }
                    else if (stage == 2 && popup is not null && window.Scene.GetNativePopupBounds(popup) is null)
                    {
                        Assert.True(root.Popover!.Opened);
                        stage = 3;
                        ((IWindowHost)window).OnActivationChanged(true);
                    }
                    else if (stage == 3 && popup is not null && window.Scene.GetNativePopupBounds(popup) is not null)
                    {
                        Assert.True(root.Popover!.Opened);
                        Assert.Empty(window.Exceptions);
                        completion.SetResult();
                        window.Close();
                    }
                }
                catch (Exception error) { completion.SetException(error); window.Close(); }
            };
        };
        using var app = builder.Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Assert.Equal(0, await app.RunAsync(timeout.Token));
        await completion.Task.WaitAsync(timeout.Token);
    }

    public sealed class PersistentPopupWindow : WindowComponentBase
    {
        public Popover? Popover { get; private set; }

        protected override void OnInitialized()
        {
            Window.Width = 240;
            Window.Height = 160;
            Window.Decorations = WindowDecorationPolicy.Require(WindowDecorations.Nazor());
            Window.AllowPopupsOutsideWindow = true;
        }

        protected override void BuildWindowContent(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Popover>(0);
            builder.AddComponentParameter(1, nameof(Popover.CloseOnOutside), false);
            builder.AddComponentParameter(2, nameof(Popover.FocusOnOpen), false);
            builder.AddComponentParameter(3, nameof(Popover.Trigger), (RenderFragment)(content =>
            {
                content.OpenComponent<Button>(0);
                content.AddComponentParameter(1, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Open")));
                content.CloseComponent();
            }));
            builder.AddComponentParameter(4, nameof(Popover.ChildContent), (RenderFragment)(content => content.AddContent(0, "Persistent content")));
            builder.AddComponentReferenceCapture(5, component => Popover = (Popover)component);
            builder.CloseComponent();
        }
    }

    [Theory(SkipUnless = nameof(HasDesktop), Skip = "Requires an isolated Wayland or X11 display.")]
    [InlineData("choose")]
    [InlineData("focus")]
    [InlineData("move")]
    public async Task Editable_choices_preserve_filtering_and_dismiss_on_selection_focus_loss_or_owner_drag(string action)
    {
        var x11 = X11Tests.HasDisplay;
        var backend = x11 ? LinuxDesktopBackend.X11 : LinuxDesktopBackend.Wayland;
        var builder = NazorApplication.CreateBuilder(platformHost: new LinuxPlatformHost(backend));
        builder.Application.UseMainWindow<PopupChoiceWindow>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        builder.Diagnostics.ConfigureWindow = window =>
        {
            var stage = 0;
            float originalHeight = 0;
            var external = !x11 || Environment.GetEnvironmentVariable("NAZOR_TEST_X11_COMPOSITING") == "1";
            window.FramePresented += frame =>
            {
                if (completion.Task.IsCompleted) return;
                try
                {
                    var root = (PopupChoiceWindow)window.Root.Component;
                    var popup = window.Scene.Overlays.FirstOrDefault(element => element.Name == "ComboBoxPopup");
                    if (stage == 0)
                    {
                        if (action != "choose") Assert.SkipUnless(external, "Requires native popup surfaces.");
                        Assert.True(window.AllowPopupsOutsideWindow);
                        Assert.Equal(external, window.Capabilities.CanShowPopupsOutsideWindow);
                        stage = 1;
                        ((IWindowHost)window).OnActivationChanged(true);
                        _ = root.Combo!.ToggleAsync();
                    }
                    else if (stage == 1 && popup is not null)
                    {
                        var native = window.Scene.GetNativePopupBounds(popup);
                        var bounds = window.Scene.GetBounds(popup);
                        Assert.Equal(external, native is not null);
                        if (external) Assert.True(bounds.Bottom > window.Scene.Viewport.Height || bounds.Y < 0);
                        else Assert.True(bounds.Y >= 0 && bounds.Bottom <= window.Scene.Viewport.Height);
                        originalHeight = bounds.Height;
                        stage = 2;
                        window.Input.TextInput("Item 12");
                    }
                    else if (stage == 2 && popup is not null &&
                             popup.Children.OfType<ElementNode>().Count(element => element.Name == "ComboBoxOption") == 1)
                    {
                        Assert.True(window.Scene.GetBounds(popup).Height < originalHeight);
                        Assert.Equal("ComboBoxInput", window.Input.Focused?.Name);
                        Assert.False(window.Semantics.Find(popup.Children.OfType<ElementNode>().Single(element => element.Name == "ComboBoxOption"))!.IsOffscreen);
                        stage = 3;
                        if (action == "choose")
                        {
                            window.Input.KeyDown("ArrowDown", KeyModifiers.None, false);
                            window.Input.KeyDown("Enter", KeyModifiers.None, false);
                        }
                        else
                        {
                            var nativeOwner = (LinuxDesktopWindow)window.PlatformWindow;
                            nativeOwner.OnEvent(new LinuxNativeApi.Event { Type = 22, A = 1 });
                            if (action == "move") nativeOwner.OnEvent(new LinuxNativeApi.Event { Type = 25, A = 1 });
                            else if (x11) nativeOwner.OnEvent(new LinuxNativeApi.Event { Type = 22, A = 0 });
                            else
                            {
                                nativeOwner.OnEvent(new LinuxNativeApi.Event { Type = 9, A = 0 });
                                nativeOwner.FlushPopupFocus();
                            }
                        }
                    }
                    else if (stage == 3 && popup is null)
                    {
                        if (action == "choose") Assert.Equal("Item 12", root.Chosen);
                        Assert.Empty(window.Exceptions);
                        completion.SetResult();
                        window.Close();
                    }
                }
                catch (Exception error)
                {
                    completion.SetException(error);
                    window.Close();
                }
            };
        };
        using var app = builder.Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Assert.Equal(0, await app.RunAsync(timeout.Token));
        await completion.Task.WaitAsync(timeout.Token);
    }

    public sealed class PopupChoiceWindow : WindowComponentBase
    {
        private readonly string[] items = Enumerable.Range(1, 20).Select(index => $"Item {index:D2}").ToArray();
        public ComboBox<string>? Combo { get; private set; }
        public string? Chosen { get; private set; }

        protected override void OnInitialized()
        {
            Window.Width = 240;
            Window.Height = 80;
            Window.Decorations = WindowDecorationPolicy.Require(WindowDecorations.Nazor());
            Window.AllowPopupsOutsideWindow = true;
        }

        protected override void BuildWindowContent(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "Container");
            builder.AddAttribute(1, "style", "padding-top: 40px; padding-left: 8px");
            builder.OpenComponent<ComboBox<string>>(2);
            builder.AddComponentParameter(3, nameof(ComboBox<string>.Items), items);
            builder.AddComponentParameter(4, nameof(ComboBox<string>.Value), Chosen);
            builder.AddComponentParameter(5, nameof(ComboBox<string>.ValueChanged), EventCallback.Factory.Create<string?>(this, value => Chosen = value));
            builder.AddComponentParameter(6, nameof(ComboBox<string>.PreferredMaxVisibleItems), 4);
            builder.AddComponentReferenceCapture(7, component => Combo = (ComboBox<string>)component);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }
}
