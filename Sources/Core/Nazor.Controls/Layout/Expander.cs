using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The side of an expander's header where its content appears.</summary>
public enum ExpandDirection
{
    /// <summary>The content appears below the header.</summary>
    Down,
    /// <summary>The content appears above the header.</summary>
    Up,
}

/// <summary>
/// A header that shows and hides content above or below it, such as advanced settings: a click on the header, Enter or
/// Space toggle it, and <see cref="IsExpanded" /> supports <c>@bind-IsExpanded</c>. The header shows
/// <see cref="Leading" /> content, the title (<see cref="Header" /> or <see cref="HeaderContent" />) with an optional
/// <see cref="Subtitle" />, <see cref="Trailing" /> content and a chevron. The parts are <c>{Name}Header</c> (a button
/// with the <c>expanded</c> state that controls the panel), <c>{Name}Leading</c>, <c>{Name}HeaderText</c>,
/// <c>{Name}Title</c>, <c>{Name}Subtitle</c>, <c>{Name}Trailing</c>, <c>{Name}Chevron</c> (a stable <c>chevron-down</c>
/// glyph whose rotation is themed) and the <see cref="Collapsible" /> <c>{Name}Panel</c> with its <c>{Name}PanelContent</c>,
/// where <c>{Name}</c> is <c>Expander</c> (or <c>AccordionItem</c> in an accordion). The element has the
/// <c>expanded</c> and <c>disabled</c> attributes.
/// </summary>
public class Expander : ContentControl
{
    private readonly ControllableState<bool> expanded = new(false);
    private readonly string headerId = UniqueId.Create("expander-header");
    private readonly string panelId = UniqueId.Create("expander-panel");

    /// <summary>Gets or sets the title of the header as plain text.</summary>
    [Parameter] public string? Header { get; set; }

    /// <summary>Gets or sets the title of the header as content; it replaces <see cref="Header" />.</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }

    /// <summary>Gets or sets a secondary line below the title.</summary>
    [Parameter] public string? Subtitle { get; set; }

    /// <summary>Gets or sets content shown at the start of the header, usually an <see cref="Icon" />.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>
    /// Gets or sets content shown at the end of the header before the chevron, such as a value or a switch; its own
    /// controls keep their actions.
    /// </summary>
    [Parameter] public RenderFragment? Trailing { get; set; }

    /// <summary>Gets or sets whether the content is shown.</summary>
    [Parameter] public bool IsExpanded { get; set; }

    /// <summary>Gets or sets whether content appears below or above the header; Down by default.</summary>
    [Parameter] public ExpandDirection ExpandDirection { get; set; }

    /// <summary>Gets or sets whether collapsed content stays mounted for local state and natural measurement; false by default.</summary>
    [Parameter] public bool KeepMounted { get; set; }

    /// <summary>Gets or sets the callback invoked when the user expands or collapses the content.</summary>
    [Parameter] public EventCallback<bool> IsExpandedChanged { get; set; }

    /// <summary>Gets or sets whether the header rejects interaction; the content is disabled too.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets whether the content is currently shown.</summary>
    public virtual bool Expanded => expanded.Value;

    /// <summary>Gets the identifier of the header, which names the panel.</summary>
    public string HeaderId => headerId;

    /// <summary>Gets a reference to the header element, the focus target of the expander.</summary>
    public ElementReference HeaderElement { get; private set; }

    /// <summary>Gets whether the expander currently rejects interaction.</summary>
    protected virtual bool IsInactive => Disabled;

    /// <summary>Gets the NSS element name emitted by Expander.</summary>
    protected override string ElementName => "Expander";

    /// <summary>Synchronizes the expanded state with the parameter.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(ExpandDirection)) throw new ArgumentOutOfRangeException(nameof(ExpandDirection));
        expanded.Sync(IsExpanded);
    }

    /// <summary>Adds the expanded and disabled states.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "expanded", Expanded);
        builder.AddAttribute(11, "disabled", IsInactive);
        builder.AddAttribute(12, "expand-direction", ExpandDirection == ExpandDirection.Up ? "up" : "down");
    }

    /// <summary>Adds the header and the collapsible panel.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var name = ElementName;
        builder.OpenElement(1000, name + "Header");
        builder.AddAttribute(1001, "id", headerId);
        builder.AddAttribute(1002, "role", "button");
        builder.AddAttribute(1003, "expanded", Expanded);
        builder.AddAttribute(1004, "controls", panelId);
        builder.AddAttribute(1005, "focusable", !IsInactive);
        builder.AddAttribute(1006, "disabled", IsInactive);
        builder.AddAttribute(1007, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleAsync));
        AddHeaderAttributes(builder);
        builder.AddElementReferenceCapture(1009, reference => HeaderElement = reference);
        if (Leading is not null)
        {
            builder.OpenElement(1010, name + "Leading");
            builder.AddContent(1011, Leading);
            builder.CloseElement();
        }
        builder.OpenElement(1020, name + "HeaderText");
        builder.OpenElement(1021, name + "Title");
        if (HeaderContent is not null) builder.AddContent(1022, HeaderContent);
        else builder.AddContent(1023, Header);
        builder.CloseElement();
        if (!string.IsNullOrEmpty(Subtitle))
        {
            builder.OpenElement(1024, name + "Subtitle");
            builder.AddContent(1025, Subtitle);
            builder.CloseElement();
        }
        builder.CloseElement();
        if (Trailing is not null)
        {
            builder.OpenElement(1030, name + "Trailing");
            builder.AddContent(1031, Trailing);
            builder.CloseElement();
        }
        builder.OpenElement(1040, name + "Chevron");
        builder.AddAttribute(1041, "glyph", "chevron-down");
        builder.AddAttribute(1042, "semantic-hidden", true);
        builder.CloseElement();
        builder.CloseElement();

        builder.OpenComponent<Collapsible>(1100);
        builder.AddComponentParameter(1101, nameof(Collapsible.As), name + "Panel");
        builder.AddComponentParameter(1102, nameof(Collapsible.IsOpen), Expanded);
        builder.AddComponentParameter(1103, nameof(Control.Id), panelId);
        builder.AddComponentParameter(1104, nameof(Collapsible.Disabled), IsInactive);
        builder.AddComponentParameter(1105, nameof(Control.LabeledBy), headerId);
        builder.AddComponentParameter(1106, "role", "group");
        builder.AddComponentParameter(1107, nameof(ContentControl.ChildContent), ChildContent);
        builder.AddComponentParameter(1108, nameof(Collapsible.ReturnFocus), HeaderElement);
        builder.AddComponentParameter(1109, nameof(Collapsible.KeepMounted), KeepMounted);
        builder.CloseComponent();
    }

    /// <summary>Adds attributes of a derived control to the header element, with sequence numbers 1008.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected virtual void AddHeaderAttributes(RenderTreeBuilder builder)
    {
    }

    /// <summary>Expands the content when collapsed and collapses it when expanded, as a click on the header does.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public virtual Task ToggleAsync() => SetExpandedAsync(!Expanded);

    /// <summary>Expands or collapses the content as the user does and reports the change.</summary>
    /// <param name="value">Whether the content should be shown.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public virtual async Task SetExpandedAsync(bool value)
    {
        if (IsInactive || Element.Element?.IsDisabled is true || !expanded.Set(value)) return;
        StateHasChanged();
        await IsExpandedChanged.InvokeAsync(value);
        await Events.RaiseChangeAsync(Element, value);
    }

    internal void Refresh() => StateHasChanged();
}
