using Nazor.Animation;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Nss;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class ItemsTransitionPage : ComponentBase
{
    public IReadOnlyList<int> Items { get; set; } = [1, 2, 3];
    public int MaximumRetained { get; set; } = 32;
    public TransitionContentLog Log { get; } = new();
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<ItemsTransition<int>>(0);
        builder.AddComponentParameter(1, nameof(ItemsTransition<int>.Items), Items);
        builder.AddComponentParameter(2, nameof(ItemsTransition<int>.ItemKey), (Func<int, object>)(value => value));
        builder.AddComponentParameter(3, nameof(ItemsTransition<int>.MaximumRetainedItems), MaximumRetained);
        builder.AddComponentParameter(4, nameof(ItemsTransition<int>.ItemTemplate), (RenderFragment<int>)(value => content =>
        {
            content.OpenComponent<StatefulTransitionContent>(0);
            content.AddComponentParameter(1, nameof(StatefulTransitionContent.Caption), value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            content.AddComponentParameter(2, nameof(StatefulTransitionContent.Log), Log);
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class ItemsTransitionTests : IDisposable
{
    private readonly ManualAnimationClock clock = new();
    private readonly RenderHarness harness;
    private readonly InputManager input;
    private readonly RootComponent root;
    private ItemsTransitionPage Page => (ItemsTransitionPage)root.Component;
    private ElementNode Button(int value) => root.Elements("Button").Single(element => element.Id == value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private ElementNode Item(int value) => Button(value).ParentElement!.ParentElement!;

    public ItemsTransitionTests()
    {
        harness = new RenderHarness(clock: clock);
        input = new InputManager(harness.Host.Renderer, harness.Scene);
        harness.Scene.Viewport = new Size(400, 400);
        harness.Scene.Transitions.ReducedMotion = false;
        var sheet = NssCompiler.Compile("items-lifetime", """
            ItemsTransition { display: flex; flex-direction: column; width: 200px; overlay-exit-duration: 5000ms; }
            ItemsTransitionItem { display: block; height: 40px; transition: transform 1000ms linear; }
            ItemsTransitionContent { display: block; }
            """);
        Assert.True(sheet.Success, string.Join("\n", sheet.Diagnostics));
        harness.Scene.Styles.Application = sheet.StyleSheet;
        root = harness.Host.Render<ItemsTransitionPage>();
        Frame();
    }

    public void Dispose() { input.Dispose(); harness.Dispose(); }

    private void Frame()
    {
        for (var i = 0; i < 3; i++)
        {
            harness.Host.RunPending();
            harness.Scene.Update();
            harness.Scene.BuildDisplayList();
        }
    }

    private void SetItems(params int[] values)
    {
        Page.Items = values;
        Page.Refresh();
        Frame();
    }

    [Fact]
    public void Reordering_preserves_component_identity_and_animates_layout_without_moving_focus()
    {
        var first = Button(1);
        var firstBox = harness.Scene.GetBounds(Item(1));
        var lastBox = harness.Scene.GetBounds(Item(3));
        input.Focus(first, visible: true);
        SetItems(3, 2, 1);
        Assert.Same(first, Button(1));
        Assert.Same(first, input.Focused);
        Assert.Equal(3, Page.Log.Created);
        Assert.Equal(firstBox, harness.Scene.GetBounds(Item(1)));
        clock.Advance(500);
        Frame();
        Assert.Equal((firstBox.Y + lastBox.Y) / 2, harness.Scene.GetBounds(Item(1)).Y, 2);
        clock.Advance(500);
        Frame();
        Assert.Equal(lastBox.Y, harness.Scene.GetBounds(Item(1)).Y, 2);
    }

    [Fact]
    public void Removing_the_focused_item_retains_its_box_but_moves_input_to_the_adjacent_survivor()
    {
        var first = Button(1);
        var box = harness.Scene.GetBounds(Item(1));
        input.Focus(first, visible: true);
        SetItems(2, 3);
        Assert.Same(first, Button(1));
        Assert.Equal(box, harness.Scene.GetBounds(Item(1)));
        Assert.True(first.IsDisabled);
        Assert.Equal(true, Item(1).Attributes["semantic-hidden"]);
        Assert.False(input.Activate(first));
        Assert.Same(Button(2), input.Focused);
        Assert.True(input.Activate(Button(2)));
        Assert.Equal(1, Page.Log.Actions);
    }

    [Fact]
    public void Reintroducing_a_retained_identity_cancels_its_exit_and_keeps_local_component_state()
    {
        var first = Button(1);
        SetItems(2, 3);
        SetItems(1, 2, 3);
        Assert.Same(first, Button(1));
        Assert.False(first.IsDisabled);
        Assert.Equal(3, Page.Log.Created);
        Assert.Equal(0, Page.Log.Disposed);
        harness.Scene.Transitions.ReducedMotion = true;
        Frame();
        Assert.Equal(3, root.Elements("ItemsTransitionItem").Count());
        Assert.False(first.IsRemoved);
    }

    [Fact]
    public void Retained_removals_are_bounded_and_reduced_motion_removes_the_remaining_exit()
    {
        Page.MaximumRetained = 1;
        SetItems(2, 3);
        SetItems(3);
        Assert.DoesNotContain(root.Elements("Button"), element => element.Id == "1");
        Assert.Equal(1, Page.Log.Disposed);
        Assert.Equal(2, root.Elements("ItemsTransitionItem").Count());
        harness.Scene.Transitions.ReducedMotion = true;
        Frame();
        Assert.Single(root.Elements("ItemsTransitionItem"));
        Assert.Equal(2, Page.Log.Disposed);
        input.Focus(Button(3), visible: true);
        SetItems();
        Assert.Empty(root.Elements("ItemsTransitionItem"));
        Assert.Same(root.Single("ItemsTransition"), input.Focused);
    }
}
