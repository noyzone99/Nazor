using Nazor.Controls;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the "Actions" group.
    private static IEnumerable<GalleryEntry> ActionEntries() =>
    [
        new("button", "Button", "Actions", "Run an action, with appearances, intents, sizes, shapes, busy state, repetition and commands.", typeof(Button), typeof(Examples.Actions.ButtonSample)),
        new("toggle-button", "ToggleButton", "Actions", "Keep a pressed state, such as Bold in an editor.", typeof(ToggleButton), typeof(Examples.Actions.ToggleButtonSample)),
        new("button-group", "ButtonGroup", "Actions", "Group related buttons and toggle buttons.", typeof(ButtonGroup), typeof(Examples.Actions.ButtonGroupSample)),
        new("menu-buttons", "MenuButton and SplitButton", "Actions", "Open a menu or run a separate primary action.", typeof(MenuButton), typeof(Examples.Actions.MenuButtonsSample), typeof(SplitButton)),
        new("toolbar", "Toolbar", "Actions", "Group commands, separate sections and push actions to the end.", typeof(Toolbar), typeof(Examples.Actions.ToolbarSample), typeof(ToolbarGroup), typeof(ToolbarSeparator), typeof(ToolbarSpacer)),
        new("action-bar", "ActionBar and FloatingActionButton", "Actions", "Place actions in a bar or float a primary action over content.", typeof(ActionBar), typeof(Examples.Actions.ActionBarSample), typeof(FloatingActionButton)),
        new("link", "Link", "Actions", "Run a navigation callback with link semantics.", typeof(Link), typeof(Examples.Actions.LinkSample)),
    ];
}
