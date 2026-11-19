using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A titled frame around related controls, such as the options of a dialog: a group named by its header, the
/// replacement of HTML <c>fieldset</c> and <c>legend</c>. <see cref="Disabled" /> disables everything inside. With
/// <see cref="IsCheckable" /> the header is a check box (<see cref="IsChecked" />, <c>@bind-IsChecked</c>) that enables
/// the content only while checked; with <see cref="IsCollapsible" /> a header button shows and hides the content
/// (<see cref="IsExpanded" />, <c>@bind-IsExpanded</c>). The parts are <c>GroupBoxHeader</c>, holding the
/// <c>GroupBoxTitle</c> or the <c>CheckBox</c> and, when collapsible, the <c>GroupBoxToggle</c> button with its
/// <c>GroupBoxChevron</c>, and the <see cref="Collapsible" /> <c>GroupBoxPanel</c> with its <c>GroupBoxPanelContent</c>.
/// The element has the <c>checkable</c>, <c>checked</c>, <c>collapsible</c>, <c>expanded</c> and <c>disabled</c>
/// attributes.
/// </summary>
public sealed class GroupBox : ContentControl
{
    private readonly ControllableState<bool> isChecked = new(true);
    private readonly ControllableState<bool> expanded = new(true);
    private readonly string headerId = UniqueId.Create("groupbox-header");
    private readonly string panelId = UniqueId.Create("groupbox-panel");
    private ElementReference toggle;

    /// <summary>Gets or sets the title as plain text.</summary>
    [Parameter] public string? Header { get; set; }

    /// <summary>Gets or sets the title as content; it replaces <see cref="Header" />.</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }

    /// <summary>Gets or sets whether the header is a check box that enables and disables the content.</summary>
    [Parameter] public bool IsCheckable { get; set; }

    /// <summary>Gets or sets whether the header check box is checked; true by default.</summary>
    [Parameter] public bool IsChecked { get; set; } = true;

    /// <summary>Gets or sets the callback invoked when the user checks or clears the header check box.</summary>
    [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

    /// <summary>Gets or sets whether a header button shows and hides the content.</summary>
    [Parameter] public bool IsCollapsible { get; set; }

    /// <summary>Gets or sets whether the content of a collapsible group is shown; true by default.</summary>
    [Parameter] public bool IsExpanded { get; set; } = true;

    /// <summary>Gets or sets the callback invoked when the user shows or hides the content.</summary>
    [Parameter] public EventCallback<bool> IsExpandedChanged { get; set; }

    /// <summary>Gets or sets whether the group and everything inside reject interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets whether the header check box is currently checked.</summary>
    public bool Checked => isChecked.Value;

    /// <summary>Gets whether the content is currently shown.</summary>
    public bool Expanded => !IsCollapsible || expanded.Value;

    /// <summary>Gets whether the content currently rejects interaction.</summary>
    public bool IsContentDisabled => Disabled || (IsCheckable && !isChecked.Value);

    /// <summary>Gets the NSS element name emitted by GroupBox.</summary>
    protected override string ElementName => "GroupBox";

    /// <summary>Synchronizes the checked and expanded states.</summary>
    protected override void OnParametersSet()
    {
        isChecked.Sync(IsChecked);
        expanded.Sync(IsExpanded);
    }

    /// <summary>Adds the group role, the name and the states.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        if (AccessibleName is null && LabeledBy is null) builder.AddAttribute(11, "labeled-by", headerId);
        builder.AddAttribute(12, "checkable", IsCheckable);
        builder.AddAttribute(13, "checked", IsCheckable && isChecked.Value);
        builder.AddAttribute(14, "collapsible", IsCollapsible);
        builder.AddAttribute(15, "expanded", Expanded);
        builder.AddAttribute(16, "disabled", Disabled);
    }

    /// <summary>Adds the header and the panel.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "GroupBoxHeader");
        builder.AddAttribute(1001, "id", headerId);
        if (IsCollapsible)
        {
            builder.OpenElement(1010, "GroupBoxToggle");
            builder.AddAttribute(1011, "role", "button");
            builder.AddAttribute(1012, "expanded", Expanded);
            builder.AddAttribute(1013, "controls", panelId);
            builder.AddAttribute(1014, "focusable", !Disabled);
            builder.AddAttribute(1015, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleExpandedAsync));
            if (IsCheckable) builder.AddAttribute(1016, "accessible-name", ControlStrings.Get(Expanded ? "Collapse" : "Expand"));
            builder.OpenElement(1017, "GroupBoxChevron");
            builder.AddAttribute(1018, "glyph", Expanded ? "chevron-down" : "chevron-right");
            builder.AddAttribute(1019, "semantic-hidden", true);
            builder.CloseElement();
            builder.AddElementReferenceCapture(1020, reference => toggle = reference);
            if (!IsCheckable) AddTitle(builder);
            builder.CloseElement();
        }
        if (IsCheckable)
        {
            builder.OpenComponent<CheckBox>(1030);
            builder.AddComponentParameter(1031, nameof(CheckBox.IsChecked), isChecked.Value);
            builder.AddComponentParameter(1032, nameof(CheckBox.IsCheckedChanged), EventCallback.Factory.Create<bool>(this, CheckedAsync));
            builder.AddComponentParameter(1033, nameof(CheckBox.Disabled), Disabled);
            builder.AddComponentParameter(1034, nameof(ContentControl.ChildContent), (RenderFragment)AddTitleText);
            builder.CloseComponent();
        }
        else if (!IsCollapsible) AddTitle(builder);
        builder.CloseElement();

        builder.OpenComponent<Collapsible>(1100);
        builder.AddComponentParameter(1101, nameof(Collapsible.As), "GroupBoxPanel");
        builder.AddComponentParameter(1102, nameof(Collapsible.IsOpen), Expanded);
        builder.AddComponentParameter(1103, nameof(Control.Id), panelId);
        builder.AddComponentParameter(1104, nameof(Collapsible.Disabled), IsContentDisabled);
        builder.AddComponentParameter(1105, nameof(ContentControl.ChildContent), ChildContent);
        builder.AddComponentParameter(1106, nameof(Collapsible.ReturnFocus), toggle);
        builder.CloseComponent();
    }

    private void AddTitle(RenderTreeBuilder builder)
    {
        builder.OpenElement(1050, "GroupBoxTitle");
        builder.AddContent(1051, (RenderFragment)AddTitleText);
        builder.CloseElement();
    }

    private void AddTitleText(RenderTreeBuilder builder)
    {
        if (HeaderContent is not null) builder.AddContent(0, HeaderContent);
        else builder.AddContent(1, Header);
    }

    private async Task CheckedAsync(bool value)
    {
        if (!isChecked.Set(value)) return;
        StateHasChanged();
        await IsCheckedChanged.InvokeAsync(value);
    }

    private Task ToggleExpandedAsync() => SetExpandedAsync(!expanded.Value);

    /// <summary>Shows or hides the content of a collapsible group as the user does and reports the change.</summary>
    /// <param name="value">Whether the content should be shown.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SetExpandedAsync(bool value)
    {
        if (!IsCollapsible || Disabled || Element.Element?.IsDisabled is true || !expanded.Set(value)) return;
        StateHasChanged();
        await IsExpandedChanged.InvokeAsync(value);
        await Events.RaiseChangeAsync(Element, value);
    }
}
