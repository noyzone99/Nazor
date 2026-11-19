using Nazor.Controls;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the "Collections" group.
    private static IEnumerable<GalleryEntry> CollectionEntries() =>
    [
        new("list-view", "ListView", "Collections", "Select an item in a scrolling list.", typeof(ListView<string>), typeof(Examples.Collections.ListViewSample)),
        new("table", "Table", "Collections", "Compose a semantic table with a caption, headers, a body, totals and merged cells.", typeof(Table), typeof(Examples.Collections.TableSample), typeof(TableHeader), typeof(TableBody), typeof(TableFooter), typeof(TableRow), typeof(TableCell), typeof(TableHeaderCell)),
        new("tree-view", "TreeView", "Collections", "Expand a hierarchy of folders and select a file.", typeof(TreeView<Examples.Collections.TreeViewSample.FileNode>), typeof(Examples.Collections.TreeViewSample)),
        new("grid-view", "GridView", "Collections", "Select tiles in a virtualized grid.", typeof(GridView<int>), typeof(Examples.Collections.GridViewSample)),
        new("flip-view", "FlipView", "Collections", "Browse packaged images or custom interactive templates one item at a time.", typeof(FlipView<Examples.Collections.FlipViewSample.FeatureCard>), typeof(Examples.Collections.FlipViewSample)),
        new("action-row", "ActionRow and RowGroup", "Collections", "Present settings as a group of rows with optional trailing controls.", typeof(RowGroup), typeof(Examples.Collections.ActionRowSample), typeof(ActionRow)),
        new("pagination", "Pagination", "Collections", "Move through pages and change the page size.", typeof(Pagination), typeof(Examples.Collections.PaginationSample)),
    ];
}
