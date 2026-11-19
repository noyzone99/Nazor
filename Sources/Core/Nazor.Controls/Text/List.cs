using System.Globalization;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The marker drawn before the items of a <see cref="List" />, written as the <c>marker-style</c> attribute.</summary>
public enum ListMarkerStyle
{
    /// <summary>
    /// Decimal numbers for an ordered list; for an unordered list a disc, a circle in a nested list and a square deeper.
    /// </summary>
    Default,
    /// <summary>A filled disc.</summary>
    Disc,
    /// <summary>An open circle.</summary>
    Circle,
    /// <summary>A filled square.</summary>
    Square,
    /// <summary>Decimal numbers: 1, 2, 3.</summary>
    Decimal,
    /// <summary>Lower-case letters: a, b, c, then aa after z.</summary>
    LowerAlpha,
    /// <summary>Upper-case letters: A, B, C, then AA after Z.</summary>
    UpperAlpha,
    /// <summary>Lower-case Roman numerals: i, ii, iii.</summary>
    LowerRoman,
    /// <summary>Upper-case Roman numerals: I, II, III.</summary>
    UpperRoman,
    /// <summary>No visible marker; the items remain a list for assistive technologies.</summary>
    None,
}

/// <summary>
/// A document list of <see cref="ListItem" /> children, bulleted or numbered (<see cref="Ordered" />). Numbering starts
/// at <see cref="Start" />, can count down (<see cref="Reversed" />) and continues from any item that sets
/// <see cref="ListItem.Value" />. Lists nest inside items; the element has the <c>list</c> role, the <c>ordered</c>,
/// <c>reversed</c> and <c>marker-style</c> attributes and the nesting <c>level</c> (1 for an outer list). A document list
/// has no selection or focus of its own: use a list view for that.
/// </summary>
public sealed class List : ContentControl
{
    private readonly List<ListItem> items = [];

    /// <summary>Gets or sets whether the items are numbered; it chooses the default marker.</summary>
    [Parameter] public bool Ordered { get; set; }

    /// <summary>Gets or sets the number of the first item; null starts at one, or at the item count when reversed.</summary>
    [Parameter] public int? Start { get; set; }

    /// <summary>Gets or sets whether the numbers decrease from item to item.</summary>
    [Parameter] public bool Reversed { get; set; }

    /// <summary>Gets or sets the marker drawn before each item.</summary>
    [Parameter] public ListMarkerStyle MarkerStyle { get; set; }

    /// <summary>Gets or sets the list that contains this one, supplied automatically.</summary>
    [CascadingParameter] public List? Parent { get; set; }

    /// <summary>Gets the nesting level: 1 for an outer list, 2 for a list inside one of its items, and so on.</summary>
    public int Level => Parent is null ? 1 : Parent.Level + 1;

    /// <summary>Gets the marker style in effect once <see cref="ListMarkerStyle.Default" /> is resolved.</summary>
    public ListMarkerStyle EffectiveMarkerStyle => MarkerStyle != ListMarkerStyle.Default ? MarkerStyle
        : Ordered ? ListMarkerStyle.Decimal
        : Level switch { 1 => ListMarkerStyle.Disc, 2 => ListMarkerStyle.Circle, _ => ListMarkerStyle.Square };

    /// <summary>Gets the NSS element name emitted by List.</summary>
    protected override string ElementName => "List";

    /// <summary>Validates the marker style and renumbers the items.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(MarkerStyle)) throw new ArgumentOutOfRangeException(nameof(MarkerStyle));
        Refresh();
    }

    /// <summary>Adds the list role and the numbering attributes.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "list");
        builder.AddAttribute(11, "ordered", Ordered);
        builder.AddAttribute(12, "reversed", Reversed);
        builder.AddAttribute(13, "marker-style", Name(EffectiveMarkerStyle));
        builder.AddAttribute(14, "level", Level);
    }

    /// <summary>Supplies the list to its items and nested lists.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<List>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    internal void Register(ListItem item)
    {
        if (items.Contains(item)) return;
        items.Add(item);
        Refresh();
    }

    internal void Unregister(ListItem item)
    {
        if (items.Remove(item)) Refresh();
    }

    internal void Refresh()
    {
        foreach (var item in items.ToArray()) item.Refresh();
    }

    internal (int Position, int Size, int Number, string Marker) Describe(ListItem item)
    {
        var ordered = items.Where(value => value.Element.Element is not { IsRemoved: true }).ToList();
        if (Element.Element is { } list)
        {
            var nodes = ControlTree.Descendants(list).ToList();
            ordered = [.. ordered.OrderBy(value => value.Element.Element is { } node ? nodes.IndexOf(node) : int.MaxValue)];
        }
        if (!ordered.Contains(item)) ordered.Add(item);
        var index = ordered.IndexOf(item);
        var number = Start ?? (Reversed ? ordered.Count : 1);
        for (var i = 0; i <= index; i++)
        {
            number = ordered[i].Value ?? number;
            if (i != index) number += Reversed ? -1 : 1;
        }
        return (index + 1, ordered.Count, number, Marker(EffectiveMarkerStyle, number));
    }

    /// <summary>Formats the marker of an item number in a marker style.</summary>
    /// <param name="style">The marker style; <see cref="ListMarkerStyle.Default" /> formats a decimal number.</param>
    /// <param name="number">The item number.</param>
    /// <returns>The marker text, empty for <see cref="ListMarkerStyle.None" />.</returns>
    public static string Marker(ListMarkerStyle style, int number) => style switch
    {
        ListMarkerStyle.None => "",
        ListMarkerStyle.Disc => "•",
        ListMarkerStyle.Circle => "◦",
        ListMarkerStyle.Square => "▪",
        ListMarkerStyle.LowerAlpha => Alpha(number).ToLowerInvariant() + ".",
        ListMarkerStyle.UpperAlpha => Alpha(number) + ".",
        ListMarkerStyle.LowerRoman => Roman(number).ToLowerInvariant() + ".",
        ListMarkerStyle.UpperRoman => Roman(number) + ".",
        _ => number.ToString(CultureInfo.InvariantCulture) + ".",
    };

    private static string Name(ListMarkerStyle style) => style switch
    {
        ListMarkerStyle.Disc => "disc", ListMarkerStyle.Circle => "circle", ListMarkerStyle.Square => "square",
        ListMarkerStyle.LowerAlpha => "lower-alpha", ListMarkerStyle.UpperAlpha => "upper-alpha",
        ListMarkerStyle.LowerRoman => "lower-roman", ListMarkerStyle.UpperRoman => "upper-roman",
        ListMarkerStyle.None => "none", _ => "decimal",
    };

    private static string Alpha(int value)
    {
        if (value <= 0) return value.ToString(CultureInfo.InvariantCulture);
        var result = "";
        while (value > 0)
        {
            value--;
            result = (char)('A' + value % 26) + result;
            value /= 26;
        }
        return result;
    }

    private static readonly (int Value, string Text)[] Numerals =
        [(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"), (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")];

    private static string Roman(int value)
    {
        if (value is <= 0 or > 3999) return value.ToString(CultureInfo.InvariantCulture);
        var result = new System.Text.StringBuilder();
        foreach (var (number, text) in Numerals)
            while (value >= number)
            {
                result.Append(text);
                value -= number;
            }
        return result.ToString();
    }
}

