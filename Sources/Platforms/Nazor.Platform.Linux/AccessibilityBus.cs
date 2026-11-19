using System.Runtime.InteropServices;
using Tmds.DBus.Protocol;

namespace Nazor.Platform.Linux;

[DBusInterface("org.a11y.Bus")]
internal interface IAccessibilityBus : IDBusObject { Task<string> GetAddressAsync(); }

[DBusInterface("org.a11y.atspi.Socket")]
internal interface IAccessibilitySocket : IDBusObject
{
    [return: Argument] Task<(string, ObjectPath)> EmbedAsync((string, ObjectPath) application);
    Task UnembedAsync((string, ObjectPath) application);
}

[DBusInterface("org.a11y.atspi.Accessible")]
internal interface IAccessible : IDBusObject
{
    Task<object> GetAsync(string property);
    Task<IDictionary<string, object>> GetAllAsync();
    [return: Argument] Task<(string, ObjectPath)> GetChildAtIndexAsync(int index);
    Task<(string, ObjectPath)[]> GetChildrenAsync();
    Task<int> GetIndexInParentAsync();
    Task<(uint, (string, ObjectPath)[])[]> GetRelationSetAsync();
    Task<uint> GetRoleAsync();
    Task<string> GetRoleNameAsync();
    Task<string> GetLocalizedRoleNameAsync();
    Task<uint[]> GetStateAsync();
    Task<IDictionary<string,string>> GetAttributesAsync();
    [return: Argument] Task<(string, ObjectPath)> GetApplicationAsync();
    Task<string[]> GetInterfacesAsync();
}

[DBusInterface("org.a11y.atspi.Application")]
internal interface IAccessibleApplication : IDBusObject
{
    Task<object> GetAsync(string property);
    Task<IDictionary<string,object>> GetAllAsync();
    Task SetAsync(string property,object value);
    Task<string> GetLocaleAsync(uint kind);
    Task<string> GetApplicationBusAddressAsync();
}

[DBusInterface("org.a11y.atspi.Action")]
internal interface IAccessibleAction : IDBusObject
{
    Task<object> GetAsync(string property);
    Task<IDictionary<string,object>> GetAllAsync();
    Task<string> GetDescriptionAsync(int index);
    Task<string> GetNameAsync(int index);
    Task<string> GetLocalizedNameAsync(int index);
    Task<string> GetKeyBindingAsync(int index);
    Task<(string,string,string)[]> GetActionsAsync();
    Task<bool> DoActionAsync(int index);
}

[DBusInterface("org.a11y.atspi.Component")]
internal interface IAccessibleComponent : IDBusObject
{
    Task<bool> ContainsAsync(int x,int y,uint coordinates);
    [return: Argument] Task<(string,ObjectPath)> GetAccessibleAtPointAsync(int x,int y,uint coordinates);
    [return: Argument] Task<(int,int,int,int)> GetExtentsAsync(uint coordinates);
    Task<(int,int)> GetPositionAsync(uint coordinates);
    Task<(int,int)> GetSizeAsync();
    Task<uint> GetLayerAsync();
    Task<short> GetMDIZOrderAsync();
    Task<bool> GrabFocusAsync();
    Task<double> GetAlphaAsync();
    Task<bool> SetExtentsAsync(int x,int y,int width,int height,uint coordinates);
    Task<bool> SetPositionAsync(int x,int y,uint coordinates);
    Task<bool> SetSizeAsync(int width,int height);
    Task<bool> ScrollToAsync(uint type);
    Task<bool> ScrollToPointAsync(uint coordinates,int x,int y);
}

[DBusInterface("org.a11y.atspi.Selection")]
internal interface IAccessibleSelection : IDBusObject
{
    Task<object> GetAsync(string property);
    Task<IDictionary<string,object>> GetAllAsync();
    [return: Argument] Task<(string,ObjectPath)> GetSelectedChildAsync(int index);
    Task<bool> SelectChildAsync(int index);
    Task<bool> DeselectSelectedChildAsync(int index);
    Task<bool> IsChildSelectedAsync(int index);
    Task<bool> SelectAllAsync();
    Task<bool> ClearSelectionAsync();
    Task<bool> DeselectChildAsync(int index);
}

[DBusInterface("org.a11y.atspi.Value")]
internal interface IAccessibleValue : IDBusObject
{
    Task<object> GetAsync(string property);
    Task<IDictionary<string,object>> GetAllAsync();
    Task SetAsync(string property,object value);
}

