using Nazor.Rendering;

namespace Nazor.Style;

/// <summary>Matches an explicit control or editor state using retained role and field metadata.</summary>
public sealed record ControlStateSelector : PseudoSelector
{
    /// <summary>Creates a control-state pseudo-class.</summary>
    /// <param name="name">Enabled, indeterminate, read-only, read-write, placeholder-shown, valid, invalid, user-valid, user-invalid, required or optional, ignoring case.</param>
    /// <exception cref="ArgumentException">The name is not a supported control-state pseudo-class.</exception>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    public ControlStateSelector(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name.ToLowerInvariant();
        if (!Names.Contains(Name, StringComparer.Ordinal)) throw new ArgumentException("Unknown control-state pseudo-class.", nameof(name));
    }

    /// <summary>Gets the normalized pseudo-class name without its colon.</summary>
    public string Name { get; }

    internal static readonly string[] Names = ["enabled", "indeterminate", "read-only", "read-write", "placeholder-shown", "valid", "invalid", "user-valid", "user-invalid", "required", "optional"];

    internal ElementStates Dependencies => Name switch
    {
        "enabled" or "read-only" or "read-write" => ElementStates.Disabled,
        "placeholder-shown" => ElementStates.Composing,
        "user-valid" or "user-invalid" => ElementStates.ValidationEligible,
        _ => ElementStates.None,
    };

    internal IEnumerable<string> AttributeDependencies => Name switch
    {
        "enabled" => ["role", "disabled"],
        "indeterminate" => ["role", "indeterminate"],
        "read-only" or "read-write" => ["role", "value", "disabled", "readonly"],
        "placeholder-shown" => ["role", "value", "placeholder-text"],
        _ => ["role", "validation-control", "field", "invalid", "required"],
    };

    /// <summary>Tests the element's role, effective disabled state and application-owned field metadata.</summary>
    /// <param name="element">The retained element to inspect.</param>
    /// <returns>Whether the requested state applies and is active.</returns>
    public override bool Matches(ElementNode element)
    {
        var attributes = element.Attributes;
        var role = attributes.GetValueOrDefault("role") as string;
        var editor = TextEditorRoles.IsEditor(element);
        var writable = editor && !element.IsDisabled && attributes.GetValueOrDefault("readonly") is not true;
        return Name switch
        {
            "enabled" => SupportsDisabled(element, role) && !element.IsDisabled,
            "indeterminate" => role is "checkbox" or "radio" or "switch" or "togglebutton" or "menuitemcheckbox" or "menuitemradio" or "treeitem" or "progressbar" &&
                attributes.GetValueOrDefault("indeterminate") is true,
            "read-only" => !writable,
            "read-write" => writable,
            "placeholder-shown" => editor && attributes.GetValueOrDefault("placeholder-text") is string { Length: > 0 } &&
                attributes.GetValueOrDefault("value") is not string { Length: > 0 } && (element.States & ElementStates.Composing) == 0,
            "required" => ParticipatesInValidation(element, role) && FieldFlag(element, "required"),
            "optional" => ParticipatesInValidation(element, role) && !FieldFlag(element, "required"),
            "valid" => ParticipatesInValidation(element, role) && !FieldFlag(element, "invalid"),
            "invalid" => ParticipatesInValidation(element, role) && FieldFlag(element, "invalid"),
            "user-valid" => ParticipatesInValidation(element, role) && ValidationFeedback.IsEligible(element) && !FieldFlag(element, "invalid"),
            "user-invalid" => ParticipatesInValidation(element, role) && ValidationFeedback.IsEligible(element) && FieldFlag(element, "invalid"),
            _ => false,
        };
    }

    internal static bool SupportsDisabled(ElementNode element, string? role) =>
        role is "button" or "link" or "splitbutton" or "togglebutton" or "checkbox" or "radio" or "radiogroup" or "switch" or
            "textbox" or "searchbox" or "combobox" or "spinbutton" or "slider" or "listbox" or "option" or "tree" or "treeitem" or
            "tab" or "menuitem" or "menuitemcheckbox" or "menuitemradio" || element.Attributes.GetValueOrDefault("disabled") is bool;

    internal static bool ParticipatesInValidation(ElementNode element, string? role) =>
        role is "textbox" or "searchbox" or "checkbox" or "radio" or "radiogroup" or "switch" or "combobox" or "spinbutton" or
            "slider" or "listbox" or "tree" or "togglebutton" || element.Attributes.GetValueOrDefault("field") is true ||
        element.Attributes.GetValueOrDefault("validation-control") is true;

    private static bool FieldFlag(ElementNode element, string name)
    {
        if (element.Attributes.GetValueOrDefault(name) is true) return true;
        for (var parent = element.ParentElement; parent is not null; parent = parent.ParentElement)
            if (parent.Attributes.GetValueOrDefault("field") is true) return parent.Attributes.GetValueOrDefault(name) is true;
        return false;
    }

    /// <summary>Returns the NSS spelling of the pseudo-class.</summary>
    /// <returns>The normalized name prefixed with a colon.</returns>
    public override string ToString() => ":" + Name;
}
