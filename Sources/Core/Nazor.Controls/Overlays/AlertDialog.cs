using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A modal decision, such as "Delete 3 files?": the <c>alertdialog</c> role, an optional <see cref="Severity" /> mark, the
/// <see cref="Title" /> and the <see cref="Message" /> (or child content), optional <see cref="Details" /> behind a
/// Show details toggle, an optional "don't ask again" check box (<see cref="IsSuppressed" />) and the
/// <see cref="Actions" />, usually <see cref="DialogButton" /> with roles. A press outside never dismisses it; Escape runs
/// the Cancel action and does nothing when there is none. With <see cref="PreferCancel" /> the Cancel action receives the
/// focus, as destructive questions should. Parts: <c>AlertDialogScrim</c>, <c>AlertDialog</c>,
/// <c>AlertDialogSeverity</c> (<c>severity</c>), <c>AlertDialogContent</c>, <c>AlertDialogTitle</c>,
/// <c>AlertDialogMessage</c>, <c>AlertDialogDetailsToggle</c>, <c>AlertDialogDetailsToggleChevron</c>,
/// <c>AlertDialogDetails</c> (a collapsible with <c>AlertDialogDetailsContent</c>), <c>AlertDialogSuppress</c> and
/// <c>AlertDialogFooter</c>.
/// </summary>
public sealed class AlertDialog : DialogBase
{
    private readonly ControllableState<bool> suppressed = new(false);
    private bool detailsOpen;
    private ElementReference footer;

    /// <summary>Gets or sets the importance shown by the severity mark, or null for no mark.</summary>
    [Parameter] public Severity? Severity { get; set; }

    /// <summary>Gets or sets the title, which names the dialog.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets the message, which describes the dialog.</summary>
    [Parameter] public string? Message { get; set; }

    /// <summary>Gets or sets rich message content, shown after <see cref="Message" />.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets technical details shown behind a toggle.</summary>
    [Parameter] public string? Details { get; set; }

    /// <summary>Gets or sets rich details content, used instead of <see cref="Details" />.</summary>
    [Parameter] public RenderFragment? DetailsContent { get; set; }

    /// <summary>Gets or sets whether the "don't ask again" box is checked.</summary>
    [Parameter] public bool IsSuppressed { get; set; }

    /// <summary>Gets or sets the callback invoked when the user checks or clears the "don't ask again" box.</summary>
    [Parameter] public EventCallback<bool> IsSuppressedChanged { get; set; }

    /// <summary>Gets or sets the text of the "don't ask again" box; the box is shown when this is set or the box is bound.</summary>
    [Parameter] public string? SuppressText { get; set; }

    /// <summary>Gets or sets the actions, usually <see cref="DialogButton" /> with roles.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>Gets or sets whether the Cancel action receives the focus on open.</summary>
    [Parameter] public bool PreferCancel { get; set; }

    /// <summary>Gets whether the "don't ask again" box is currently checked.</summary>
    public bool Suppressed => suppressed.Value;

    /// <summary>Gets the NSS element name emitted by AlertDialog.</summary>
    protected override string ElementName => "AlertDialog";

    /// <summary>Gets the alert dialog role.</summary>
    protected override string Role => "alertdialog";

    /// <summary>Gets whether the Cancel action receives the focus.</summary>
    protected override bool FocusCancel => PreferCancel;

    /// <summary>Gets false: an alert dialog closes only through its actions.</summary>
    protected override bool DismissWithoutCancel => false;

    /// <summary>Gets whether the dialog has a title part.</summary>
    protected override bool HasTitle => Title is not null;

    /// <summary>Gets whether the dialog has a message part.</summary>
    protected override bool HasDescription => Message is not null || ChildContent is not null;

    private bool ShowSuppress => SuppressText is not null || IsSuppressedChanged.HasDelegate;

    /// <summary>Validates the severity and synchronizes the "don't ask again" state.</summary>
    protected override void Validate()
    {
        if (Severity is { } severity && !Enum.IsDefined(severity)) throw new ArgumentOutOfRangeException(nameof(Severity));
        suppressed.Sync(IsSuppressed);
    }

    /// <summary>Collapses the details when the dialog opens again.</summary>
    /// <param name="value">The new open state.</param>
    protected override void OnOpenChanged(bool value) => detailsOpen = false;

    /// <summary>Focuses the default action, then the first action, unless the Cancel action is preferred.</summary>
    /// <returns>The element to focus, or null to let the container choose.</returns>
    protected override ElementReference? DefaultInitialFocus()
    {
        if (PreferCancel && HasCancel) return null;
        if (footer.Element is { IsRemoved: false } element &&
            ControlTree.Descendants(element).FirstOrDefault(e => e.Attributes.GetValueOrDefault("default") is true && ControlTree.IsFocusable(e)) is { } primary)
            return new ElementReference(primary);
        return FirstFocusable(footer);
    }

