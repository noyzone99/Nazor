using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// What a <see cref="Field" /> tells the control inside it: the identifiers that connect the control to the label, the
/// description and the error, and the required, invalid and disabled states. Controls of this library read it
/// automatically; a custom control can receive it as a cascading parameter and apply <see cref="ApplyTo" />.
/// </summary>
public sealed class FieldContext
{
    internal FieldContext(string id) => Id = id;

    /// <summary>Gets the stable identifier of the field; the parts' identifiers derive from it.</summary>
    public string Id { get; }

    /// <summary>Gets the identifier given to the field's main control when it has none of its own.</summary>
    public string ControlId => Id + "-control";

    /// <summary>Gets the identifier of the label part.</summary>
    public string LabelId => Id + "-label";

    /// <summary>Gets the identifier of the description part.</summary>
    public string DescriptionId => Id + "-description";

    /// <summary>Gets the identifier of the error part.</summary>
    public string ErrorId => Id + "-error";

    /// <summary>Gets whether the field has a label.</summary>
    public bool HasLabel { get; internal set; }

    /// <summary>Gets whether the field has a description.</summary>
    public bool HasDescription { get; internal set; }

    /// <summary>Gets whether the field shows an error.</summary>
    public bool HasError { get; internal set; }

    /// <summary>Gets whether the field requires a value.</summary>
    public bool Required { get; internal set; }

    /// <summary>Gets whether the application considers the value invalid.</summary>
    public bool Invalid { get; internal set; }

    /// <summary>Gets whether the field is disabled.</summary>
    public bool Disabled { get; internal set; }

    /// <summary>Gets the space-separated identifiers of the description and the error, or null.</summary>
    public string? DescribedBy => (HasDescription, HasError) switch
    {
        (true, true) => DescriptionId + " " + ErrorId,
        (true, false) => DescriptionId,
        (false, true) => ErrorId,
        _ => null,
    };

    /// <summary>
    /// Writes the labeling and description relationships on a control's root element, unless the control set its own;
    /// the control combines <see cref="Required" /> and <see cref="Invalid" /> with its own states.
    /// </summary>
    /// <param name="builder">The builder positioned on the control's element.</param>
    /// <param name="sequence">The first of two sequence numbers to use.</param>
    /// <param name="labeledBy">The control's own labeling target, which takes precedence.</param>
    /// <param name="describedBy">The control's own description targets, which take precedence.</param>
    public void ApplyTo(RenderTreeBuilder builder, int sequence, string? labeledBy = null, string? describedBy = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (labeledBy is null && HasLabel) builder.AddAttribute(sequence, "labeled-by", LabelId);
        if (describedBy is null && DescribedBy is { } described) builder.AddAttribute(sequence + 1, "described-by", described);
    }
}

/// <summary>Where a <see cref="Field" /> places its label.</summary>
public enum FieldLayout
{
    /// <summary>The label above the control.</summary>
    Stacked,
    /// <summary>The label beside the control, in a column shared by a <see cref="FormLayout" />.</summary>
    Inline,
}

/// <summary>
/// Gathers a control with its label, description and error, and connects them for assistive technologies: the label
/// names the control, the description and the error describe it, a click on the label activates it. The field owns no
/// validation; set <see cref="Invalid" /> and <see cref="Error" /> from the application. The parts are a
/// <see cref="Controls.Label" /> (with its required mark), <c>FieldControl</c>, <c>FieldDescription</c> and <c>FieldError</c>.
/// </summary>
public sealed class Field : ContentControl
{
    private FieldContext context = new(UniqueId.Create("field"));

    /// <summary>Gets or sets the label content.</summary>
    [Parameter] public RenderFragment? Label { get; set; }

    /// <summary>Gets or sets the label text, used when <see cref="Label" /> is not set.</summary>
    [Parameter] public string? LabelText { get; set; }

    /// <summary>Gets or sets help shown with the control.</summary>
    [Parameter] public RenderFragment? Description { get; set; }

    /// <summary>Gets or sets the error shown while <see cref="Invalid" /> is true.</summary>
    [Parameter] public RenderFragment? Error { get; set; }

    /// <summary>Gets or sets whether a value is required; the label shows a mark.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets or sets whether the application considers the value invalid.</summary>
    [Parameter] public bool Invalid { get; set; }

    /// <summary>Gets or sets whether the field and its control are disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets where the label is placed; null uses the enclosing <see cref="FormLayout" />'s default.</summary>
    [Parameter] public FieldLayout? Layout { get; set; }

