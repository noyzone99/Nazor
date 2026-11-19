using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class Checklist : ComponentBase
{
    public bool[] Checked { get; } = new bool[30];
    public bool LastDisabled { get; set; } = true;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Checklist));
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            page.OpenComponent<Container>(3, typeof(Checklist));
            page.AddComponentParameter(4, "Class", "list");
            page.AddAttribute(5, "ChildContent", (RenderFragment)(list =>
            {
                for (var i = 0; i < Checked.Length; i++)
                {
                    var index = i;
                    list.OpenComponent<CheckBox>(6, typeof(Checklist));
                    list.SetKey(index);
                    list.AddComponentParameter(7, "IsChecked", Checked[index]);
                    list.AddComponentParameter(8, "IsCheckedChanged", EventCallback.Factory.Create<bool>(this, (bool value) => Checked[index] = value));
                    list.AddComponentParameter(9, "Disabled", LastDisabled && index == Checked.Length - 1);
                    list.AddAttribute(10, "ChildContent", (RenderFragment)(label => label.AddContent(11, $"Elemento {index + 1}")));
                    list.CloseComponent();
                }
            }));
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class ControlsTests : IDisposable
{
    private readonly RenderHarness harness = new();
    private readonly InputManager input;
    private readonly RootComponent root;
    private readonly Checklist checklist;

    public ControlsTests()
    {
        var sheet = NssCompiler.Compile("Checklist", """
            .page { display: flex; flex-direction: column; height: 100%; padding: 12px; }
            .list { flex: 1; overflow-y: auto; border: 1px solid #c8c8c8; padding: 4px 8px; }
            CheckBox { padding: 4px 0; }
            """).StyleSheet;
        StyleRegistry.RegisterScoped(typeof(Checklist), () => sheet);
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        root = harness.Host.Render<Checklist>();
        checklist = (Checklist)root.Component;
        harness.Render(300, 200, 1);
    }

    public void Dispose()
    {
        input.Dispose();
        harness.Dispose();
    }

    private ElementNode Box(int index) => root.Elements("CheckBox").ElementAt(index);

    private ElementNode List => root.Elements("Container").Single(e => e.Class == "list");

    private Point Center(ElementNode element)
    {
        var bounds = harness.Scene.GetBounds(element);
        return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    private void Click(ElementNode element)
    {
        var point = Center(element);
        input.PointerPressed(point, PointerButton.Primary, KeyModifiers.None);
        input.PointerReleased(point, PointerButton.Primary, KeyModifiers.None);
        harness.Render(300, 200, 1);
    }

    [Fact]
    public void Clicking_a_check_box_toggles_the_bound_value_and_draws_the_mark()
    {
        Click(Box(0));
        Assert.True(checklist.Checked[0]);
        Assert.True(Box(0).States.HasFlag(ElementStates.Checked));
        var mark = Assert.Single(harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>());
        Assert.Equal(new Graphics.Color(255, 255, 255, 255), mark.Color);
        Assert.Equal(Style.SystemColors.Resolve().AccentColor, harness.Scene.GetStyle(root.Elements("CheckBoxMark").First()).BackgroundColor);

        Click(Box(0));
        Assert.False(checklist.Checked[0]);
        Assert.Empty(harness.Scene.BuildDisplayList().Commands.OfType<DrawPolyline>());
    }

    [Fact]
    public void Space_toggles_the_focused_check_box_and_disabled_ones_ignore_input()
    {
        input.KeyDown("Tab", KeyModifiers.None, false);
        input.KeyDown(" ", KeyModifiers.None, false);
        input.KeyUp(" ", KeyModifiers.None);
        Assert.True(checklist.Checked[0]);

        var disabled = Box(29);
        Assert.True(disabled.States.HasFlag(ElementStates.Disabled));
        harness.Scene.BringIntoView(disabled);
        harness.Render(300, 200, 1);
        Click(disabled);
        Assert.False(checklist.Checked[29]);
    }


    [Fact]
    public void Continuous_scroll_is_not_multiplied_by_the_wheel_preference()
    {
        var scroll = harness.Scene.GetScrollState(List)!;
        var point = Center(Box(2));
        input.WheelScrollDistance = 96;
        input.PointerWheel(point, 0, -7.5f, KeyModifiers.None, ScrollDeltaUnit.LogicalPixels);
        Assert.Equal(7.5f, scroll.OffsetY);
        input.PointerWheel(point, 0, -1, KeyModifiers.None);
        Assert.Equal(103.5f, scroll.OffsetY);
    }

    [Fact]
    public void The_wheel_scrolls_the_list_and_hit_testing_follows_the_content()
    {
        var scroll = harness.Scene.GetScrollState(List)!;
        Assert.True(scroll.CanScroll);
        var firstBefore = harness.Scene.GetBounds(Box(0));
        var probe = Center(Box(2));

        input.PointerWheel(probe, -1, KeyModifiers.None);
        harness.Render(300, 200, 1);

        Assert.Equal(48, scroll.OffsetY);
        Assert.Equal(firstBefore.Y - 48, harness.Scene.GetBounds(Box(0)).Y);
        var hit = harness.Scene.HitTest(probe);
        Assert.NotNull(hit);
        Assert.NotSame(Box(2), hit!.Name == "CheckBox" ? hit : hit.ParentElement);
        var listTop = harness.Scene.GetBounds(List).Y;
        Assert.Equal("Container", harness.Scene.HitTest(new Point(probe.X, listTop - 4))!.Name);

        var commands = harness.Scene.BuildDisplayList().Commands;
        Assert.Contains(commands, c => c is PushTranslate { Y: -48 });
        Assert.Contains(commands, c => c is PushClip);

        input.PointerWheel(probe, 100, KeyModifiers.None);
        Assert.Equal(0, scroll.OffsetY);
    }

    [Fact]
    public void Focus_within_marks_the_focused_element_and_its_ancestors()
    {
        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.Same(Box(0), input.Focused);
        Assert.True(Box(0).States.HasFlag(ElementStates.FocusWithin));
        Assert.True(List.States.HasFlag(ElementStates.FocusWithin));
        Assert.False(Box(1).States.HasFlag(ElementStates.FocusWithin));

        input.KeyDown("Tab", KeyModifiers.None, false);
        Assert.False(Box(0).States.HasFlag(ElementStates.FocusWithin));
        Assert.True(Box(1).States.HasFlag(ElementStates.FocusWithin));
        Assert.True(List.States.HasFlag(ElementStates.FocusWithin));

        input.Focus(null, visible: false);
        Assert.False(List.States.HasFlag(ElementStates.FocusWithin));
    }

    [Fact]
    public void Tab_reveals_the_focused_check_box_and_page_down_scrolls()
    {
        for (var i = 0; i < 20; i++) input.KeyDown("Tab", KeyModifiers.None, false);
        harness.Render(300, 200, 1);
        Assert.Same(Box(19), input.Focused);
        var list = harness.Scene.GetBounds(List);
        var focused = harness.Scene.GetBounds(Box(19));
        Assert.True(focused.Y >= list.Y && focused.Bottom <= list.Bottom, $"{focused} not inside {list}");

        var before = harness.Scene.GetScrollState(List)!.OffsetY;
        Assert.True(input.KeyDown("PageDown", KeyModifiers.None, false));
        Assert.True(harness.Scene.GetScrollState(List)!.OffsetY > before);
        Assert.True(input.KeyDown("Home", KeyModifiers.None, false));
        Assert.Equal(0, harness.Scene.GetScrollState(List)!.OffsetY);
    }

    [Fact]
    public void Scrolled_checklist_matches_the_reference_image()
    {
        Click(Box(1));
        Click(Box(4));
        input.PointerWheel(Center(Box(2)), -0.5f, KeyModifiers.None);
        var image = harness.Render(300, 200, 1.5f);
        ImageAssert.MatchesBaseline(harness.Graphics, image, "checklist-scrolled-300x200@1.5x");
    }
}

[CollectionDefinition("StyleRegistry", DisableParallelization = true)]
public sealed class StyleRegistryCollection;