    /// <summary>Renders the severity mark, the texts, the details, the "don't ask again" box and the actions.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildContent(RenderTreeBuilder builder)
    {
        if (Severity is { } severity)
        {
            builder.OpenElement(0, "AlertDialogSeverity");
            builder.AddAttribute(1, "severity", Vocabulary.Name(severity));
            builder.AddAttribute(2, "role", "image");
            builder.AddAttribute(3, "accessible-name", ControlStrings.Get(severity switch
            {
                Controls.Severity.Success => "Success", Controls.Severity.Warning => "Warning", Controls.Severity.Error => "Error", _ => "Information",
            }));
            builder.AddAttribute(4, "glyph", severity == Controls.Severity.Success ? "check" : null);
            if (severity != Controls.Severity.Success) builder.AddContent(5, severity switch { Controls.Severity.Warning => "!", Controls.Severity.Error => "×", _ => "i" });
            builder.CloseElement();
        }
        builder.OpenElement(10, "AlertDialogContent");
        if (Title is not null)
        {
            builder.OpenElement(11, "AlertDialogTitle");
            builder.AddAttribute(12, "id", TitleId);
            builder.AddAttribute(13, "role", "heading");
            builder.AddAttribute(14, "level", 2);
            builder.AddContent(15, Title);
            builder.CloseElement();
        }
        if (HasDescription)
        {
            builder.OpenElement(20, "AlertDialogMessage");
            builder.AddAttribute(21, "id", DescriptionId);
            builder.AddContent(22, Message);
            builder.AddContent(23, ChildContent);
            builder.CloseElement();
        }
        if (Details is not null || DetailsContent is not null) BuildDetails(builder);
        if (ShowSuppress)
        {
            builder.OpenElement(50, "AlertDialogSuppress");
            builder.OpenComponent<CheckBox>(51);
            builder.AddComponentParameter(52, nameof(CheckBox.IsChecked), suppressed.Value);
            builder.AddComponentParameter(53, nameof(CheckBox.IsCheckedChanged), EventCallback.Factory.Create<bool>(this, SuppressAsync));
            builder.AddComponentParameter(54, nameof(CheckBox.ChildContent), (RenderFragment)(label => label.AddContent(0, SuppressText ?? ControlStrings.Get("DontAskAgain"))));
            builder.CloseComponent();
            builder.CloseElement();
        }
        builder.CloseElement();
        builder.OpenElement(60, "AlertDialogFooter");
        builder.AddElementReferenceCapture(61, reference => footer = reference);
        builder.AddContent(62, Actions);
        builder.CloseElement();
    }

    private void BuildDetails(RenderTreeBuilder builder)
    {
        builder.OpenElement(30, "AlertDialogDetailsToggle");
        builder.AddAttribute(31, "role", "button");
        builder.AddAttribute(32, "focusable", true);
        builder.AddAttribute(33, "expanded", detailsOpen);
        builder.AddAttribute(34, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => detailsOpen = !detailsOpen));
        builder.OpenElement(35, "AlertDialogDetailsToggleChevron");
        builder.AddAttribute(36, "glyph", detailsOpen ? "chevron-down" : "chevron-right");
        builder.AddAttribute(37, "semantic-hidden", true);
        builder.CloseElement();
        builder.AddContent(38, ControlStrings.Get(detailsOpen ? "HideDetails" : "ShowDetails"));
        builder.CloseElement();
        builder.OpenComponent<Collapsible>(40);
        builder.AddComponentParameter(41, nameof(Collapsible.As), "AlertDialogDetails");
        builder.AddComponentParameter(42, nameof(Collapsible.IsOpen), detailsOpen);
        builder.AddComponentParameter(43, nameof(Collapsible.ChildContent), DetailsContent ?? (RenderFragment)(text =>
        {
            text.OpenElement(0, "AlertDialogDetailsText");
            text.AddAttribute(1, "role", "text");
            text.AddAttribute(2, "selectable", true);
            text.AddAttribute(3, "focusable", true);
            text.AddAttribute(4, "tabindex", -1);
            text.AddContent(5, Details);
            text.CloseElement();
        }));
        builder.CloseComponent();
    }

    private async Task SuppressAsync(bool value)
    {
        if (!suppressed.Set(value)) return;
        await IsSuppressedChanged.InvokeAsync(value);
    }
}
