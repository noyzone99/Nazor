using Nazor.Input;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Controls;

/// <summary>Directive event attributes available on Nazor controls, for example <c>@onclick</c>.</summary>
[EventHandler("onclick", typeof(PointerEventArgs))]
[EventHandler("ondblclick", typeof(PointerEventArgs))]
[EventHandler("oncontextmenu", typeof(PointerEventArgs))]
[EventHandler("onpointerdown", typeof(PointerEventArgs))]
[EventHandler("onpointerup", typeof(PointerEventArgs))]
[EventHandler("onpointermove", typeof(PointerEventArgs))]
[EventHandler("onpointerenter", typeof(PointerEventArgs))]
[EventHandler("onpointerleave", typeof(PointerEventArgs))]
[EventHandler("onlostpointercapture", typeof(PointerEventArgs))]
[EventHandler("onwheel", typeof(WheelEventArgs))]
[EventHandler("onkeydown", typeof(KeyboardEventArgs))]
[EventHandler("onkeyup", typeof(KeyboardEventArgs))]
[EventHandler("onfocus", typeof(FocusEventArgs))]
[EventHandler("onblur", typeof(FocusEventArgs))]
[EventHandler("onfocusin", typeof(FocusEventArgs))]
[EventHandler("onfocusout", typeof(FocusEventArgs))]
[EventHandler("oninput", typeof(ChangeEventArgs))]
[EventHandler("onchange", typeof(ChangeEventArgs))]
[EventHandler("onload", typeof(ImageLoadEventArgs))]
[EventHandler("onerror", typeof(ImageErrorEventArgs))]
[EventHandler("ondismiss", typeof(EventArgs))]
[EventHandler("onscroll", typeof(ScrollEventArgs))]
[EventHandler("onresize", typeof(ResizeEventArgs))]
public static class EventHandlers;

/// <summary>
/// Base class of the built-in controls. A control is a component that renders exactly one element named after the
/// control; styling, layout, input and accessibility work on that element.
/// </summary>
public abstract class Control : ComponentBase
{
    /// <summary>Gets or sets the space-separated NSS classes applied to the element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Gets or sets explicitly published, case-sensitive NSS states for this control's physical root. Custom states do not change accessibility or input behavior.</summary>
    [Parameter] public CustomStateSet? CustomStates { get; set; }

    /// <summary>Gets or sets explicit public part bindings on this physical root. Exposing ownership is checked against the logical component tree.</summary>
    [Parameter] public StylePartSet? Parts { get; set; }

    /// <summary>Gets or sets an explicit logical collection position for custom or recycled roots; null clears it.</summary>
    [Parameter] public LogicalItemPosition? ItemPosition { get; set; }

    /// <summary>Gets or sets the element identifier used by NSS selectors and control references.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Gets or sets the keyboard tab order; null uses the control's default focus behavior.</summary>
    [Parameter] public int? TabIndex { get; set; }

    /// <summary>Gets or sets unmatched markup attributes forwarded to the rendered element.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object?>? AdditionalAttributes { get; set; }

    /// <summary>Gets the retained element associated with this control or element reference.</summary>
    public ElementReference Element { get; protected set; }

    /// <summary>Requests keyboard focus for the target element, optionally preserving scroll offsets.</summary>
    /// <param name="preventScroll">Whether focusing should preserve the current scroll position.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task FocusAsync(bool preventScroll = false) => Element.FocusAsync(preventScroll);

    /// <summary>Captures pointer input for the target element until capture is released or canceled.</summary>
    /// <returns>true if pointer capture was acquired; otherwise, false.</returns>
    public bool CapturePointer() => Element.CapturePointer();

    /// <summary>Releases pointer capture held by the target element.</summary>
    public void ReleasePointerCapture() => Element.ReleasePointerCapture();

    /// <summary>Name exposed to assistive technologies when the visible content is not enough.</summary>
    [Parameter] public string? AccessibleName { get; set; }

    /// <summary>
    /// A longer description for assistive technologies, read after the name (UI Automation's full description and
    /// help).
    /// </summary>
    [Parameter] public string? AccessibleDescription { get; set; }

    /// <summary>
    /// The <c>Id</c> of the control whose text names this one for assistive technologies, as the label before a
    /// field: <c>&lt;TextBlock Id="name"&gt;Name&lt;/TextBlock&gt; &lt;TextField LabeledBy="name" /&gt;</c>.
    /// </summary>
    [Parameter] public string? LabeledBy { get; set; }

    /// <summary>Gets or sets space-separated identifiers of elements describing this control.</summary>
    [Parameter] public string? DescribedBy { get; set; }

    /// <summary>
    /// A short text shown below the control when the pointer rests on it or the keyboard focus reaches it.
    /// </summary>
    [Parameter] public string? Tooltip { get; set; }

    /// <summary>
    /// NSS declarations for this control only, applied after every rule as the style attribute of HTML: <c>Style="@($"width:
    /// {percent}%")"</c>. Values are read at runtime.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Element name, matched by NSS type selectors.</summary>
    protected abstract string ElementName { get; }

    /// <summary>
    /// The style attribute of the element: <see cref="Style" />, to which a control can add its own declarations.
    /// </summary>
    protected virtual string? ElementStyle => Style;

    /// <summary>Gets the tab index of the root. Composite controls can place the tab stop on a child.</summary>
    protected virtual int? ElementTabIndex => TabIndex;

    /// <summary>Writes the render frames for Control to the supplied builder.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, ElementName, this);
        builder.AddAttribute(1, "class", Class);
        builder.AddAttribute(2, "id", Id);
        builder.AddAttribute(3, "accessible-name", AccessibleName);
        builder.AddAttribute(4, "tabindex", ElementTabIndex);
        builder.AddAttribute(5, "tooltip", Tooltip);
        builder.AddAttribute(6, "style", ElementStyle);
        builder.AddAttribute(7, "accessible-description", AccessibleDescription);
        builder.AddAttribute(8, "labeled-by", LabeledBy);
        builder.AddAttribute(9, "described-by", DescribedBy);
        AddAttributes(builder);
        builder.AddAttribute(897, "item-position", ItemPosition);
        builder.AddAttribute(898, "style-parts", Parts);
        builder.AddAttribute(899, "custom-states", CustomStates);
        builder.AddMultipleAttributes(900, AdditionalAttributes);
        builder.AddElementReferenceCapture(999, reference => Element = reference);
        AddContent(builder);
        builder.CloseElement();
    }

    /// <summary>Adds control-specific element attributes, with sequence numbers from 10 to 999.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected virtual void AddAttributes(RenderTreeBuilder builder)
    {
    }

    /// <summary>Adds the element children, with sequence numbers from 1000.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected virtual void AddContent(RenderTreeBuilder builder)
    {
    }
}

/// <summary>A control whose children come from the markup between its tags.</summary>
public abstract class ContentControl : Control
{
    /// <summary>Gets or sets the render fragment supplied between the component's opening and closing tags.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Adds the child render frames used by ContentControl.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder) => builder.AddContent(1000, ChildContent);
}
