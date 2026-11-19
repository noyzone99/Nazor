using Nazor.Controls;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the "Menus and overlays" group.
    private static IEnumerable<GalleryEntry> OverlayEntries() =>
    [
        new("menu", "Menu", "Menus and overlays", "Commands, check and radio items, sections, shortcuts and submenus.", typeof(Menu), typeof(Examples.Overlays.MenuSample), typeof(MenuItem), typeof(MenuCheckItem), typeof(MenuRadioGroup<string>), typeof(MenuRadioItem<string>), typeof(MenuSeparator), typeof(MenuSection), typeof(Submenu)),
        new("context-menu", "ContextMenu", "Menus and overlays", "Open a menu where the user right-clicks or presses the menu key.", typeof(ContextMenu), typeof(Examples.Overlays.ContextMenuSample)),
        new("menu-bar", "MenuBar", "Menus and overlays", "The menu bar of a window, with keyboard and hover switching between its menus.", typeof(MenuBar), typeof(Examples.Overlays.MenuBarSample), typeof(MenuBarItem)),
        new("popover", "Popover", "Menus and overlays", "Interactive content anchored to a trigger, or a hover card that opens on hover and focus.", typeof(Popover), typeof(Examples.Overlays.PopoverSample)),
        new("tooltip", "Tooltip", "Menus and overlays", "A short description of a control shown on hover and focus.", typeof(Tooltip), typeof(Examples.Overlays.TooltipSample)),
        new("dialog", "Dialog", "Menus and overlays", "A window-local dialog with a title, a body and actions ordered by role.", typeof(Dialog), typeof(Examples.Overlays.DialogSample), typeof(DialogButton)),
        new("alert-dialog", "AlertDialog", "Menus and overlays", "A modal decision with severity, details and a don't-ask-again option.", typeof(AlertDialog), typeof(Examples.Overlays.AlertDialogSample)),
        new("drawer", "Drawer", "Menus and overlays", "A side panel or bottom sheet that slides over the window.", typeof(Drawer), typeof(Examples.Overlays.DrawerSample)),
        new("command-palette", "CommandPalette", "Menus and overlays", "Search and run every command of the application from the keyboard.", typeof(CommandPalette), typeof(Examples.Overlays.CommandPaletteSample), typeof(CommandPaletteGroup)),
        new("teaching-tip", "TeachingTip", "Menus and overlays", "A non-modal callout that points at a control to introduce a feature.", typeof(TeachingTip), typeof(Examples.Overlays.TeachingTipSample)),
    ];
}
