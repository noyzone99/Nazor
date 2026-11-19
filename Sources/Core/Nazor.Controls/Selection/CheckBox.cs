using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// An independent yes-or-no choice with a label. <see cref="IsChecked" /> supports <c>@bind-IsChecked</c>;
/// <see cref="IsIndeterminate" /> shows a mixed state, such as a parent of partly checked children, which activation
/// resolves to checked. The parts are <c>CheckBoxMark</c> with persistent <c>CheckBoxCheck</c> and <c>CheckBoxMixed</c>
/// vector glyphs, <c>CheckBoxLabel</c> and <c>CheckBoxDescription</c>. Themes reveal the glyphs with stroke trimming;
/// checked and mixed semantics change immediately while visual transitions can finish or reverse independently.
/// </summary>
public class CheckBox : ContentControl
{
    private readonly ControllableState<bool> isChecked = new(false);
    private readonly ControllableState<bool> indeterminate = new(false);

    /// <summary>Gets or sets whether the box is checked.</summary>
    [Parameter] public bool IsChecked { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the checked state.</summary>
    [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

    /// <summary>Gets or sets whether the box shows the mixed state.</summary>
    [Parameter] public bool IsIndeterminate { get; set; }

    /// <summary>Gets or sets the callback invoked when activation clears the mixed state.</summary>
    [Parameter] public EventCallback<bool> IsIndeterminateChanged { get; set; }

    /// <summary>Gets or sets a secondary line shown below the label.</summary>
    [Parameter] public RenderFragment? Description { get; set; }

    /// <summary>Gets or sets whether the control rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets whether the box is currently checked.</summary>
    public bool Checked => isChecked.Value;

    /// <summary>Gets whether the box currently shows the mixed state.</summary>
    public bool Indeterminate => indeterminate.Value;

    /// <summary>Gets the accessible role; derived controls such as menu items replace it.</summary>
    protected virtual string Role => "checkbox";

    /// <summary>Gets the NSS element name emitted by CheckBox.</summary>
    protected override string ElementName => "CheckBox";

    /// <summary>Gets whether the control rejects interaction, including through its field.</summary>
    protected bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Synchronizes the states with the parameters.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        isChecked.Sync(IsChecked);
        indeterminate.Sync(IsIndeterminate);
    }

    /// <summary>Adds the role, the states and the activation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleAsync));
        builder.AddAttribute(11, "checked", isChecked.Value && !indeterminate.Value);
        builder.AddAttribute(12, "indeterminate", indeterminate.Value);
        builder.AddAttribute(13, "disabled", IsInactive);
        builder.AddAttribute(14, "role", Role);
        builder.AddAttribute(15, "focusable", !IsInactive);
        builder.AddAttribute(16, "size", Vocabulary.Name(Size));
        if (Id is null && Field is { } field) builder.AddAttribute(17, "id", field.ControlId);
        builder.AddAttribute(18, "required", Field?.Required == true);
        builder.AddAttribute(19, "invalid", Field?.Invalid == true);
        Field?.ApplyTo(builder, 20, LabeledBy, DescribedBy);
    }

    /// <summary>Adds the mark, the label and the description.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, ElementName + "Mark");
        builder.AddAttribute(1001, "semantic-hidden", true);
        builder.OpenElement(1002, ElementName + "Check");
        builder.AddAttribute(1003, "glyph", "check");
        builder.CloseElement();
        builder.OpenElement(1004, ElementName + "Mixed");
        builder.AddAttribute(1005, "glyph", "minus");
        builder.CloseElement();
        builder.CloseElement();
        if (ChildContent is not null)
        {
            builder.OpenElement(1010, ElementName + "Label");
            builder.AddContent(1011, ChildContent);
            builder.CloseElement();
        }
        if (Description is not null)
        {
            builder.OpenElement(1020, ElementName + "Description");
            builder.AddContent(1021, Description);
            builder.CloseElement();
        }
    }

    /// <summary>Changes the checked state as a user activation does and reports the change.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public virtual async Task ToggleAsync()
    {
        if (IsInactive || Element.Element?.IsDisabled is true) return;
        var wasMixed = indeterminate.Value;
        var value = wasMixed || !isChecked.Value;
        isChecked.Set(value);
        indeterminate.Set(false);
        StateHasChanged();
        if (wasMixed) await IsIndeterminateChanged.InvokeAsync(false);
        await IsCheckedChanged.InvokeAsync(value);
        await Events.RaiseChangeAsync(Element, value);
    }
}
