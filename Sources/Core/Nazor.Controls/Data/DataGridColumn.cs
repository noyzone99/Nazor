using System.Globalization;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>How the content of a <see cref="DataGridColumn{TItem}" /> is aligned, written as the <c>align</c> attribute.</summary>
public enum DataGridAlignment
{
    /// <summary>Aligned to the start, as text.</summary>
    Start,
    /// <summary>Centered, as marks and short codes.</summary>
    Center,
    /// <summary>Aligned to the end, as numbers and amounts.</summary>
    End,
}

/// <summary>Which lines a <see cref="DataGrid{TItem}" /> draws between cells, written as the <c>gridlines</c> attribute.</summary>
public enum DataGridLines
{
    /// <summary>No lines.</summary>
    None,
    /// <summary>Lines between rows.</summary>
    Horizontal,
    /// <summary>Lines between columns.</summary>
    Vertical,
    /// <summary>Lines between rows and between columns.</summary>
    Both,
}

/// <summary>A cell value changed by the user in a <see cref="DataGrid{TItem}" />.</summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
/// <param name="Item">The edited item.</param>
/// <param name="Column">The key of the edited column.</param>
/// <param name="OldValue">The value before the edit.</param>
/// <param name="NewValue">The committed value.</param>
public sealed record DataGridCellEdit<TItem>(TItem Item, string Column, object? OldValue, object? NewValue);

/// <summary>
/// The state of a cell being edited, given to <see cref="DataGridColumn{TItem}.EditorTemplate" />: the editor reads and
/// changes <see cref="Value" /> and calls <see cref="CommitAsync" /> or <see cref="Cancel" />. Enter commits and Escape
/// cancels from any editor; moving the focus out of the cell commits.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed class DataGridEditContext<TItem>
{
    private readonly Func<Task> commit;
    private readonly Action cancel;

    internal DataGridEditContext(TItem item, string column, object? value, Func<Task> commit, Action cancel)
    {
        Item = item;
        Column = column;
        OldValue = value;
        Value = value;
        this.commit = commit;
        this.cancel = cancel;
    }

    /// <summary>Gets the edited item.</summary>
    public TItem Item { get; }

    /// <summary>Gets the key of the edited column.</summary>
    public string Column { get; }

    /// <summary>Gets the value before the edit.</summary>
    public object? OldValue { get; }

    /// <summary>Gets or sets the value that a commit stores.</summary>
    public object? Value { get; set; }

    /// <summary>Gets or sets whether the value is not acceptable; a commit is refused while it is set.</summary>
    public bool Invalid { get; set; }

    /// <summary>Stores <see cref="Value" /> and ends the edit, unless <see cref="Invalid" /> is set.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task CommitAsync() => commit();

    /// <summary>Ends the edit without storing anything.</summary>
    public void Cancel() => cancel();
}

/// <summary>
/// A column of a <see cref="DataGrid{TItem}" />, declared inside it. A column shows <see cref="Field" /> formatted with
/// <see cref="Format" />, or <see cref="CellTemplate" />; its header shows <see cref="Header" /> or
/// <see cref="HeaderTemplate" />. Its width is <see cref="Width" />, or a share of the remaining space by
/// <see cref="Flex" />, within <see cref="MinWidth" /> and <see cref="MaxWidth" />; the user can resize it
/// (<see cref="Resizable" />), sort by it (<see cref="Sortable" />) and edit its cells (<see cref="Editable" />). Frozen
/// columns stay at the start while the grid scrolls sideways.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
public sealed class DataGridColumn<TItem> : ComponentBase, IDisposable
{
    private DataGrid<TItem>? registered;
    private object?[]? shown;

    /// <summary>Gets or sets the stable key of the column, used by sorts and reordering; the header text when null.</summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>Gets or sets the header text, also the accessible name of the column.</summary>
    [Parameter] public string? Header { get; set; }

    /// <summary>Gets or sets the header content, replacing <see cref="Header" /> on screen.</summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>Gets or sets the function that reads the value of the column; it is also the default sort key and edited value.</summary>
    [Parameter] public Func<TItem, object?>? Field { get; set; }

    /// <summary>Gets or sets the format string applied to values that are <see cref="IFormattable" />, such as <c>N2</c> or <c>d</c>.</summary>
    [Parameter] public string? Format { get; set; }

    /// <summary>Gets or sets the culture used to format and parse values; the current culture when null.</summary>
    [Parameter] public IFormatProvider? FormatProvider { get; set; }

    /// <summary>Gets or sets the content of a cell, replacing the formatted value.</summary>
    [Parameter] public RenderFragment<TItem>? CellTemplate { get; set; }

    /// <summary>Gets or sets the width in pixels, or null to use <see cref="Flex" /> or the default width of 120 pixels.</summary>
    [Parameter] public double? Width { get; set; }

    /// <summary>Gets or sets the smallest width the column can have.</summary>
    [Parameter] public double MinWidth { get; set; } = 40;

    /// <summary>Gets or sets the largest width the column can have.</summary>
    [Parameter] public double MaxWidth { get; set; } = double.PositiveInfinity;

    /// <summary>Gets or sets the share of the remaining width the column takes, zero for none; used when <see cref="Width" /> is null.</summary>
    [Parameter] public double Flex { get; set; }

