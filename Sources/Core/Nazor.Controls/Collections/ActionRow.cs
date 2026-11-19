using Nazor.Input;
using Nazor.Controls.Primitives;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A settings-style row: <see cref="Leading" /> content such as an icon, a <see cref="Title" /> with an optional
/// <see cref="Subtitle" />, <see cref="Trailing" /> content such as a switch or a value, and an optional
/// <see cref="Chevron" /> for rows that open another page. A row is either clickable (<see cref="IsClickable" /> or an
/// <see cref="OnClick" /> handler: it is a button activated by a click, Enter or Space), or it forwards a click anywhere
/// on it to the first focusable control of its trailing content (<see cref="ActivatesTrailing" />), as a label does for
/// its check box, or it is plain. Put rows in a <see cref="RowGroup" />. The parts are <c>ActionRowLeading</c>,
/// <c>ActionRowText</c> (with <c>ActionRowTitle</c>, <c>ActionRowSubtitle</c> and the <c>ChildContent</c>),
/// <c>ActionRowTrailing</c> and <c>ActionRowChevron</c>.
/// </summary>
public sealed class ActionRow : ContentControl, IDisposable
{
    private readonly string titleId = UniqueId.Create("action-row-title");
    private readonly string subtitleId = UniqueId.Create("action-row-subtitle");
    private RowGroup? registered;
    private GroupPosition? position;
    private int index, count;

    /// <summary>Gets or sets the main text of the row.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets the secondary text shown below the title.</summary>
    [Parameter] public string? Subtitle { get; set; }

    /// <summary>Gets or sets the content before the text, such as an icon or an avatar.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets the content after the text, such as a switch, a check box, a value or a button.</summary>
    [Parameter] public RenderFragment? Trailing { get; set; }

    /// <summary>Gets or sets whether a chevron shows that the row opens something.</summary>
    [Parameter] public bool Chevron { get; set; }

    /// <summary>Gets or sets whether the whole row is a button; it is implied by an <see cref="OnClick" /> handler.</summary>
    [Parameter] public bool IsClickable { get; set; }

    /// <summary>Gets or sets the callback invoked when a clickable row is activated by a click, Enter or Space.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnClick { get; set; }

    /// <summary>
    /// Gets or sets whether a click anywhere on the row activates the first focusable control of <see cref="Trailing" />,
    /// for example toggling its switch; the control itself stays the keyboard stop.
    /// </summary>
    [Parameter] public bool ActivatesTrailing { get; set; }

    /// <summary>Gets or sets whether the row and its content reject interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the enclosing group, supplied automatically.</summary>
    [CascadingParameter] public RowGroup? Group { get; set; }

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "ActionRow";

    private bool Clickable => IsClickable || OnClick.HasDelegate;

    /// <summary>Validates the parameters and joins the enclosing group.</summary>
    protected override void OnParametersSet()
    {
        if (Clickable && ActivatesTrailing) throw new InvalidOperationException("An ActionRow is either clickable or activates its trailing control, not both.");
        if (registered == Group) return;
        registered?.Unregister(this);
        registered = Group;
        registered?.Register(this);
    }

    internal void SetPosition(GroupPosition? value, int index, int count)
    {
        if (position == value && this.index == index && this.count == count) return;
        (position, this.index, this.count) = (value, index, count);
        StateHasChanged();
    }

    /// <summary>Adds the role, the states, the activation and the accessible name.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var clickable = Clickable;
        builder.AddAttribute(10, "role", clickable ? "button" : Group is not null ? "listitem" : "group");
        builder.AddAttribute(11, "disabled", Disabled);
        builder.AddAttribute(12, "clickable", clickable);
        builder.AddAttribute(13, "activates-trailing", ActivatesTrailing);
        builder.AddAttribute(14, "focusable", clickable && !Disabled);
        builder.AddAttribute(15, "position", position is { } p ? Vocabulary.Name(p) : null);
        if (Group is not null && count > 0)
        {
            builder.AddAttribute(16, "position-in-set", index);
            builder.AddAttribute(17, "size-of-set", count);
        }
        if (AccessibleName is null && LabeledBy is null && Title is not null) builder.AddAttribute(18, "labeled-by", titleId);
        if (AccessibleDescription is null && DescribedBy is null && Subtitle is not null) builder.AddAttribute(19, "described-by", subtitleId);
        if (clickable) builder.AddAttribute(20, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ClickAsync));
        else if (ActivatesTrailing) builder.AddAttribute(21, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ForwardAsync));
    }

    /// <summary>Adds the leading content, the text, the trailing content and the chevron.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Leading is not null)
        {
            builder.OpenElement(1000, "ActionRowLeading");
            builder.AddContent(1001, Leading);
            builder.CloseElement();
        }
        builder.OpenElement(1010, "ActionRowText");
        if (Title is not null)
        {
            builder.OpenElement(1011, "ActionRowTitle");
            builder.AddAttribute(1012, "id", titleId);
            builder.AddContent(1013, Title);
            builder.CloseElement();
        }
        if (Subtitle is not null)
        {
            builder.OpenElement(1014, "ActionRowSubtitle");
            builder.AddAttribute(1015, "id", subtitleId);
            builder.AddContent(1016, Subtitle);
            builder.CloseElement();
        }
        builder.AddContent(1017, ChildContent);
        builder.CloseElement();
        if (Trailing is not null)
        {
            builder.OpenElement(1020, "ActionRowTrailing");
            builder.AddContent(1021, Trailing);
            builder.CloseElement();
        }
        if (Chevron)
        {
            builder.OpenElement(1030, "ActionRowChevron");
            builder.AddAttribute(1031, "glyph", "chevron-right");
            builder.AddAttribute(1032, "semantic-hidden", true);
            builder.CloseElement();
        }
    }

    private Task ClickAsync(PointerEventArgs e) => Disabled ? Task.CompletedTask : OnClick.InvokeAsync(e);

    private async Task ForwardAsync()
    {
        if (Disabled || Element.Element is not { Root: { } root } row) return;
        var trailing = ControlTree.Descendants(row).FirstOrDefault(node => node.Name == "ActionRowTrailing");
        var control = trailing is null ? null : ControlTree.Descendants(trailing).FirstOrDefault(ControlTree.IsFocusable);
        if (control is null) return;
        await new ElementReference(control).FocusAsync();
        await root.Renderer.DispatchEventAsync(control, "onclick", new PointerEventArgs { IsSynthetic = true });
    }

    /// <summary>Leaves the enclosing group.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }
}

