using Nazor.Graphics;
using Nazor.Style;

namespace Nazor.Layout;

public static partial class LayoutEngine
{
    private static List<LayoutNode> AddTableItems(LayoutNode table, List<GridItem> items)
    {
        var rows = new List<LayoutNode>();
        var occupied = new HashSet<(int Row, int Column)>();
        var caption = table.Children.FirstOrDefault(node => node.Style.TablePart == "caption" && node.Style.Display != Display.None);
        var offset = caption is null ? 0 : 1;
        var columns = 0;
        foreach (var group in table.Children.Where(node => node.Style.TablePart == "group"))
        {
            if (group.Style.Display == Display.None) { Hide(group); continue; }
            var children = group.Children.Where(node => node.Style.TablePart == "row" && node.Style.Display != Display.None).ToArray();
            foreach (var hidden in group.Children.Where(node => node.Style.Display == Display.None)) Hide(hidden);
            for (var index = 0; index < children.Length; index++)
            {
                var row = children[index];
                var rowIndex = rows.Count;
                rows.Add(row);
                row.TableRow = rowIndex;
                var column = 0;
                foreach (var cell in row.Children.Where(node => node.Style.TablePart == "cell"))
                {
                    if (cell.Style.Display == Display.None) { Hide(cell); continue; }
                    var span = Math.Clamp(cell.Style.TableColumnSpan, 1, 1000);
                    var rowSpan = Math.Clamp(cell.Style.TableRowSpan, 1, children.Length - index);
                    while (Enumerable.Range(column, span).Any(c => occupied.Contains((rowIndex, c)))) column++;
                    for (var r = rowIndex; r < rowIndex + rowSpan; r++)
                        for (var c = column; c < column + span; c++) occupied.Add((r, c));
                    cell.TableRow = rowIndex; cell.TableColumn = column; cell.TableRowSpan = rowSpan;
                    items.Add(TableItem(cell, rowIndex + offset, column, rowSpan, span));
                    column += span;
                    columns = Math.Max(columns, column);
                }
            }
        }
        table.TableRows = rows.Count; table.TableColumns = columns;
        if (caption is not null) items.Insert(0, TableItem(caption, 0, 0, 1, Math.Max(1, columns)));
        return rows;
    }

    private static GridItem TableItem(LayoutNode node, int row, int column, int rowSpan, int columnSpan) => new()
    {
        Node = node, Box = ChildBox(node, null, null, AvailableSpace.MaxContent),
        Row = row, Column = column, RowSpan = rowSpan, ColumnSpan = columnSpan,
        Justify = AlignSelf.Stretch, Align = AlignSelf.Stretch,
    };

    private static void PositionTableParts(LayoutNode table, List<GridItem> items, List<LayoutNode> tableRows,
        List<GridTrack> tracks, float width, Thickness edges)
    {
        var offset = items.Any(item => item.Node.Style.TablePart == "caption") ? 1 : 0;
        foreach (var group in table.Children.Where(node => node.Style.TablePart == "group" && node.Style.Display != Display.None))
        {
            var groupRows = tableRows.Where(row => row.Parent == group).ToArray();
            if (groupRows.Length == 0) { group.Frame = Rect.Empty; continue; }
            var first = tracks[groupRows[0].TableRow!.Value + offset];
            var last = tracks[groupRows[^1].TableRow!.Value + offset];
            group.Frame = new Rect(edges.Left, edges.Top + first.Start, Math.Max(0, width - edges.Horizontal), last.Start + last.Base - first.Start);
            group.Padding = group.Border = group.Margin = Thickness.Zero;
            foreach (var row in groupRows)
            {
                var track = tracks[row.TableRow!.Value + offset];
                row.Frame = new Rect(0, track.Start - first.Start, group.Frame.Width, track.Base);
                row.Padding = row.Border = row.Margin = Thickness.Zero;
                foreach (var item in items.Where(item => item.Node.Parent == row))
                    item.Node.Frame = item.Node.Frame.Offset(-group.Frame.X - row.Frame.X, -group.Frame.Y - row.Frame.Y);
            }
        }
    }

    private static void Hide(LayoutNode node)
    {
        node.Frame = Rect.Empty;
        node.TableRow = node.TableColumn = null;
        foreach (var child in node.Children) Hide(child);
    }
}
