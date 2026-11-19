using Nazor.Controls.Primitives;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// Shows a keyboard shortcut in the notation of the current platform: "Ctrl+Shift+S" on Windows and Linux, "⇧⌘S" on
/// macOS. Each key is a <c>ShortcutLabelKey</c> part, joined by <c>ShortcutLabelSeparator</c> parts where the platform
/// writes a plus sign. Assistive technologies read the whole shortcut as one text.
/// </summary>
public sealed class ShortcutLabel : Control
{
    /// <summary>Gets or sets the shortcut to show.</summary>
    [Parameter, EditorRequired] public KeyGesture Gesture { get; set; }

    /// <summary>Gets the NSS element name emitted by ShortcutLabel.</summary>
    protected override string ElementName => "ShortcutLabel";

    /// <summary>Validates the gesture.</summary>
    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(Gesture.Key)) throw new InvalidOperationException("ShortcutLabel requires a Gesture.");
    }

    /// <summary>Adds the text role and the accessible text of the shortcut.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "text");
        if (AccessibleName is null) builder.AddAttribute(11, "accessible-name", Gesture.ToDisplayString());
    }

    /// <summary>Adds one part per key.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var parts = Gesture.DisplayParts();
        var mac = OperatingSystem.IsMacOS();
        for (var i = 0; i < parts.Count; i++)
        {
            if (i > 0 && !mac)
            {
                builder.OpenElement(1000, "ShortcutLabelSeparator");
                builder.AddContent(1001, "+");
                builder.CloseElement();
            }
            builder.OpenElement(1002, "ShortcutLabelKey");
            builder.AddContent(1003, parts[i]);
            builder.CloseElement();
        }
    }
}
