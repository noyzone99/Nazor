using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Nss;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class ScopedTooltipPage : ComponentBase
{
    public TooltipContainer Tooltip { get; private set; } = null!;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TooltipContainer>(0, typeof(ScopedTooltipPage));
        builder.AddComponentParameter(1, nameof(Control.Class), "custom-tooltip");
        builder.AddComponentParameter(2, nameof(TooltipContainer.Trigger), (RenderFragment)(content => content.AddContent(0, "Trigger")));
        builder.AddComponentParameter(3, nameof(TooltipContainer.ChildContent), (RenderFragment)(content => content.AddContent(0, "Description")));
        builder.AddComponentReferenceCapture(4, component => Tooltip = (TooltipContainer)component);
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class ScopedOverlayRenderingTests
{
    [Fact]
    public async Task Tooltip_caller_scope_survives_initial_open_close_and_reopen_through_presence()
    {
        var compiled = NssCompiler.Compile("tooltip-scope", "TooltipContainer.custom-tooltip { width: 142px; padding: 0; border: none; }");
        Assert.True(compiled.Success);
        StyleRegistry.RegisterScoped(typeof(ScopedTooltipPage), () => compiled.StyleSheet);
        using var h = new RenderHarness();
        var root = h.Host.Render<ScopedTooltipPage>();
        var page = (ScopedTooltipPage)root.Component;
        Check(false);
        await page.Tooltip.OpenAsync(); Check(true);
        await page.Tooltip.CloseAsync(); Check(false);
        await page.Tooltip.OpenAsync(); Check(true);

        void Check(bool visible)
        {
            h.Host.RunPending(); h.Render(400, 200, 1);
            var surface = root.Single("TooltipContainer");
            Assert.Equal(typeof(ScopedTooltipPage), surface.Scope);
            Assert.Equal("142px", h.Scene.GetStyle(surface).Get(StyleProperties.Width).ToString());
            if (visible) Assert.Equal(142, h.Scene.GetBounds(surface).Width);
        }
    }
}
