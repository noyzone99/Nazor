using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>Where a <see cref="Switch" /> places its label, written as the <c>label-position</c> attribute.</summary>
public enum SwitchLabelPosition
{
    /// <summary>The label follows the switch.</summary>
    After,
    /// <summary>The label precedes the switch, as in a settings list.</summary>
    Before,
}

/// <summary>
/// An on/off setting that applies at once, such as Wi-Fi in a settings page: <see cref="IsOn" /> (<c>@bind-IsOn</c>)
/// flips with a click or Space. Dragging the thumb previews its position and commits the nearest end on release;
/// Escape or losing capture cancels the preview. The element has the <c>switch</c> role, is <c>:checked</c> when
/// on and has the <c>size</c> and <c>label-position</c> attributes. The parts are <c>SwitchTrack</c> holding
/// <c>SwitchOnLabel</c> (<see cref="OnContent" />), <c>SwitchThumbRail</c> containing <c>SwitchThumbPosition</c>
/// and <c>SwitchThumb</c>, and <c>SwitchOffLabel</c>
/// (<see cref="OffContent" />), then <c>SwitchLabel</c> and <c>SwitchDescription</c>.
/// </summary>
public sealed class Switch : ContentControl, IDisposable
{
    private readonly ControllableState<bool> isOn = new(false);
    private ElementReference rail;
    private ElementReference position;
    private Drag? drag;
    private bool suppressPointerClick;

    /// <summary>Gets or sets whether the switch is on.</summary>
    [Parameter] public bool IsOn { get; set; }

    /// <summary>Gets or sets the callback invoked when the user flips the switch.</summary>
    [Parameter] public EventCallback<bool> IsOnChanged { get; set; }

    /// <summary>Gets or sets content drawn inside the track while the switch is on, such as <c>I</c> or a short word.</summary>
    [Parameter] public RenderFragment? OnContent { get; set; }

    /// <summary>Gets or sets content drawn inside the track while the switch is off, such as <c>O</c> or a short word.</summary>
    [Parameter] public RenderFragment? OffContent { get; set; }

    /// <summary>Gets or sets a secondary line shown below the label.</summary>
    [Parameter] public RenderFragment? Description { get; set; }

    /// <summary>Gets or sets where the label is placed relative to the track.</summary>
    [Parameter] public SwitchLabelPosition LabelPosition { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets whether the switch rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets whether the switch is currently on.</summary>
    public bool On => isOn.Value;

    /// <summary>Gets whether the thumb is held with pointer capture, including the movement threshold before previewing.</summary>
    public bool IsDragging => drag is not null && Element.IsPointerCaptured;

    /// <summary>Gets the NSS element name emitted by Switch.</summary>
    protected override string ElementName => "Switch";

    /// <summary>Publishes the visual thumb fraction from zero to one without changing the committed accessible state.</summary>
    protected override string? ElementStyle => Css.Join("--nz-switch-position: " + Css.Number(IsDragging ? drag!.Fraction : On ? 1 : 0), Style);

    private bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Validates the parameters and synchronizes the state.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (!Enum.IsDefined(LabelPosition)) throw new ArgumentOutOfRangeException(nameof(LabelPosition));
        if (isOn.Sync(IsOn) || IsInactive) CancelDrag();
    }