    /// <summary>Gets or sets the enclosing form layout, supplied automatically.</summary>
    [CascadingParameter] public FormLayout? Form { get; set; }

    private FieldLayout EffectiveLayout => Layout ?? Form?.DefaultLayout ?? FieldLayout.Stacked;

    /// <summary>Gets the context shared with the control.</summary>
    public FieldContext Context => context;

    /// <summary>Gets the NSS element name emitted by Field.</summary>
    protected override string ElementName => "Field";

    /// <summary>Updates the shared context.</summary>
    protected override void OnParametersSet()
    {
        if (Layout is { } layout && !Enum.IsDefined(layout)) throw new ArgumentOutOfRangeException(nameof(Layout));
        var hasLabel = Label is not null || !string.IsNullOrEmpty(LabelText);
        var hasDescription = Description is not null;
        var hasError = Invalid && Error is not null;
        if ((context.HasLabel, context.HasDescription, context.HasError, context.Required, context.Invalid, context.Disabled) ==
            (hasLabel, hasDescription, hasError, Required, Invalid, Disabled)) return;

        context = new FieldContext(context.Id)
        {
            HasLabel = hasLabel,
            HasDescription = hasDescription,
            HasError = hasError,
            Required = Required,
            Invalid = Invalid,
            Disabled = Disabled,
        };
    }

    /// <summary>Adds the field markers and states.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "field", true);
        builder.AddAttribute(12, "required", Required);
        builder.AddAttribute(13, "invalid", Invalid);
        builder.AddAttribute(14, "disabled", Disabled);
        builder.AddAttribute(15, "layout", EffectiveLayout == FieldLayout.Inline ? "inline" : "stacked");
        if (context.HasLabel) builder.AddAttribute(16, "labeled-by", context.LabelId);
    }

    /// <summary>Adds the label, the control, the description and the error.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (context.HasLabel)
        {
            OpenDelegatedComponent<Label>(builder, 1000);
            builder.AddComponentParameter(1001, nameof(Controls.Label.Id), context.LabelId);
            builder.AddComponentParameter(1003, nameof(Controls.Label.Required), Required);
            builder.AddComponentParameter(1004, nameof(Controls.Label.ChildContent), Label ?? (b => b.AddContent(0, LabelText)));
            builder.CloseComponent();
        }
        builder.OpenElement(1010, "FieldControl");
        builder.OpenComponent<CascadingValue<FieldContext>>(1011);
        builder.AddComponentParameter(1012, "Value", context);
        builder.AddComponentParameter(1014, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
        if (Description is not null) Part(builder, 1020, "FieldDescription", context.DescriptionId, Description);
        if (context.HasError) Part(builder, 1030, "FieldError", context.ErrorId, Error!);
    }

    private static void Part(RenderTreeBuilder builder, int sequence, string name, string id, RenderFragment content)
    {
        builder.OpenElement(sequence, name);
        builder.AddAttribute(sequence + 1, "role", "text");
        builder.AddAttribute(sequence + 2, "field-description", true);
        builder.AddAttribute(sequence + 3, "id", id);
        if (name == "FieldError") builder.AddAttribute(sequence + 4, "live", "polite");
        builder.AddContent(sequence + 5, content);
        builder.CloseElement();
    }
}

/// <summary>
/// The name of a control. With <see cref="For" /> it names the control with that identifier; inside a <see cref="Field" />
/// it names the field's control. A click on the label focuses the control and toggles a check box, switch or radio
/// button, as in HTML; the label adds no Tab stop. <see cref="Required" /> shows a <c>LabelRequiredMark</c> part.
/// </summary>
public sealed class Label : ContentControl
{
    /// <summary>Gets or sets the identifier of the labeled control; null uses the enclosing field's control.</summary>
    [Parameter] public string? For { get; set; }

    /// <summary>Gets or sets whether the label marks a required value.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets the NSS element name emitted by Label.</summary>
    protected override string ElementName => "Label";

