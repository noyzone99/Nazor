using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>Declares application caption-button groups independently of the title-bar provider.</summary>
/// <remarks>Declare once in the window file. The fragments mount only for WindowCaptionButtons.User. The application supplies commands, accessible names and optional Snap areas; no automatic buttons are added.</remarks>
public sealed class UserCaptionButtons : ComponentBase, IDisposable
{
    /// <summary>Controls on the physical left, irrespective of text direction.</summary>
    [Parameter] public RenderFragment? Left { get; set; }
    /// <summary>Controls on the physical right, irrespective of text direction.</summary>
    [Parameter] public RenderFragment? Right { get; set; }
    /// <summary>Shorthand for Right; cannot be supplied together with Right.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Validates the right-hand fragment shorthand.</summary>
    protected override void OnParametersSet()
    {
        if (Right is not null && ChildContent is not null) throw new ArgumentException("Use either Right or ChildContent for UserCaptionButtons.");
    }
    /// <summary>Registers the fragments without instantiating their content.</summary>
    /// <param name="builder">The render destination.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder) => SetWindowCaptionButtons(Left, Right ?? ChildContent);
    /// <summary>Removes the declaration from its window.</summary>
    public void Dispose() => SetWindowCaptionButtons(null, null);
}