/// <summary>
/// A titled group of <see cref="ActionRow" /> rows, as a section of a settings page: a <see cref="Title" />, an optional
/// <see cref="Description" /> and <see cref="HeaderActions" /> above a boxed list. The element <c>RowGroup</c> (role
/// <c>group</c>, named by its title) holds <c>RowGroupHeader</c> (with <c>RowGroupHeading</c>, <c>RowGroupTitle</c>,
/// <c>RowGroupDescription</c> and <c>RowGroupActions</c>) and <c>RowGroupList</c> (role <c>list</c>), whose rows receive
/// the <c>position</c> attribute (only, first, middle, last) so themes can round the ends of the box.
/// </summary>
public sealed class RowGroup : ContentControl
{
    private readonly string titleId = UniqueId.Create("row-group-title");
    private readonly string descriptionId = UniqueId.Create("row-group-description");
    private readonly List<ActionRow> rows = [];
    private HoverTracker hover = null!;
    private ElementReference list;

    /// <summary>Initializes the list's decorative hover tracker.</summary>
    protected override void OnInitialized() => hover = new(StateHasChanged);

    /// <summary>Gets or sets the title of the group.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets the text that explains the group, below the title.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Gets or sets controls shown at the end of the header, such as an add button.</summary>
    [Parameter] public RenderFragment? HeaderActions { get; set; }

    /// <summary>Gets or sets whether the group and its rows reject interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => "RowGroup";

    internal void Register(ActionRow row) => rows.Add(row);

    internal void Unregister(ActionRow row) => rows.Remove(row);

    /// <summary>Adds the group role and its name.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "disabled", Disabled);
        if (AccessibleName is null && LabeledBy is null && Title is not null) builder.AddAttribute(12, "labeled-by", titleId);
        if (AccessibleDescription is null && DescribedBy is null && Description is not null) builder.AddAttribute(13, "described-by", descriptionId);
    }

    /// <summary>Adds the header and the list of rows.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Title is not null || Description is not null || HeaderActions is not null)
        {
            builder.OpenElement(1000, "RowGroupHeader");
            builder.OpenElement(1001, "RowGroupHeading");
            if (Title is not null)
            {
                builder.OpenElement(1002, "RowGroupTitle");
                builder.AddAttribute(1003, "id", titleId);
                builder.AddAttribute(1004, "role", "heading");
                builder.AddAttribute(1005, "level", 2);
                builder.AddContent(1006, Title);
                builder.CloseElement();
            }
            if (Description is not null)
            {
                builder.OpenElement(1007, "RowGroupDescription");
                builder.AddAttribute(1008, "id", descriptionId);
                builder.AddContent(1009, Description);
                builder.CloseElement();
            }
            builder.CloseElement();
            if (HeaderActions is not null)
            {
                builder.OpenElement(1010, "RowGroupActions");
                builder.AddContent(1011, HeaderActions);
                builder.CloseElement();
            }
            builder.CloseElement();
        }
        builder.OpenElement(1020, "RowGroupList");
        builder.AddAttribute(1021, "role", "list");
        builder.AddMultipleAttributes(1022, hover.Attributes(this, () => list, row => row.Name == "ActionRow" && row.Attributes.GetValueOrDefault("clickable") is true));
        builder.AddElementReferenceCapture(1023, element => list = element);
        builder.OpenComponent<Indicator>(1024);
        builder.AddComponentParameter(1025, nameof(Indicator.As), "HoverIndicator");
        builder.AddComponentParameter(1026, nameof(Indicator.Target), hover.Target);
        builder.CloseComponent();
        builder.OpenComponent<CascadingValue<RowGroup>>(1030);
        builder.AddComponentParameter(1031, "Value", this);
        builder.AddComponentParameter(1032, "IsFixed", true);
        builder.AddComponentParameter(1033, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    /// <summary>Tells each row its position in the list once the rows are laid out.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        hover.Refresh();
        if (Element.Element is not { } group) return;
        var order = ControlTree.Descendants(group).Where(node => node.Name == "ActionRow").ToList();
        var placed = rows.Select(row => (Row: row, Index: row.Element.Element is { } element ? order.IndexOf(element) : -1))
            .Where(pair => pair.Index >= 0).OrderBy(pair => pair.Index).Select(pair => pair.Row).ToList();
        for (var i = 0; i < placed.Count; i++)
        {
            placed[i].SetPosition(Vocabulary.Position(i, placed.Count), i + 1, placed.Count);
        }
    }
}
