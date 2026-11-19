using Nazor.Accessibility;
using Tmds.DBus.Protocol;

namespace Nazor.Platform.Linux;

internal sealed partial class AccessibilityBridge
{
    private partial class AccessibleObject
    {
        private SemanticNode? Table => tree is not null && Node is not null ? tree.Tree.ContainingTable(Node) : null;
        private IReadOnlyList<SemanticNode> Cells => Table is { } table ? tree!.Tree.TableCells(table) : [];
        private SemanticNode? Cell(int row, int column) => Table is { } table ? tree!.Tree.TableCellAt(table, row, column) : null;
        private (string,ObjectPath) CellReference(SemanticNode? cell) => tree is null ? NullReference : owner.Reference(owner.Find(tree,cell));
        private SemanticNode? Header(int position, bool columns) => Table is { } table ? tree!.Tree.TableHeaders(table,columns).FirstOrDefault(header =>
            columns ? position >= header.ColumnIndex && position < header.ColumnIndex + header.ColumnSpan : position >= header.RowIndex && position < header.RowIndex + header.RowSpan) : null;
        private Dictionary<string,object> TableProperties() => new()
        {
            ["version"]=1u, ["NRows"]=Node?.RowCount ?? 0, ["NColumns"]=Node?.ColumnCount ?? 0,
            ["Caption"]=CellReference(Node?.Children.FirstOrDefault(child=>child.Element?.Name=="TableCaption")),
            ["Summary"]=NullReference, ["NSelectedRows"]=0, ["NSelectedColumns"]=0,
        };
        private Dictionary<string,object> CellProperties() => new()
        {
            ["version"]=1u, ["Position"]=(Node?.RowIndex ?? -1,Node?.ColumnIndex ?? -1),
            ["RowSpan"]=Node?.RowSpan ?? 1, ["ColumnSpan"]=Node?.ColumnSpan ?? 1, ["Table"]=CellReference(Table),
        };
        Task<object> IAccessibleTable.GetAsync(string property) => OnUi(()=>TableProperties()[property]);
        Task<IDictionary<string,object>> IAccessibleTable.GetAllAsync() => OnUi<IDictionary<string,object>>(TableProperties);
        Task<object> IAccessibleTableCell.GetAsync(string property) => OnUi(()=>CellProperties()[property]);
        Task<IDictionary<string,object>> IAccessibleTableCell.GetAllAsync() => OnUi<IDictionary<string,object>>(CellProperties);
        public Task<(string,ObjectPath)> GetAccessibleAtAsync(int row,int column) => OnUi(()=>CellReference(Cell(row,column)));
        public Task<int> GetIndexAtAsync(int row,int column) => OnUi(()=>Cell(row,column) is { } cell ? Cells.ToList().IndexOf(cell) : -1);
        public Task<int> GetRowAtIndexAsync(int index) => OnUi(()=>Cells.ElementAtOrDefault(index)?.RowIndex ?? -1);
        public Task<int> GetColumnAtIndexAsync(int index) => OnUi(()=>Cells.ElementAtOrDefault(index)?.ColumnIndex ?? -1);
        public Task<string> GetRowDescriptionAsync(int row) => OnUi(()=>Header(row,false)?.Name ?? "");
        public Task<string> GetColumnDescriptionAsync(int column) => OnUi(()=>Header(column,true)?.Name ?? "");
        public Task<int> GetRowExtentAtAsync(int row,int column) => OnUi(()=>Cell(row,column)?.RowSpan ?? 0);
        public Task<int> GetColumnExtentAtAsync(int row,int column) => OnUi(()=>Cell(row,column)?.ColumnSpan ?? 0);
        public Task<(string,ObjectPath)> GetRowHeaderAsync(int row) => OnUi(()=>CellReference(Header(row,false)));
        public Task<(string,ObjectPath)> GetColumnHeaderAsync(int column) => OnUi(()=>CellReference(Header(column,true)));
        public Task<int[]> GetSelectedRowsAsync() => Task.FromResult(Array.Empty<int>());
        public Task<int[]> GetSelectedColumnsAsync() => Task.FromResult(Array.Empty<int>());
        public Task<bool> IsRowSelectedAsync(int row) => Task.FromResult(false);
        public Task<bool> IsColumnSelectedAsync(int column) => Task.FromResult(false);
        public Task<bool> IsSelectedAsync(int row,int column) => Task.FromResult(false);
        public Task<bool> AddRowSelectionAsync(int row) => Task.FromResult(false);
        public Task<bool> AddColumnSelectionAsync(int column) => Task.FromResult(false);
        public Task<bool> RemoveRowSelectionAsync(int row) => Task.FromResult(false);
        public Task<bool> RemoveColumnSelectionAsync(int column) => Task.FromResult(false);
        public Task<(bool,int,int,int,int,bool)> GetRowColumnExtentsAtIndexAsync(int index) => OnUi(()=>
            Cells.ElementAtOrDefault(index) is { } cell ? (true,cell.RowIndex ?? -1,cell.ColumnIndex ?? -1,cell.RowSpan,cell.ColumnSpan,false) : (false,-1,-1,0,0,false));
        public Task<(bool,int,int,int,int)> GetRowColumnSpanAsync() => OnUi(()=>(Node?.RowIndex is not null,Node?.RowIndex ?? -1,Node?.ColumnIndex ?? -1,Node?.RowSpan ?? 1,Node?.ColumnSpan ?? 1));
        public Task<(string,ObjectPath)[]> GetColumnHeaderCellsAsync() => HeaderReferences(true);
        public Task<(string,ObjectPath)[]> GetRowHeaderCellsAsync() => HeaderReferences(false);
        private Task<(string,ObjectPath)[]> HeaderReferences(bool columns) => OnUi(()=>tree is not null && Node is not null ? tree.Tree.TableHeaders(Node,columns).Select(CellReference).ToArray() : []);
    }
}