/// <summary>
/// An item of a <see cref="List" />: a decorative <c>ListItemMarker</c> (the bullet or the number) and a
/// <c>ListItemContent</c> part with any content, including a nested list. The element has the <c>listitem</c> role, its
/// position in the list and the size of the list.
/// </summary>
public sealed class ListItem : ContentControl, IDisposable
{
    private List? registered;
    private (int Position, int Size, int Number, string Marker) description;

    /// <summary>Gets or sets the number of this item in an ordered list; the following items continue from it.</summary>
    [Parameter] public int? Value { get; set; }

    /// <summary>Gets or sets the enclosing list, supplied automatically.</summary>
    [CascadingParameter] public List? Owner { get; set; }

    /// <summary>Gets the number of the item as last rendered.</summary>
    public int Number => description.Number;

    /// <summary>Gets the NSS element name emitted by ListItem.</summary>
    protected override string ElementName => "ListItem";

    /// <summary>Joins the enclosing list.</summary>
    protected override void OnParametersSet()
    {
        if (Owner is null) throw new InvalidOperationException("ListItem must be inside a List.");
        if (registered == Owner)
        {
            Owner.Refresh();
            return;
        }
        registered?.Unregister(this);
        (registered = Owner).Register(this);
    }

    /// <summary>Renumbers the list once the item is laid out in its final order.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender) Owner?.Refresh();
    }

    /// <summary>Adds the list item role and its position.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        description = Owner!.Describe(this);
        builder.AddAttribute(10, "role", "listitem");
        builder.AddAttribute(11, "position-in-set", description.Position);
        builder.AddAttribute(12, "size-of-set", description.Size);
    }

    /// <summary>Adds the marker and the content.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "ListItemMarker");
        builder.AddAttribute(1001, "semantic-hidden", true);
        builder.AddContent(1002, description.Marker);
        builder.CloseElement();
        builder.OpenElement(1010, "ListItemContent");
        builder.AddContent(1011, ChildContent);
        builder.CloseElement();
    }

    internal void Refresh() => StateHasChanged();

    /// <summary>Leaves the list.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }
}

/// <summary>How a <see cref="DefinitionList" /> arranges its terms and descriptions, written as the <c>layout</c> attribute.</summary>
public enum DefinitionListLayout
{
    /// <summary>Each description below its term, indented.</summary>
    Stacked,
    /// <summary>Terms in a first column and descriptions beside them, as a property sheet.</summary>
    Inline,
}

/// <summary>
/// A list of terms and their descriptions, such as a glossary or the properties of a file: write
/// <see cref="DefinitionTerm" /> and <see cref="DefinitionDescription" /> children in order; a term can have several
/// descriptions. The element has the <c>definitionlist</c> role and the <c>layout</c> attribute.
/// </summary>
public sealed class DefinitionList : ContentControl
{
    /// <summary>Gets or sets how terms and descriptions are arranged.</summary>
    [Parameter] public DefinitionListLayout Layout { get; set; }

    /// <summary>Gets the NSS element name emitted by DefinitionList.</summary>
    protected override string ElementName => "DefinitionList";

    /// <summary>Validates the layout.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Layout)) throw new ArgumentOutOfRangeException(nameof(Layout));
    }

    /// <summary>Adds the definition list role and the layout.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "definitionlist");
        builder.AddAttribute(11, "layout", Layout == DefinitionListLayout.Inline ? "inline" : "stacked");
    }
}

/// <summary>A term of a <see cref="DefinitionList" />, with the <c>term</c> role.</summary>
public sealed class DefinitionTerm : ContentControl
{
    /// <summary>Gets the NSS element name emitted by DefinitionTerm.</summary>
    protected override string ElementName => "DefinitionTerm";

    /// <summary>Adds the term role.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "role", "term");
}

/// <summary>The description of the preceding <see cref="DefinitionTerm" />, with the <c>definition</c> role.</summary>
public sealed class DefinitionDescription : ContentControl
{
    /// <summary>Gets the NSS element name emitted by DefinitionDescription.</summary>
    protected override string ElementName => "DefinitionDescription";

    /// <summary>Adds the definition role.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "role", "definition");
}
