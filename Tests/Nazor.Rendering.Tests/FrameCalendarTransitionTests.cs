using System.Globalization;
using Nazor.Animation;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;
using Calendar = Nazor.Controls.Calendar;
using Frame = Nazor.Controls.Frame;
using FluentThemes = Nazor.Themes.Fluent.Themes;

namespace Nazor.Rendering.Tests;

public sealed class MotionFramePage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Frame>(0);
        builder.AddComponentParameter(1, nameof(Frame.Assemblies), Array.Empty<System.Reflection.Assembly>());
        builder.AddComponentParameter(2, nameof(Frame.Transition), ContentTransitionKind.Slide);
        builder.AddComponentParameter(3, nameof(Frame.NotFound), (RenderFragment)(content => content.AddContent(0, "Not found")));
        builder.CloseComponent();
    }
}

public sealed class MotionNavigationPage : ComponentBase
{
    [Parameter] public int Id { get; set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Button>(0);
        builder.AddComponentParameter(1, nameof(Control.Id), "page-" + Id.ToString(CultureInfo.InvariantCulture));
        builder.AddComponentParameter(2, nameof(Button.ChildContent), (RenderFragment)(content => content.AddContent(0, Id)));
        builder.CloseComponent();
    }
}

internal sealed class MotionNavigationServices : IServiceProvider
{
    public NavigationManager Navigation { get; } = new();
    public KeyBindings Keys { get; } = new();
    public object? GetService(Type serviceType) => serviceType == typeof(NavigationManager) ? Navigation : serviceType == typeof(KeyBindings) ? Keys : null;
}

public sealed class FrameCalendarTransitionTests
{
    private static void Frame(RenderHarness harness)
    {
        for (var i = 0; i < 3; i++)
        {
            harness.Host.RunPending();
            harness.Scene.Update();
            harness.Scene.BuildDisplayList();
        }
    }

    private static void Retain(RenderHarness harness)
    {
        var sheet = NssCompiler.Compile("transition-lifetime", "FrameTransition, CalendarTransition { overlay-exit-duration: 5000ms; }");
        Assert.True(sheet.Success, string.Join("\n", sheet.Diagnostics));
        harness.Scene.Styles.Application = sheet.StyleSheet;
        harness.Scene.Viewport = new Size(640, 480);
        harness.Scene.Transitions.ReducedMotion = false;
    }

    private static NavigationEntry PageEntry(int id) => new(null, typeof(MotionNavigationPage), new Dictionary<string, object?> { ["id"] = id.ToString(CultureInfo.InvariantCulture) });

    [Fact]
    public async Task Frame_keeps_outgoing_page_parameters_and_uses_history_direction_without_reusing_hidden_focus()
    {
        var services = new MotionNavigationServices();
        await services.Navigation.NavigateToAsync(PageEntry(1));
        using var harness = new RenderHarness(services);
        using var input = new InputManager(harness.Host.Renderer, harness.Scene, services.Keys);
        Retain(harness);
        var root = harness.Host.Render<MotionFramePage>();
        Frame(harness);
        var first = root.Elements("Button").Single();
        input.Focus(first, visible: true);
        await services.Navigation.NavigateToAsync(PageEntry(2));
        Frame(harness);
        Assert.Same(first, root.Elements("Button").Single(element => element.Id == "page-1"));
        Assert.True(first.IsDisabled);
        Assert.Equal("page-2", input.Focused?.Id);
        Assert.Equal("forward", root.Elements("FrameTransitionLayer").Single(element => element.Attributes.GetValueOrDefault("current") is true).Attributes["direction"]);
        await services.Navigation.GoBackAsync();
        Frame(harness);
        Assert.True(first.IsRemoved);
        Assert.Equal("page-1", input.Focused?.Id);
        var current = root.Elements("FrameTransitionLayer").Single(element => element.Attributes.GetValueOrDefault("current") is true);
        Assert.Equal("backward", current.Attributes["direction"]);
        Assert.Equal(2, root.Elements("FrameTransitionLayer").Count());
        using var cancellation = services.Navigation.RegisterLocationChangingHandler(context => { context.PreventNavigation(); return ValueTask.CompletedTask; });
        Assert.False(await services.Navigation.NavigateToAsync(PageEntry(3)));
        Frame(harness);
        Assert.Same(current, root.Elements("FrameTransitionLayer").Single(element => element.Attributes.GetValueOrDefault("current") is true));
        harness.Scene.Transitions.ReducedMotion = true;
        Frame(harness);
        Assert.Single(root.Elements("FrameTransitionLayer"));
    }

    private static RootComponent CalendarRoot(RenderHarness harness) => harness.Host.Render<Calendar>(ParameterView.FromDictionary(new Dictionary<string, object?>
    {
        [nameof(Calendar.Culture)] = CultureInfo.GetCultureInfo("en-US"),
        [nameof(Calendar.Value)] = new DateOnly(2026, 11, 12),
        [nameof(Calendar.Today)] = new DateOnly(2026, 11, 12),
    }));

