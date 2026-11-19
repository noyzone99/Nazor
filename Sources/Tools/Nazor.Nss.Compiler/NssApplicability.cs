using Nazor.Style;

namespace Nazor.Nss;

/// <summary>Conservative authoring checks for selectors with a statically known subject.</summary>
public static class NssApplicability
{
    // Custom components can emit arbitrary elements; their rendering cannot be inferred from their name.
    private static readonly HashSet<string> Controls = new(StringComparer.Ordinal)
    {
        "Window", "Container", "TextBlock", "Button", "CheckBox", "Image", "Icon", "Popover", "Dialog", "DialogTitle", "DialogDescription", "DialogClose", "AlertDialog", "AlertDialogAction", "AlertDialogCancel", "ContextMenu", "Submenu", "SubmenuTrigger", "MenuItem", "MenuCheckBoxItem", "MenuRadioItem", "TextBox", "Tooltip",
        "ComboBox", "RadioGroup", "RadioButton", "ToggleSwitch", "Slider", "CalendarView", "DatePicker", "Anchor", "SplitButton",
        "DropDownButton", "ToolBar", "MenuBar", "MenuBarItem", "InfoBar", "ListView", "Frame", "Progress", "Meter", "Toggle", "ToggleGroup", "Accordion", "AccordionItem", "Table", "TableHeader", "TableBody", "TableRow", "TableCell", "TableHeaderCell", "Heading", "List", "ListItem", "Label", "Field", "Fieldset", "Legend", "Form", "DefinitionList", "DefinitionTerm", "DefinitionDescription", "Separator",
        "Expander", "TabView", "SplitView", "Splitter", "PasswordBox", "NumberBox", "TitleBar", "SnapLayoutArea", "TreeView",
        "CheckMark", "ExpanderChevron", "SplitButtonChevron", "DropDownButtonChevron", "TreeViewExpander", "CalendarPrevious", "CalendarNext",
    };

    public static bool AppliesTo(string propertyName, IReadOnlyList<Selector> selectors, Func<string, string?>? idType = null) =>
        selectors.Count == 0 || selectors.Any(selector => AppliesTo(propertyName, selector, idType));

    private static bool AppliesTo(string name, Selector selector, Func<string, string?>? idType)
    {
        var target = selector.Subject.PseudoElement;
        if (StyleProperties.TryGetShorthand(name, out var shorthand)) return shorthand.Longhands.Any(p => AppliesTo(p.Name, selector, idType));
        if (StyleProperties.TryGet(name, out var property))
        {
            if (target != PseudoElement.None) return StyleEngine.AppliesToPseudo(property, target);
            if (property == StyleProperties.Content) return false;
        }
        return AppliesTo(name, SubjectType(selector, idType));
    }

    private static string? SubjectType(Selector selector, Func<string, string?>? idType)
    {
        var subject = selector.Subject;
        if (subject.Type is { } type) return type;
        if (subject.Pseudos.Any(p => p is RootSelector)) return "Window";
        // An ID beyond a component anchor or ::deep may belong to another component's private scope.
        return subject.Id is { } id && !selector.HasDeep && selector.Compounds.Take(selector.Compounds.Count - 1)
            .All(c => c.Type is null || Controls.Contains(c.Type)) ? idType?.Invoke(id) : null;
    }

    private static bool AppliesTo(string name, string? type)
    {
        if (type is null) return true;
        if (type.StartsWith("Nazor.Controls.", StringComparison.Ordinal)) type = type["Nazor.Controls.".Length..];
        type = type switch
        {
            "Input" or "TextArea" => "TextBox",
            "NumberField" => "NumberBox",
            "Select" => "ComboBox",
            "MenuContent" => "MenuFlyout",
            _ => type,
        };
        if (StyleProperties.TryGetShorthand(name, out var shorthand)) return shorthand.Longhands.Any(p => AppliesTo(p.Name, type));
        if (!StyleProperties.TryGet(name, out var property)) return true;
        if (!StyleProperties.AppliesTo(property, type, type == "Window")) return false;
        if (!Controls.Contains(type)) return true;
        return property.Name switch
        {
            "object-fit" or "object-position" => type is "Image" or "Icon",
            "stroke-width" or "stroke-trim-start" or "stroke-trim-end" => type is "Progress" or "CheckMark" or "ExpanderChevron" or "SplitButtonChevron" or "DropDownButtonChevron" or "TreeViewExpander" or "CalendarPrevious" or "CalendarNext",
            "stroke-radius" => type == "Progress",
            _ => true,
        };
    }
}