    /// <summary>Gets or sets how the content of the cells and of the header is aligned.</summary>
    [Parameter] public DataGridAlignment Alignment { get; set; }

    /// <summary>Gets or sets whether clicking the header or pressing Enter on it sorts by the column, when it has a sort key.</summary>
    [Parameter] public bool Sortable { get; set; } = true;

    /// <summary>Gets or sets the function that reads the sort key, when it differs from <see cref="Field" />.</summary>
    [Parameter] public Func<TItem, object?>? SortBy { get; set; }

    /// <summary>Gets or sets the comparer of the sort keys, or null for the default one.</summary>
    [Parameter] public IComparer<object?>? Comparer { get; set; }

    /// <summary>Gets or sets whether the user can change the width with the handle at the end of the header.</summary>
    [Parameter] public bool Resizable { get; set; } = true;

    /// <summary>Gets or sets whether the column stays at the start while the grid scrolls sideways.</summary>
    [Parameter] public bool Frozen { get; set; }

    /// <summary>Gets or sets whether the column is not shown.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Gets or sets whether the cells can be edited with Enter, F2 or a double click.</summary>
    [Parameter] public bool Editable { get; set; }

    /// <summary>Gets or sets the function that stores a committed value in an item; without it only the grid's callback is invoked.</summary>
    [Parameter] public Action<TItem, object?>? Setter { get; set; }

    /// <summary>
    /// Gets or sets the conversion of the text typed in the default editor to a value; by default the text is converted to
    /// the type of the current value with the column's culture. Throw <see cref="FormatException" /> to reject the text.
    /// </summary>
    [Parameter] public Func<string, object?>? Parse { get; set; }

    /// <summary>Gets or sets the editor of a cell, replacing the default text editor.</summary>
    [Parameter] public RenderFragment<DataGridEditContext<TItem>>? EditorTemplate { get; set; }

    /// <summary>Gets or sets the content of the footer cell, which receives every item that passes the filter, for aggregates.</summary>
    [Parameter] public RenderFragment<IReadOnlyList<TItem>>? FooterTemplate { get; set; }

    /// <summary>Gets or sets whether the cells of this column name their rows for assistive technologies (<c>rowheader</c>).</summary>
    [Parameter] public bool IsRowHeader { get; set; }

    /// <summary>Gets or sets NSS classes added to the header and the cells of the column.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Gets or sets the enclosing grid, supplied automatically.</summary>
    [CascadingParameter] public DataGrid<TItem>? Grid { get; set; }

    internal string ColumnKey { get; set; } = "";

    internal Func<TItem, object?>? SortKey => SortBy ?? Field;

    internal bool CanSort => Sortable && SortKey is not null;

    internal IFormatProvider Culture => FormatProvider ?? CultureInfo.CurrentCulture;

    /// <summary>Validates the parameters and joins the enclosing grid.</summary>
    protected override void OnParametersSet()
    {
        if (Grid is null) throw new InvalidOperationException("DataGridColumn must be inside a DataGrid of the same item type.");
        if (Width is { } width && (!double.IsFinite(width) || width < 0)) throw new ArgumentOutOfRangeException(nameof(Width));
        if (!double.IsFinite(MinWidth) || MinWidth < 0) throw new ArgumentOutOfRangeException(nameof(MinWidth));
        if (double.IsNaN(MaxWidth) || MaxWidth < MinWidth) throw new ArgumentOutOfRangeException(nameof(MaxWidth));
        if (!double.IsFinite(Flex) || Flex < 0) throw new ArgumentOutOfRangeException(nameof(Flex));
        if (!Enum.IsDefined(Alignment)) throw new ArgumentOutOfRangeException(nameof(Alignment));
        object?[] state = [Key, Header, HeaderTemplate, Field, Format, FormatProvider, CellTemplate, Width, MinWidth, MaxWidth, Flex, Alignment,
            Sortable, SortBy, Comparer, Resizable, Frozen, Hidden, Editable, Setter, Parse, EditorTemplate, FooterTemplate, IsRowHeader, Class];
        if (registered != Grid)
        {
            registered?.Unregister(this);
            (registered = Grid).Register(this);
        }
        else if (shown is null || !shown.SequenceEqual(state)) registered.ColumnChanged();
        shown = state;
    }

    /// <summary>Renders nothing; the grid renders the column.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }

    internal object? ValueOf(TItem item) => Field?.Invoke(item);

    internal string TextOf(TItem item) => FormatValue(ValueOf(item));

    internal string FormatValue(object? value) =>
        value is IFormattable formattable && Format is not null ? formattable.ToString(Format, Culture) : Convert.ToString(value, Culture) ?? "";

    internal object? ParseText(string text, object? old)
    {
        if (Parse is { } parse) return parse(text);
        if (old is null || old is string) return text;
        var type = old.GetType();
        if (type.IsEnum) return Enum.Parse(type, text, ignoreCase: true);
        if (old is DateTime) return DateTime.Parse(text, Culture);
        if (old is DateTimeOffset) return DateTimeOffset.Parse(text, Culture);
        if (old is IConvertible) return Convert.ChangeType(text, type, Culture);
        throw new FormatException("The column has no conversion for values of type " + type.Name + ".");
    }

    /// <summary>Leaves the enclosing grid.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }
}
