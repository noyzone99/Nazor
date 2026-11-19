using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A static document table, laid out by the engine's table layout: columns share their widths across every row, cells
/// span columns and rows, and the table is exposed to assistive technologies with its rows, headers and cells. Its
/// direct children are an optional <see cref="TableHeader" />, one or more <see cref="TableBody" /> groups and an
/// optional <see cref="TableFooter" />, in that order; the optional <see cref="Caption" /> names the table. It has no
/// selection, sorting or virtualization: use a data grid for that. The element <c>Table</c> (role <c>table</c>) carries
/// the <c>striped</c>, <c>bordered</c> and <c>dense</c> attributes; the caption is <c>TableCaption</c>.
/// </summary>
public sealed class Table : ContentControl
{
    /// <summary>Gets or sets the visible caption, also the accessible name of the table.</summary>
    [Parameter] public RenderFragment? Caption { get; set; }

    /// <summary>Gets or sets whether every other body row is shaded, for long tables.</summary>
    [Parameter] public bool Striped { get; set; }

    /// <summary>Gets or sets whether every cell has a border, instead of lines between rows only.</summary>
    [Parameter] public bool Bordered { get; set; }

    /// <summary>Gets or sets whether the cells have less padding.</summary>
    [Parameter] public bool Dense { get; set; }

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "Table";

    /// <summary>Adds the table semantics, the table layout and the style options.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "table");
        builder.AddAttribute(11, "table-part", "table");
        builder.AddAttribute(12, "striped", Striped);
        builder.AddAttribute(13, "bordered", Bordered);
        builder.AddAttribute(14, "dense", Dense);
    }

    /// <summary>Checks that the direct children are row groups.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (Element.Element?.Children.OfType<ElementNode>().Any(child => child.Attributes.GetValueOrDefault("table-part") is not ("group" or "caption")) is true)
            throw new InvalidOperationException("Table accepts TableHeader, TableBody and TableFooter as direct children.");
    }

    /// <summary>Adds the caption followed by the row groups.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Caption is not null)
        {
            builder.OpenElement(1000, "TableCaption");
            builder.AddAttribute(1001, "role", "text");
            builder.AddAttribute(1002, "table-part", "caption");
            builder.AddContent(1003, Caption);
            builder.CloseElement();
        }
        builder.AddContent(1010, ChildContent);
    }
}

/// <summary>A group of rows directly inside a <see cref="Table" />: <see cref="TableHeader" />, <see cref="TableBody" /> or <see cref="TableFooter" />.</summary>
public abstract class TableRowGroup : ContentControl
{
    /// <summary>Checks that the group is inside a table and holds rows.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (Element.Element is { } group && (group.ParentElement?.Attributes.GetValueOrDefault("table-part") is not "table" ||
            group.Children.OfType<ElementNode>().Any(child => child.Attributes.GetValueOrDefault("table-part") is not "row")))
            throw new InvalidOperationException(ElementName + " requires a Table parent and TableRow children.");
    }

    /// <summary>Adds the row group semantics.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "rowgroup");
        builder.AddAttribute(11, "table-part", "group");
    }
}

/// <summary>The header rows of a <see cref="Table" />, usually one row of <see cref="TableHeaderCell" /> cells. The element is <c>TableHeader</c>.</summary>
public sealed class TableHeader : TableRowGroup
{
    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "TableHeader";
}

/// <summary>A group of body rows of a <see cref="Table" />. The element is <c>TableBody</c>.</summary>
public sealed class TableBody : TableRowGroup
{
    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "TableBody";
}

/// <summary>The footer rows of a <see cref="Table" />, such as totals, placed after the bodies. The element is <c>TableFooter</c>.</summary>
public sealed class TableFooter : TableRowGroup
{
    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "TableFooter";
}

/// <summary>
/// A row of a <see cref="TableHeader" />, <see cref="TableBody" /> or <see cref="TableFooter" />, holding
/// <see cref="TableCell" /> and <see cref="TableHeaderCell" /> cells. The element <c>TableRow</c> (role <c>row</c>)
/// carries <c>alternate</c> on every second row of its group, for striped tables.
/// </summary>
public sealed class TableRow : ContentControl
{
    private bool alternate;

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "TableRow";