    /// <summary>Adds the label marker, its target and the activation forwarding.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "text");
        builder.AddAttribute(11, "label", true);
        builder.AddAttribute(12, "label-for", For);
        builder.AddAttribute(13, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ActivateTargetAsync));
    }

    /// <summary>Adds the text and the required mark.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.AddContent(1000, ChildContent);
        if (!Required) return;
        builder.OpenElement(1001, "LabelRequiredMark");
        builder.AddAttribute(1002, "semantic-hidden", true);
        builder.AddContent(1003, "*");
        builder.CloseElement();
    }

    private async Task ActivateTargetAsync(PointerEventArgs e)
    {
        if (Element.Element is not { } label) return;
        var field = ControlTree.Ancestors(label).FirstOrDefault(node => node.Attributes.GetValueOrDefault("field") is true);
        var target = For is { Length: > 0 } id ? ControlTree.Find(label, id)
            : field is null ? null : ControlTree.Descendants(field).FirstOrDefault(ControlTree.IsFieldControl);
        if (target is null || target.IsDisabled) return;
        await new ElementReference(target).FocusAsync();
        if (target.Attributes.GetValueOrDefault("role") is "checkbox" or "radio" or "switch" or "togglebutton")
            await target.Root!.Renderer.DispatchEventAsync(target, "onclick", new PointerEventArgs { Button = PointerButton.Primary });
    }
}

/// <summary>
/// A submission scope: Enter in a field, or a <see cref="Button" /> with <see cref="ButtonType.Submit" />, calls
/// <see cref="OnSubmit" /> when no default button handles it; a reset button calls <see cref="OnReset" />. The application
/// owns the model, the validation and the reset of the values.
/// </summary>
public sealed class Form : ContentControl
{
    /// <summary>Gets or sets the submission callback.</summary>
    [Parameter] public EventCallback OnSubmit { get; set; }

    /// <summary>Gets or sets the callback requesting the application-owned reset of the values.</summary>
    [Parameter] public EventCallback OnReset { get; set; }

    /// <summary>Gets the NSS element name emitted by Form.</summary>
    protected override string ElementName => "Form";

    /// <summary>Adds the form role and the scoped callbacks.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "form");
        builder.AddAttribute(11, "__form-submit", EventCallback.Factory.Create(this, SubmitAsync));
        builder.AddAttribute(12, "__form-reset", EventCallback.Factory.Create(this, ResetAsync));
    }

    /// <summary>Enables validation feedback for this form, then requests submission. Does not run validation or block the callback.</summary>
    /// <returns>A task that completes when the callback has finished.</returns>
    public Task SubmitAsync()
    {
        if (Element.Element is { } form) ValidationFeedback.Submit(form);
        return OnSubmit.InvokeAsync();
    }

    /// <summary>Clears validation feedback, then requests the application-owned reset of values and validity.</summary>
    /// <returns>A task that completes when the callback has finished.</returns>
    public Task ResetAsync()
    {
        if (Element.Element is { } form) ValidationFeedback.Reset(form);
        return OnReset.InvokeAsync();
    }
}

/// <summary>
/// Lays out a column of <see cref="Field" /> controls. Inline fields share the label column, whose width is
/// <see cref="LabelWidth" /> (the <c>--nz-form-label-width</c> variable); stacked fields span the whole row.
/// </summary>
public sealed class FormLayout : ContentControl
{
    /// <summary>Gets or sets the width of the label column of inline fields, in logical units, or null for the theme's.</summary>
    [Parameter] public double? LabelWidth { get; set; }

    /// <summary>Gets or sets where the fields inside place their labels unless they choose otherwise.</summary>
    [Parameter] public FieldLayout DefaultLayout { get; set; }

    /// <summary>Gets the NSS element name emitted by FormLayout.</summary>
    protected override string ElementName => "FormLayout";

    /// <summary>Gets the label column width variable and the caller's declarations.</summary>
    protected override string? ElementStyle => LabelWidth is { } width ? Css.Join("--nz-form-label-width: " + Css.Px(width), Style) : Style;

    /// <summary>Validates the label width.</summary>
    protected override void OnParametersSet()
    {
        if (LabelWidth is { } width && (!double.IsFinite(width) || width < 0)) throw new ArgumentOutOfRangeException(nameof(LabelWidth));
        if (!Enum.IsDefined(DefaultLayout)) throw new ArgumentOutOfRangeException(nameof(DefaultLayout));
    }

    /// <summary>Adds the default layout of the fields.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) =>
        builder.AddAttribute(10, "layout", DefaultLayout == FieldLayout.Inline ? "inline" : "stacked");

    /// <summary>Supplies the default layout to the fields.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<FormLayout>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "IsFixed", true);
        builder.AddComponentParameter(1003, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}