    /// <summary>Adds the role, the states and the activation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "switch");
        builder.AddAttribute(11, "role-description", ControlStrings.Get("Switch"));
        builder.AddAttribute(12, "checked", isOn.Value);
        builder.AddAttribute(13, "disabled", IsInactive);
        builder.AddAttribute(14, "focusable", !IsInactive);
        builder.AddAttribute(15, "size", Vocabulary.Name(Size));
        builder.AddAttribute(16, "label-position", LabelPosition == SwitchLabelPosition.Before ? "before" : "after");
        builder.AddAttribute(17, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ClickAsync));
        if (Id is null && Field is { } field) builder.AddAttribute(18, "id", field.ControlId);
        builder.AddAttribute(19, "required", Field?.Required == true);
        builder.AddAttribute(20, "invalid", Field?.Invalid == true);
        Field?.ApplyTo(builder, 21, LabeledBy, DescribedBy);
        builder.AddAttribute(30, "dragging", IsDragging && drag!.Moved);
        builder.AddAttribute(31, "drag-on", IsDragging && drag!.Fraction >= .5);
        builder.AddAttribute(32, "__internal_onpointerdown#switch", EventCallback.Factory.Create<PointerEventArgs>(this, PointerDown));
        builder.AddAttribute(33, "__internal_onpointermove#switch", EventCallback.Factory.Create<PointerEventArgs>(this, PointerMove));
        builder.AddAttribute(34, "__internal_onpointerup#switch", EventCallback.Factory.Create<PointerEventArgs>(this, PointerUpAsync));
        builder.AddAttribute(35, "__internal_onpointerleave#switch", EventCallback.Factory.Create<PointerEventArgs>(this, () => { if (!Element.IsPointerCaptured) CancelDrag(); }));
        builder.AddAttribute(36, "__internal_onkeydown#switch", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDown));
        builder.AddAttribute(37, "__internal_onlostpointercapture#switch", EventCallback.Factory.Create<PointerEventArgs>(this, e => { if (e.OriginalTarget == Element.Element) CancelDrag(); }));
    }

    /// <summary>Adds the track with its thumb and state labels, the label and the description.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "SwitchTrack");
        if (OnContent is not null)
        {
            builder.OpenElement(1001, "SwitchOnLabel");
            builder.AddAttribute(1002, "semantic-hidden", true);
            builder.AddContent(1003, OnContent);
            builder.CloseElement();
        }
        builder.OpenElement(1004, "SwitchThumbRail");
        builder.AddElementReferenceCapture(1005, reference => rail = reference);
        builder.OpenElement(1006, "SwitchThumbPosition");
        builder.AddElementReferenceCapture(1007, reference => position = reference);
        builder.OpenElement(1008, "SwitchThumb");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
        if (OffContent is not null)
        {
            builder.OpenElement(1009, "SwitchOffLabel");
            builder.AddAttribute(1010, "semantic-hidden", true);
            builder.AddContent(1011, OffContent);
            builder.CloseElement();
        }
        builder.CloseElement();
        if (ChildContent is not null)
        {
            builder.OpenElement(1020, "SwitchLabel");
            builder.AddContent(1021, ChildContent);
            builder.CloseElement();
        }
        if (Description is not null)
        {
            builder.OpenElement(1030, "SwitchDescription");
            builder.AddContent(1031, Description);
            builder.CloseElement();
        }
    }

    /// <summary>Flips the switch as a user activation does and reports the change.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task ToggleAsync()
    {
        if (IsInactive || Element.Element?.IsDisabled is true) return;
        CancelDrag();
        await SetAsync(!On);
    }

    private async Task SetAsync(bool value)
    {
        if (!isOn.Set(value)) return;
        StateHasChanged();
        await IsOnChanged.InvokeAsync(value);
        await Events.RaiseChangeAsync(Element, value);
    }

    private Task ClickAsync(PointerEventArgs e)
    {
        if (!e.IsSynthetic && suppressPointerClick)
        {
            suppressPointerClick = false;
            return Task.CompletedTask;
        }
        return ToggleAsync();
    }

    private void PointerDown(PointerEventArgs e)
    {
        if (e.Button != PointerButton.Primary) return;
        CancelDrag();
        suppressPointerClick = false;
        if (IsInactive || Element.Element?.IsDisabled is true || e.Modifiers != KeyModifiers.None || !double.IsFinite(e.X) || !double.IsFinite(e.Y)) return;
        var thumb = false;
        for (var node = e.OriginalTarget; node is not null && node != Element.Element; node = node.ParentElement)
            if (node.Name is "SwitchThumb" or "SwitchThumbPosition") { thumb = true; break; }
        if (!thumb || rail.GetBounds() is not { } railBounds || position.GetBounds() is not { } thumbBounds) return;
        var distance = railBounds.Width - thumbBounds.Width;
        if (distance <= 0 || !Element.CapturePointer()) return;
        drag = new(e.X, e.Y, Math.Clamp((thumbBounds.X - railBounds.X) / distance, 0, 1));
        StateHasChanged();
    }

    private void PointerMove(PointerEventArgs e)
    {
        if (drag is not { } active) return;
        if (IsInactive || Element.Element?.IsDisabled is true || !Element.IsPointerCaptured) { CancelDrag(); return; }
        if (!double.IsFinite(e.X) || !double.IsFinite(e.Y)) return;
        if (rail.GetBounds() is not { } railBounds || position.GetBounds() is not { } thumbBounds || railBounds.Width <= thumbBounds.Width) { CancelDrag(); return; }
        var dx = e.X - active.StartX;
        var dy = e.Y - active.StartY;
        if (!active.Moved)
        {
            if (Math.Abs(dx) < 4 && Math.Abs(dy) < 4) return;
            if (Math.Abs(dy) > Math.Abs(dx)) { CancelDrag(); return; }
            active.Moved = true;
        }
        active.Fraction = Math.Clamp(active.StartFraction + dx / (railBounds.Width - thumbBounds.Width), 0, 1);
        StateHasChanged();
    }

    private async Task PointerUpAsync(PointerEventArgs e)
    {
        if (e.Button != PointerButton.Primary || drag is not { } active) return;
        if (!Element.IsPointerCaptured || IsInactive || Element.Element?.IsDisabled is true) { CancelDrag(); return; }
        PointerMove(e);
        if (drag is null) return;
        if (!active.Moved)
        {
            var bounds = Element.GetBounds();
            var within = bounds is { } box && e.X >= box.X && e.X <= box.X + box.Width && e.Y >= box.Y && e.Y <= box.Y + box.Height;
            CancelDrag(suppressClick: !within);
            return;
        }
        var next = active.Fraction >= .5;
        CancelDrag();
        await SetAsync(next);
    }

    private void KeyDown(KeyboardEventArgs e)
    {
        if (e.Handled || e.Key != "Escape" || drag is null) return;
        e.Handled = true;
        CancelDrag();
    }

    private void CancelDrag(bool suppressClick = true)
    {
        if (drag is null) return;
        drag = null;
        suppressPointerClick |= suppressClick;
        if (Element.IsPointerCaptured) Element.ReleasePointerCapture();
        StateHasChanged();
    }

    /// <summary>Releases capture when the control is removed.</summary>
    public void Dispose()
    {
        drag = null;
        if (Element.IsPointerCaptured) Element.ReleasePointerCapture();
    }

    private sealed class Drag(double startX, double startY, double fraction)
    {
        public double StartX { get; } = startX;
        public double StartY { get; } = startY;
        public double StartFraction { get; } = fraction;
        public double Fraction { get; set; } = fraction;
        public bool Moved { get; set; }
    }
}
