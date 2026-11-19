using Nazor.Controls;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the "Layout and surfaces" group.
    private static IEnumerable<GalleryEntry> LayoutEntries() =>
    [
        new("container", "Container", "Layout and surfaces", "Group content; NSS chooses block, flex or grid layout and scrolling.", typeof(Container), typeof(Examples.Layout.ContainerSample)),
        new("separator", "Separator", "Layout and surfaces", "Divide content horizontally or vertically, optionally with a label.", typeof(Separator), typeof(Examples.Layout.SeparatorSample)),
        new("scroll-view", "ScrollView", "Layout and surfaces", "Scroll a bounded document with the pointer or keyboard.", typeof(ScrollView), typeof(Examples.Layout.ScrollViewSample)),
        new("card", "Card", "Layout and surfaces", "Compose a card with a heading, body and footer action.", typeof(Card), typeof(Examples.Layout.CardSample)),
        new("disclosure", "GroupBox, Expander and Accordion", "Layout and surfaces", "Group settings, disclose a section and coordinate multiple panels.", typeof(GroupBox), typeof(Examples.Layout.DisclosureSample), typeof(Expander), typeof(Accordion), typeof(AccordionItem)),
        new("split-view", "SplitView", "Layout and surfaces", "Resize the side pane and toggle its visibility.", typeof(SplitView), typeof(Examples.Layout.SplitViewSample)),
        new("status-bar", "StatusBar", "Layout and surfaces", "Align flexible and fixed sections in a status bar.", typeof(StatusBar), typeof(Examples.Layout.StatusBarSample), typeof(StatusBarSection)),
    ];
}
