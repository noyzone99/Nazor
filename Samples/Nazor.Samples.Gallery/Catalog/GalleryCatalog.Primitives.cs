using Nazor.Controls;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the "Primitives" group.
    private static IEnumerable<GalleryEntry> PrimitiveEntries() =>
    [
        new("pressable", "Pressable", "Primitives", "Make any content pressable, with repetition and long press.", typeof(global::Nazor.Controls.Primitives.Pressable), typeof(Examples.Primitives.PressableSample)),
        new("commands", "Command and shortcuts", "Primitives", "Share one action between controls and the keyboard.", typeof(global::Nazor.Controls.Primitives.Command), typeof(Examples.Primitives.CommandSample), typeof(global::Nazor.Controls.Primitives.ShortcutScope), typeof(global::Nazor.Controls.Primitives.Shortcut)),
        new("popover-container", "PopoverContainer", "Primitives", "Anchor content to an element with placement, alignment and dismissal.", typeof(global::Nazor.Controls.Primitives.PopoverContainer), typeof(Examples.Primitives.PopoverContainerSample)),
        new("dialog-container", "DialogContainer", "Primitives", "Build any dialog, sheet or drawer with scrim, focus trapping and placement.", typeof(global::Nazor.Controls.Primitives.DialogContainer), typeof(Examples.Primitives.DialogContainerSample)),
        new("tooltip-container", "TooltipContainer", "Primitives", "Describe a control on hover and focus with rich content.", typeof(global::Nazor.Controls.Primitives.TooltipContainer), typeof(Examples.Primitives.TooltipContainerSample)),
        new("presence", "Presence", "Primitives", "Keep content during its exit so themes can animate it.", typeof(global::Nazor.Controls.Primitives.Presence), typeof(Examples.Primitives.PresenceSample)),
        new("collapsible", "Collapsible", "Primitives", "Open and close a region that publishes its natural height.", typeof(global::Nazor.Controls.Primitives.Collapsible), typeof(Examples.Primitives.CollapsibleSample)),
        new("roving-focus", "RovingFocusGroup", "Primitives", "One Tab stop with arrow keys, grids and typing.", typeof(global::Nazor.Controls.Primitives.RovingFocusGroup), typeof(Examples.Primitives.RovingFocusSample), typeof(global::Nazor.Controls.Primitives.RovingFocusItem)),
        new("listbox", "Listbox", "Primitives", "Single and multiple selection lists without a predefined look.", typeof(global::Nazor.Controls.Primitives.Listbox<string>), typeof(Examples.Primitives.ListboxSample), typeof(global::Nazor.Controls.Primitives.ListboxOption<string>)),
        new("combobox", "Combobox", "Primitives", "A text field with suggestions while the focus stays in the text.", typeof(global::Nazor.Controls.Primitives.Combobox<string>), typeof(Examples.Primitives.ComboboxSample)),
        new("virtualizer", "Virtualizer", "Primitives", "Lists and grids of any length that create only the visible items.", typeof(global::Nazor.Controls.Primitives.Virtualizer<int>), typeof(Examples.Primitives.VirtualizerSample)),
        new("overflow-container", "OverflowContainer", "Primitives", "Move the items that do not fit into an overflow control.", typeof(global::Nazor.Controls.Primitives.OverflowContainer<string>), typeof(Examples.Primitives.OverflowContainerSample)),
        new("draggable", "Draggable", "Primitives", "Report pointer drags with a threshold and an optional axis.", typeof(global::Nazor.Controls.Primitives.Draggable), typeof(Examples.Primitives.DraggableSample)),
        new("resize-handle", "ResizeHandle", "Primitives", "Resize panes, columns and dialogs with the pointer or the keyboard.", typeof(global::Nazor.Controls.Primitives.ResizeHandle), typeof(Examples.Primitives.ResizeHandleSample)),
        new("indicator", "Indicator", "Primitives", "A mark that follows the selected or hovered item of a group.", typeof(global::Nazor.Controls.Primitives.Indicator), typeof(Examples.Primitives.IndicatorSample)),
        new("number-presenter", "NumberPresenter", "Primitives", "Present a target and its displayed scalar with linear, exponential or immediate motion.", typeof(global::Nazor.Controls.Primitives.NumberPresenter), typeof(Examples.Primitives.NumberPresenterSample)),
        new("size-observer", "SizeObserver", "Primitives", "Report the size of content after layout.", typeof(global::Nazor.Controls.Primitives.SizeObserver), typeof(Examples.Primitives.SizeObserverSample)),
        new("accessibility", "VisuallyHidden and LiveRegion", "Primitives", "Text for screen readers only and announced status lines.", typeof(global::Nazor.Controls.Primitives.LiveRegion), typeof(Examples.Primitives.AccessibilitySample), typeof(global::Nazor.Controls.Primitives.VisuallyHidden)),
        new("models", "Selection, calendar and tree models", "Primitives", "The state behind lists, calendars and trees, usable on its own.", typeof(global::Nazor.Controls.Primitives.SelectionModel<string>), typeof(Examples.Primitives.ModelsSample)),
        new("editable-text", "EditableText", "Primitives", "Compose the text editor primitive with a custom surface.", typeof(global::Nazor.Controls.Primitives.EditableText), typeof(Examples.Primitives.EditableTextSample)),
        new("color-controls", "ColorArea and ColorSlider", "Primitives", "Compose a color picker from a saturation and brightness surface and a hue slider.", typeof(global::Nazor.Controls.Primitives.ColorArea), typeof(Examples.Primitives.ColorControlsSample), typeof(global::Nazor.Controls.Primitives.ColorSlider)),
        new("loading-boundary", "LoadingBoundary", "Primitives", "Switch content between ready, loading and failure states.", typeof(global::Nazor.Controls.Primitives.LoadingBoundary), typeof(Examples.Primitives.LoadingBoundarySample)),
    ];
}