[DBusInterface("org.a11y.atspi.Text")]
internal interface IAccessibleText : IDBusObject
{
    Task<object> GetAsync(string property);
    Task<IDictionary<string,object>> GetAllAsync();
    Task<string> GetTextAsync(int start,int end);
    Task<(string,int,int)> GetStringAtOffsetAsync(int offset,uint granularity);
    Task<(string,int,int)> GetTextBeforeOffsetAsync(int offset,uint boundary);
    Task<(string,int,int)> GetTextAtOffsetAsync(int offset,uint boundary);
    Task<(string,int,int)> GetTextAfterOffsetAsync(int offset,uint boundary);
    Task<int> GetCharacterAtOffsetAsync(int offset);
    Task<bool> SetCaretOffsetAsync(int offset);
    Task<int> GetNSelectionsAsync();
    Task<(int,int)> GetSelectionAsync(int index);
    Task<bool> AddSelectionAsync(int start,int end);
    Task<bool> RemoveSelectionAsync(int index);
    Task<bool> SetSelectionAsync(int index,int start,int end);
    Task<(int,int,int,int)> GetCharacterExtentsAsync(int offset,uint coordinates);
    Task<(int,int,int,int)> GetRangeExtentsAsync(int start,int end,uint coordinates);
    Task<int> GetOffsetAtPointAsync(int x,int y,uint coordinates);
    Task<string> GetAttributeValueAsync(int offset,string attribute);
    Task<(IDictionary<string,string>,int,int)> GetAttributesAsync(int offset);
    Task<IDictionary<string,string>> GetDefaultAttributesAsync();
    Task<IDictionary<string,string>> GetDefaultAttributeSetAsync();
    Task<(IDictionary<string,string>,int,int)> GetAttributeRunAsync(int offset,bool defaults);
    Task<(int,int,string,object)[]> GetBoundedRangesAsync(int x,int y,int width,int height,uint coordinates,uint xClip,uint yClip);
    Task<bool> ScrollSubstringToAsync(int start,int end,uint type);
    Task<bool> ScrollSubstringToPointAsync(int start,int end,uint coordinates,int x,int y);
}

[DBusInterface("org.a11y.atspi.EditableText")]
internal interface IAccessibleEditableText : IDBusObject
{
    Task<bool> SetTextContentsAsync(string text);
    Task<bool> InsertTextAsync(int position,string text,int length);
    Task CopyTextAsync(int start,int end);
    Task<bool> CutTextAsync(int start,int end);
    Task<bool> DeleteTextAsync(int start,int end);
    Task<bool> PasteTextAsync(int position);
}

[DBusInterface("org.a11y.atspi.Event.Object")]
internal interface IAccessibleEvents : IDBusObject
{
    Task<IDisposable> WatchPropertyChangeAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler);
    Task<IDisposable> WatchStateChangedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler);
    Task<IDisposable> WatchChildrenChangedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler);
    Task<IDisposable> WatchTextChangedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler);
    Task<IDisposable> WatchTextCaretMovedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler);
    Task<IDisposable> WatchTextSelectionChangedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler);
    Task<IDisposable> WatchSelectionChangedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler);
    Task<IDisposable> WatchVisibleDataChangedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler);
    Task<IDisposable> WatchAnnouncementAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler);
}

[StructLayout(LayoutKind.Sequential)]
internal struct AccessibleCacheItem
{
    internal (string,ObjectPath) Reference, Application, Parent;
    internal int Index, Children;
    internal string[] Interfaces;
    internal string Name;
    internal uint Role;
    internal string Description;
    internal uint[] States;
}

[DBusInterface("org.a11y.atspi.Cache")]
internal interface IAccessibleCache : IDBusObject
{
    Task<AccessibleCacheItem[]> GetItemsAsync();
    Task<IDisposable> WatchAddAccessibleAsync([Argument] Action<AccessibleCacheItem> handler);
    Task<IDisposable> WatchRemoveAccessibleAsync([Argument] Action<(string,ObjectPath)> handler);
}

[DBusInterface("org.a11y.atspi.Table")]
internal interface IAccessibleTable : IDBusObject
{
    Task<object> GetAsync(string property);
    Task<IDictionary<string,object>> GetAllAsync();
    [return: Argument] Task<(string,ObjectPath)> GetAccessibleAtAsync(int row,int column);
    Task<int> GetIndexAtAsync(int row,int column);
    Task<int> GetRowAtIndexAsync(int index);
    Task<int> GetColumnAtIndexAsync(int index);
    Task<string> GetRowDescriptionAsync(int row);
    Task<string> GetColumnDescriptionAsync(int column);
    Task<int> GetRowExtentAtAsync(int row,int column);
    Task<int> GetColumnExtentAtAsync(int row,int column);
    [return: Argument] Task<(string,ObjectPath)> GetRowHeaderAsync(int row);
    [return: Argument] Task<(string,ObjectPath)> GetColumnHeaderAsync(int column);
    Task<int[]> GetSelectedRowsAsync();
    Task<int[]> GetSelectedColumnsAsync();
    Task<bool> IsRowSelectedAsync(int row);
    Task<bool> IsColumnSelectedAsync(int column);
    Task<bool> IsSelectedAsync(int row,int column);
    Task<bool> AddRowSelectionAsync(int row);
    Task<bool> AddColumnSelectionAsync(int column);
    Task<bool> RemoveRowSelectionAsync(int row);
    Task<bool> RemoveColumnSelectionAsync(int column);
    Task<(bool,int,int,int,int,bool)> GetRowColumnExtentsAtIndexAsync(int index);
}

[DBusInterface("org.a11y.atspi.TableCell")]
internal interface IAccessibleTableCell : IDBusObject
{
    Task<object> GetAsync(string property);
    Task<IDictionary<string,object>> GetAllAsync();
    Task<(bool,int,int,int,int)> GetRowColumnSpanAsync();
    Task<(string,ObjectPath)[]> GetColumnHeaderCellsAsync();
    Task<(string,ObjectPath)[]> GetRowHeaderCellsAsync();
}
