using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>Declares application title-bar content in a window. The content mounts only when the selected configuration requests User.</summary>
/// <remarks>One declaration is permitted per window. Native and Nazor selections ignore it without instantiating its children. Keep persistent state outside the caption subtree.</remarks>
public sealed class UserTitleBar : TitleBar
{
    /// <summary>Marks the rendered TitleBar element as user-provided.</summary>
    protected override string Source => "user";
    /// <summary>Registers the surface with its declaring window instead of rendering it in place.</summary>
    /// <param name="builder">The render destination.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder) => SetWindowTitleBar(base.BuildRenderTree);
    /// <summary>Unregisters the declaration and unsubscribes from the owning window.</summary>
    public override void Dispose()
    {
        SetWindowTitleBar(null);
        base.Dispose();
    }
}