    [Fact]
    public void Calendar_zoom_keeps_the_old_grid_snapshot_and_header_identity_with_one_active_keyboard_grid()
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        Retain(harness);
        var root = CalendarRoot(harness);
        Frame(harness);
        var calendar = (Calendar)root.Component;
        var title = root.Single("CalendarTitle");
        var active = calendar.ActiveElement.Element;
        input.Focus(active, visible: true);
        Assert.True(input.Activate(title));
        Frame(harness);
        Assert.Equal(CalendarViewMode.Year, calendar.CurrentView);
        Assert.Same(title, root.Single("CalendarTitle"));
        Assert.Equal(42, root.Elements("CalendarDay").Count());
        Assert.Equal(12, root.Elements("CalendarItem").Count());
        Assert.True(active!.IsDisabled);
        Assert.False(input.Activate(active));
        Assert.Equal("CalendarItem", calendar.ActiveElement.Element?.Name);
        Assert.Same(calendar.ActiveElement.Element, input.Focused);
        Assert.Equal("November 2026", root.Elements("CalendarGrid").Single(element => element.Attributes.GetValueOrDefault("view") is "month").Attributes["accessible-name"]);
        var item = calendar.ActiveElement.Element!;
        Assert.True(input.Activate(item));
        Frame(harness);
        Assert.Equal(CalendarViewMode.Month, calendar.CurrentView);
        Assert.Equal(2, root.Elements("CalendarTransitionLayer").Count());
        Assert.Equal(42, root.Elements("CalendarDay").Count());
        Assert.Equal("drillin", root.Elements("CalendarTransitionLayer").Single(element => element.Attributes.GetValueOrDefault("current") is true).Attributes["transition"]);
        harness.Scene.Transitions.ReducedMotion = true;
        Frame(harness);
        Assert.Single(root.Elements("CalendarGrid"));
    }

    [Fact]
    public void Calendar_paging_freezes_the_outgoing_month_and_bounds_rapid_navigation_lifetimes()
    {
        using var harness = new RenderHarness();
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        Retain(harness);
        var root = CalendarRoot(harness);
        Frame(harness);
        Assert.True(input.Activate(root.Single("CalendarNext")));
        Frame(harness);
        Assert.Equal(["December 2026", "November 2026"], root.Elements("CalendarGrid").Select(element => element.Attributes["accessible-name"]));
        Assert.True(input.Activate(root.Single("CalendarPrevious")));
        Frame(harness);
        Assert.Equal(["November 2026", "December 2026"], root.Elements("CalendarGrid").Select(element => element.Attributes["accessible-name"]));
        Assert.Equal("backward", root.Elements("CalendarTransitionLayer").Single(element => element.Attributes.GetValueOrDefault("current") is true).Attributes["direction"]);
    }

    [Fact]
    public void Fluent_calendar_zoom_uses_delayed_incoming_scale_and_a_separate_outgoing_timeline()
    {
        var clock = new ManualAnimationClock();
        using var harness = new RenderHarness(clock: clock);
        using var input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Styles.Theme = FluentThemes.Fluent;
        harness.Scene.Viewport = new Size(640, 480);
        harness.Scene.Transitions.ReducedMotion = false;
        var root = CalendarRoot(harness);
        Frame(harness);
        Assert.True(input.Activate(root.Single("CalendarTitle")));
        Frame(harness);
        var outgoing = root.Elements("CalendarTransitionLayer").Single(element => element.Attributes.GetValueOrDefault("exiting") is true);
        var incoming = root.Elements("CalendarTransitionLayer").Single(element => element.Attributes.GetValueOrDefault("current") is true);
        var initialScale = Assert.IsType<ScaleFunction>(harness.Scene.GetEffectiveValue(incoming, StyleProperties.Transform).Items.Single());
        Assert.Equal(1.29, initialScale.X, 3);
        Assert.Equal(0, harness.Scene.GetEffectiveValue(incoming, StyleProperties.Opacity));
        clock.Advance(150);
        Frame(harness);
        Assert.Equal(0, harness.Scene.GetEffectiveValue(outgoing, StyleProperties.Opacity));
        Assert.Equal(0.84, Assert.IsType<ScaleFunction>(harness.Scene.GetEffectiveValue(outgoing, StyleProperties.Transform).Items.Single()).X, 3);
        Assert.Equal(0, harness.Scene.GetEffectiveValue(incoming, StyleProperties.Opacity));
        clock.Advance(175);
        Frame(harness);
        Assert.InRange(harness.Scene.GetEffectiveValue(incoming, StyleProperties.Opacity), 0.01, 0.99);
        Assert.InRange(Assert.IsType<ScaleFunction>(harness.Scene.GetEffectiveValue(incoming, StyleProperties.Transform).Items.Single()).X, 1.001, 1.289);
        clock.Advance(175);
        Frame(harness);
        Assert.Equal(1, harness.Scene.GetEffectiveValue(incoming, StyleProperties.Opacity));
        Assert.Empty(harness.Scene.GetEffectiveValue(incoming, StyleProperties.Transform).Items);
        harness.Scene.Transitions.ReducedMotion = true;
        Frame(harness);
        Assert.Single(root.Elements("CalendarTransitionLayer"));
    }
}
