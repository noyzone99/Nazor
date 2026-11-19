using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;

namespace Nazor.Linux.Tests;

public sealed class PopupGeometryTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(1.25, false)]
    [InlineData(1.5, false)]
    [InlineData(2, false)]
    [InlineData(1, true)]
    [InlineData(1.5, true)]
    public void Native_popup_geometry_preserves_content_sizing_and_real_compositor_constraints(float scale, bool constrained)
    {
        var dispatcher = new UIDispatcher();
        using var renderer = new Renderer(dispatcher);
        renderer.AddRootComponent(typeof(ContextMenuContent));
        using var graphics = new SkiaRenderer();
        using var scene = new Scene(renderer, graphics.Fonts, graphics.Shaper)
        {
            Viewport = new Size(640, 480),
            Scale = scale,
        };
        scene.Styles.Theme = StyleSheets.Default;
        scene.NativePopupWorkArea = _ => new Rect(0, 0, 1200, 900);
        scene.ArrangeNativePopup = (_, placement) => placement.Bounds with
        {
            Height = constrained ? Math.Min(50, placement.Bounds.Height) : placement.Bounds.Height,
        };
        scene.Update();
        scene.Update();
        var menu = Assert.Single(scene.Overlays);
        Assert.Equal(constrained, scene.GetScrollState(menu)!.CanScrollY);
        Assert.Equal(constrained, scene.GetScrollBar(menu, horizontal: false) is not null);
    }

    public sealed class ContextMenuContent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Menu>(0);
            builder.AddComponentParameter(1, nameof(Menu.IsOpen), true);
            builder.AddComponentParameter(2, nameof(Menu.AnchorPoint), (60d, 80d));
            builder.AddComponentParameter(3, nameof(Menu.ChildContent), (RenderFragment)(items =>
            {
                foreach (var label in new[] { "Copy", "Paste", "More commands" })
                {
                    items.OpenComponent<MenuItem>(0);
                    items.AddComponentParameter(1, nameof(MenuItem.ChildContent), (RenderFragment)(text => text.AddContent(0, label)));
                    items.CloseComponent();
                }
                items.OpenComponent<MenuSeparator>(10);
                items.CloseComponent();
                items.OpenComponent<MenuItem>(20);
                items.AddComponentParameter(21, nameof(MenuItem.Disabled), true);
                items.AddComponentParameter(22, nameof(MenuItem.ChildContent), (RenderFragment)(text => text.AddContent(0, "Unavailable command")));
                items.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