    /// <summary>Checks that the row is inside a row group and holds cells, and updates its alternation.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (Element.Element is not { } row) return;
        if (row.ParentElement?.Attributes.GetValueOrDefault("table-part") is not "group" ||
            row.Children.OfType<ElementNode>().Any(child => child.Attributes.GetValueOrDefault("table-part") is not "cell"))
            throw new InvalidOperationException("TableRow requires a TableHeader, TableBody or TableFooter parent and cell children.");
        var index = row.ParentElement.Children.OfType<ElementNode>().TakeWhile(node => !ReferenceEquals(node, row)).Count();
        if (alternate == (index % 2 == 1)) return;
        alternate = index % 2 == 1;
        StateHasChanged();
    }

    /// <summary>Adds the row semantics and the alternation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "row");
        builder.AddAttribute(11, "table-part", "row");
        builder.AddAttribute(12, "alternate", alternate);
    }
}

/// <summary>Whether a <see cref="TableHeaderCell" /> describes its column or its row.</summary>
public enum TableHeaderScope
{
    /// <summary>The header describes the cells of its column.</summary>
    Column,
    /// <summary>The header describes the cells of its row.</summary>
    Row,
}

/// <summary>A data cell of a <see cref="TableRow" />, which can span several columns and rows. The element is <c>TableCell</c> (role <c>cell</c>).</summary>
public class TableCell : ContentControl
{
    /// <summary>Gets or sets the number of columns the cell occupies, from 1 to 1000.</summary>
    [Parameter] public int ColumnSpan { get; set; } = 1;

    /// <summary>Gets or sets the number of rows the cell occupies, from 1 to 65534; a span stops at the end of its row group.</summary>
    [Parameter] public int RowSpan { get; set; } = 1;

    /// <summary>Gets the accessible role of the cell.</summary>
    protected virtual string Role => "cell";

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "TableCell";

    /// <summary>Validates the spans.</summary>
    protected override void OnParametersSet()
    {
        if (ColumnSpan is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(ColumnSpan));
        if (RowSpan is < 1 or > 65534) throw new ArgumentOutOfRangeException(nameof(RowSpan));
    }

    /// <summary>Checks that the cell is inside a row.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (Element.Element?.ParentElement?.Attributes.GetValueOrDefault("table-part") is not "row")
            throw new InvalidOperationException(ElementName + " requires a TableRow parent.");
    }

    /// <summary>Adds the cell semantics and the spans.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", Role);
        builder.AddAttribute(11, "table-part", "cell");
        builder.AddAttribute(12, "column-span", ColumnSpan);
        builder.AddAttribute(13, "row-span", RowSpan);
    }
}

/// <summary>
/// A header cell of a <see cref="TableRow" />: it names the cells of its column or, with <see cref="Scope" />
/// <see cref="TableHeaderScope.Row" />, of its row. The element <c>TableHeaderCell</c> has the role
/// <c>columnheader</c> or <c>rowheader</c> and the <c>scope</c> attribute.
/// </summary>
public sealed class TableHeaderCell : TableCell
{
    /// <summary>Gets or sets whether the header describes its column or its row.</summary>
    [Parameter] public TableHeaderScope Scope { get; set; }

    /// <summary>Gets the accessible role matching the scope.</summary>
    protected override string Role => Scope == TableHeaderScope.Row ? "rowheader" : "columnheader";

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "TableHeaderCell";

    /// <summary>Validates the scope and the spans.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Scope)) throw new ArgumentOutOfRangeException(nameof(Scope));
        base.OnParametersSet();
    }

    /// <summary>Adds the header semantics, the scope and the spans.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        base.AddAttributes(builder);
        builder.AddAttribute(20, "scope", Scope == TableHeaderScope.Row ? "row" : "column");
    }
}
