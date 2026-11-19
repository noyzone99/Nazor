using System.Runtime.InteropServices;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class TitleBarInteractionTests
{
    [DllImport("user32.dll")]
    private static extern nint SendMessageW(nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(POINT point);

    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.22000")]
    public void Desktop_hit_testing_reaches_the_caption_control_instead_of_the_AppWindow_input_sink()
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000), "AppWindow input regions require Windows 11.");
        using var h = new WindowHarness(typeof(InteractionWindow), new()
        {
            Rendering = RenderingMode.Cpu, Theme = Nazor.Themes.Fluent.Themes.Fluent,
            Decorations = WindowDecorationPolicy.Require(WindowDecorations.Native(WindowTitleBarContent.User, WindowCaptionButtons.Native)),
        });
        h.WaitUntil(w => w.FrameCount > 0, "caption layout");
        h.Read(w => { w.Activate(); return true; });
        Assert.SkipUnless(GetForegroundWindow() == h.Handle, "Desktop hit testing requires a foreground test window.");
        h.Read(w =>
        {
            var bounds = w.Scene.GetBounds(w.Root.Elements().Single(e => e.Id == "caption-search"));
            var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + 2);
            Assert.Equal(1, NativeHit(w, point));
            var screen = new POINT { X = (int)MathF.Round(point.X * w.Scene.Scale), Y = (int)MathF.Round(point.Y * w.Scene.Scale) };
            Assert.True(ClientToScreen(h.Handle, ref screen));
            Assert.Equal(h.Handle, WindowFromPoint(screen));
            var source = Microsoft.UI.Input.InputNonClientPointerSource.GetForWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(h.Handle));
            Assert.Contains(source.GetRegionRects(Microsoft.UI.Input.NonClientRegionKind.Passthrough), r =>
                point.X * w.Scene.Scale >= r.X && point.X * w.Scene.Scale < r.X + r.Width &&
                point.Y * w.Scene.Scale >= r.Y && point.Y * w.Scene.Scale < r.Y + r.Height);
            return true;
        });
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("fluent")]
    [InlineData("leaf")]
    [InlineData("none")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.22000")]
    public void Native_passthrough_tracks_caption_geometry_and_clears_for_system_decorations(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000), "AppWindow input regions require Windows 11.");
        var client = WindowDecorations.Native(WindowTitleBarContent.User, WindowCaptionButtons.Native);
        using var h = new WindowHarness(typeof(InteractionWindow), new()
        {
            Rendering = RenderingMode.Cpu,
            Theme = theme switch
            {
                "fluent" => Nazor.Themes.Fluent.Themes.Fluent,
                "leaf" => Nazor.Themes.NazorLeaf.NazorLeafTheme.Theme,
                "none" => Nazor.Themes.Themes.None,
                _ => Nazor.Themes.Themes.Basic,
            },
            Decorations = WindowDecorationPolicy.Require(client),
        });
        h.WaitUntil(w => w.FrameCount > 0, "caption layout");
        foreach (var width in new[] { 610, 1360 })
            h.Read(w => { w.Width = width; AssertRegion(w); return true; });
        h.Read(w => { w.Style = "-windows-title-bar-height: tall"; return true; });
        h.WaitUntil(w => Microsoft.UI.Windowing.AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(h.Handle))
            .TitleBar.PreferredHeightOption == Microsoft.UI.Windowing.TitleBarHeightOption.Tall, "tall native caption");
        h.Read(w =>
        {
            AssertRegion(w);
            var source = Microsoft.UI.Input.InputNonClientPointerSource.GetForWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(h.Handle));
            w.ApplyDecorations(WindowDecorations.Native());
            Assert.Empty(source.GetRegionRects(Microsoft.UI.Input.NonClientRegionKind.Passthrough) ?? []);
            w.ApplyDecorations(client);
            AssertRegion(w);
            Assert.Empty(w.Exceptions);
            return true;
        });

        static void AssertRegion(NazorWindow w)
        {
            var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(w.PlatformWindow.Handle);
            var app = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
            var region = Assert.Single(Microsoft.UI.Input.InputNonClientPointerSource.GetForWindowId(id).GetRegionRects(Microsoft.UI.Input.NonClientRegionKind.Passthrough));
            Assert.Equal(app.TitleBar.LeftInset, region.X);
            Assert.Equal(0, region.Y);
            Assert.Equal(app.ClientSize.Width - app.TitleBar.LeftInset - app.TitleBar.RightInset, region.Width);
            Assert.Equal(app.TitleBar.Height, region.Height);
            Assert.True(region.Height > 0);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Custom_caption_controls_keep_native_input_tooltips_and_suggestions(bool nativeFrame)
    {
        using var h = new WindowHarness(typeof(InteractionWindow), new()
        {
            Rendering = RenderingMode.Cpu, Theme = Nazor.Themes.Fluent.Themes.Fluent,
            Decorations = WindowDecorationPolicy.Require(nativeFrame
                ? WindowDecorations.Native(WindowTitleBarContent.User, WindowCaptionButtons.Native)
                : WindowDecorations.Nazor(WindowTitleBarContent.User, WindowCaptionButtons.Nazor)),
        });
        h.WaitUntil(w => w.FrameCount > 0, "caption layout");
        var points = h.Read(w =>
        {
            var field = w.Scene.GetBounds(w.Root.Elements().Single(e => e.Id == "caption-search"));
            var custom = w.Scene.GetBounds(w.Root.Single("CustomCaptionControl"));
            var gap = new Point(760, 30);
            Assert.Equal(WindowRegion.Caption, w.Input.RegionAt(gap));
            Assert.Equal(2, NativeHit(w, gap));
            w.Input.TooltipDelay = TimeSpan.Zero;
            return new[] { new Point(field.X + 2, field.Y + field.Height / 2), new Point(field.X + field.Width / 2, field.Y + 2), new Point(field.X + field.Width / 2, field.Bottom - 2), new Point(custom.X + 4, custom.Y + 4) };
        });
        foreach (var point in points.Take(3))
        {
            h.Read(w => { Assert.Equal(1, NativeHit(w, point)); return true; });
            h.Click(point);
            h.WaitUntil(w => w.Input.Focused?.Id == "caption-search-input", "field frame focus");
            h.Click(point);
            h.WaitUntil(w => w.Input.Focused?.Id == "caption-search-input", "repeated field frame focus");
        }
        h.Character('R');
        h.WaitUntil(w => w.Root.Elements("SearchFieldOption").Any(), "caption suggestions");
        var optionPoint = h.Read(w => Center(w.Scene.GetBounds(w.Root.Elements("SearchFieldOption").First())));
        h.Click(optionPoint);
        h.WaitUntil(w => ((InteractionWindow)w.Root.Component).Chosen == "Rome", "suggestion choice");
        h.Read(w => { Assert.Equal(1, NativeHit(w, points[3])); return true; });
        h.Click(points[3]);
        h.WaitUntil(w => ((InteractionWindow)w.Root.Component).Presses == 1, "custom pointer callback");
        h.Wheel(points[3], 120);
        h.WaitUntil(w => ((InteractionWindow)w.Root.Component).Wheels == 1, "custom wheel callback");
        h.Mouse(WM_RBUTTONDOWN, points[3], 2);
        h.Mouse(WM_RBUTTONUP, points[3]);
        h.WaitUntil(w => ((InteractionWindow)w.Root.Component).Menus == 1, "custom context menu callback");
        h.Mouse(WM_MOUSEMOVE, new Point(760, 30));
        h.Mouse(WM_MOUSEMOVE, points[3]);
        h.WaitUntil(w =>
        {
            Move(w, points[3]);
            return w.Input.TooltipTarget is not null && w.Scene.Overlays.Any(e => e.Attributes.GetValueOrDefault("role") is "tooltip");
        }, "caption tooltip");
        h.Read(w =>
        {
            Move(w, points[3]);
            Assert.Contains(w.Scene.Overlays, e => e.Attributes.GetValueOrDefault("role") is "tooltip");
            Assert.True((w.Root.Single("CustomCaptionControl").States & ElementStates.Hover) != 0);
            Assert.Empty(w.Exceptions);
            return true;
        });
        h.Mouse(WM_MOUSELEAVE, points[3]);
        h.WaitUntil(w => w.Input.TooltipTarget is null, "tooltip dismissal");
        var richPoint = h.Read(w => Center(w.Scene.GetBounds(w.Root.Elements().Single(e => e.Id == "rich-trigger"))));
        h.Read(w => { Assert.Equal(1, NativeHit(w, richPoint)); return true; });
        h.Mouse(WM_MOUSEMOVE, richPoint);
        h.WaitUntil(w =>
        {
            Move(w, richPoint);
            return w.Input.TooltipTarget == w.Root.Single("TooltipTrigger") &&
                w.Scene.Overlays.Any(e => e.Attributes.GetValueOrDefault("role") is "tooltip" && e.Attributes.GetValueOrDefault("exiting") is not true);
        }, "rich caption tooltip");
        h.Read(w =>
        {
            Move(w, richPoint);
            var surface = Assert.Single(w.Scene.Overlays, e => e.Attributes.GetValueOrDefault("role") is "tooltip" && e.Attributes.GetValueOrDefault("exiting") is not true);
            var contentPoint = Center(w.Scene.GetBounds(surface));
            Assert.NotSame(surface, w.Scene.HitTest(contentPoint));
            Move(w, contentPoint);
            Assert.NotNull(w.Input.TooltipTarget);
            Move(w, new Point(760, 130));
            Assert.Null(w.Input.TooltipTarget);
            Assert.Empty(w.Exceptions);
            return true;
        });
    }

    private static Point Center(Rect bounds) => new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

    private static void Move(NazorWindow window, Point point)
    {
        var x = (int)MathF.Round(point.X * window.Scene.Scale);
        var y = (int)MathF.Round(point.Y * window.Scene.Scale);
        SendMessageW(window.PlatformWindow.Handle, WM_MOUSEMOVE, 0, (nint)(((long)y << 16) | (ushort)x));
        window.Scene.Update();
    }

    private static long NativeHit(NazorWindow window, Point point)
    {
        var screen = new POINT { X = (int)MathF.Round(point.X * window.Scene.Scale), Y = (int)MathF.Round(point.Y * window.Scene.Scale) };
        ClientToScreen(window.PlatformWindow.Handle, ref screen);
        return (long)SendMessageW(window.PlatformWindow.Handle, WM_NCHITTEST, 0, (nint)(((long)screen.Y << 16) | (ushort)screen.X));
    }

    public sealed class InteractionWindow : WindowComponentBase
    {
        public int Presses, Wheels, Menus;
        public string? Chosen;
        protected override void OnInitialized() { Window.Width = 1000; Window.Height = 300; }
        protected override void BuildWindowContent(RenderTreeBuilder b)
        {
            b.OpenComponent<UserTitleBar>(0, typeof(InteractionWindow), this);
            b.AddComponentParameter(1, nameof(Control.Style), "min-height: 64px;");
            b.AddComponentParameter(2, nameof(TitleBar.ChildContent), (RenderFragment)(content =>
            {
                content.OpenComponent<SearchField<string>>(0);
                content.AddComponentParameter(1, nameof(Control.Id), "caption-search");
                content.AddComponentParameter(2, nameof(Control.Style), "width: 320px;");
                content.AddComponentParameter(3, nameof(SearchField<string>.Suggestions), new[] { "Rome", "Paris" });
                content.AddComponentParameter(4, nameof(SearchField<string>.SuggestionChosen), EventCallback.Factory.Create<string>(this, value => Chosen = value));
                content.CloseComponent();
                content.OpenElement(10, "CustomCaptionControl");
                content.AddAttribute(11, "style", "width: 160px; height: 36px; margin-left: 24px;");
                content.AddAttribute(12, "tooltip", "Custom caption description");
                content.AddAttribute(13, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, e => { if (e.Button == PointerButton.Primary) Presses++; }));
                content.AddAttribute(14, "onwheel", EventCallback.Factory.Create<WheelEventArgs>(this, _ => Wheels++));
                content.AddAttribute(15, "oncontextmenu", EventCallback.Factory.Create<PointerEventArgs>(this, _ => Menus++));
                content.CloseElement();
                content.OpenComponent<Tooltip>(20);
                content.AddComponentParameter(21, nameof(Tooltip.Text), "Rich caption description");
                content.AddComponentParameter(22, nameof(Tooltip.Delay), TimeSpan.Zero);
                content.AddComponentParameter(23, nameof(Tooltip.Trigger), (RenderFragment)(trigger =>
                {
                    trigger.OpenComponent<Container>(0);
                    trigger.AddComponentParameter(1, nameof(Control.Id), "rich-trigger");
                    trigger.AddComponentParameter(2, nameof(Control.Style), "width: 100px; height: 32px;");
                    trigger.CloseComponent();
                }));
                content.CloseComponent();
            }));
            b.CloseComponent();
        }
    }
}
