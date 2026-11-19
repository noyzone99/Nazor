namespace Nazor.Rendering;

/// <summary>
/// Identifies the elements edited by the shared text editor: a <c>textbox</c>, or a <c>combobox</c>, <c>spinbutton</c> or
/// <c>searchbox</c> that carries a string <c>value</c> attribute, so editable combo boxes and number fields keep their
/// own accessible role.
/// </summary>
public static class TextEditorRoles
{
    /// <summary>Gets whether the element is edited as text.</summary>
    /// <param name="element">The element to inspect.</param>
    /// <returns>true for text editors.</returns>
    public static bool IsEditor(ElementNode element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.Attributes.GetValueOrDefault("role") switch
        {
            "textbox" => true,
            "combobox" or "spinbutton" or "searchbox" => element.Attributes.GetValueOrDefault("value") is string,
            _ => false,
        };
    }
}
