using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;
using StyleColor = Nazor.Style.Color;

namespace Nazor.Controls;

/// <summary>The numeric channels displayed by a <see cref="ColorPicker" />.</summary>
public enum ColorRepresentation
{
    /// <summary>Red, green and blue, from zero to 255.</summary>
    Rgb,
    /// <summary>Hue in degrees, saturation and value in percent.</summary>
    Hsv,
}

/// <summary>
/// Edits an RGBA color with a rectangular HSV spectrum, its remaining-channel slider, optional opacity,
/// RGB/HSV channel text, hexadecimal input and a checkerboard preview. <see cref="Color" /> supports
/// <c>@bind-Color</c>. Sliders report previews through <see cref="ColorChanged" /> and completions through
/// <see cref="OnChange" />; channel and hex drafts commit with Enter or focus loss. Invalid drafts keep the color
/// unchanged and are restored on focus loss. Hex uses CSS byte order, <c>#rrggbbaa</c>, including when Fluent is active.
/// Parts are <c>ColorPickerVisuals</c>, <c>ColorPickerSpectrum</c> and its thumb, <c>ColorPickerSlider</c> and
/// <c>ColorPickerAlphaSlider</c> with track/thumb parts, <c>ColorPickerPreview</c> with previous/current swatches,
/// <c>ColorPickerInputs</c>, <c>ColorPickerRepresentation</c>, <c>ColorPickerChannels</c>, <c>ColorPickerChannel</c>
/// and <c>ColorPickerHex</c>. Child fields and the representation select retain their ordinary themed parts.
/// </summary>
public sealed class ColorPicker : Control
{
    private readonly ControllableState<StyleColor> color = new(new(255, 255, 255, 255));
    private readonly ControllableState<ColorRepresentation> representation = new(ColorRepresentation.Rgb);
    private readonly Dictionary<ColorChannel, string> channelText = [];
    private readonly HashSet<ColorChannel> invalidChannels = [];
    private readonly string fieldId = UniqueId.Create("color-picker");
    private HsvColor hsv = new(0, 0, 1);
    private string hexText = "#ffffff";
    private bool hexInvalid;
    private bool initialized;
    private bool shownAlpha;
    private CultureInfo? shownCulture;

    /// <summary>Gets or sets the RGBA color; opaque white by default. The symbolic currentColor value is rejected.</summary>
    [Parameter] public StyleColor Color { get; set; } = new(255, 255, 255, 255);

    /// <summary>Gets or sets the callback invoked when user editing changes the color bytes, including drag previews.</summary>
    [Parameter] public EventCallback<StyleColor> ColorChanged { get; set; }

    /// <summary>Gets or sets the callback invoked when a drag, slider key or valid text edit completes. HSV-only edits may report unchanged color bytes.</summary>
    [Parameter] public EventCallback<StyleColor> OnChange { get; set; }

    /// <summary>Gets or sets the numeric channel representation; RGB by default.</summary>
    [Parameter] public ColorRepresentation Representation { get; set; }

    /// <summary>Gets or sets the callback invoked when the user chooses RGB or HSV.</summary>
    [Parameter] public EventCallback<ColorRepresentation> RepresentationChanged { get; set; }

    /// <summary>Gets or sets the spectrum axes; HueSaturation by default. Its remaining channel determines the color slider.</summary>
    [Parameter] public ColorSpectrumComponents SpectrumComponents { get; set; } = ColorSpectrumComponents.HueSaturation;

    /// <summary>Gets or sets whether visual controls are above the text inputs (Vertical, the default) or beside them (Horizontal).</summary>
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Vertical;

    /// <summary>Gets or sets whether opacity can be edited; false by default. Disabling editing preserves the current alpha channel.</summary>
    [Parameter] public bool IsAlphaEnabled { get; set; }

    /// <summary>Gets or sets whether the spectrum is shown; true by default.</summary>
    [Parameter] public bool IsColorSpectrumVisible { get; set; } = true;

    /// <summary>Gets or sets whether the remaining-channel slider is shown; true by default.</summary>
    [Parameter] public bool IsColorSliderVisible { get; set; } = true;

    /// <summary>Gets or sets whether the opacity slider is shown when alpha editing is enabled; true by default.</summary>
    [Parameter] public bool IsAlphaSliderVisible { get; set; } = true;

    /// <summary>Gets or sets whether the current and optional previous-color preview is shown; true by default.</summary>
    [Parameter] public bool IsColorPreviewVisible { get; set; } = true;

    /// <summary>Gets or sets whether numeric RGB/HSV channel inputs and their format selector are shown; true by default.</summary>
    [Parameter] public bool IsColorChannelTextInputVisible { get; set; } = true;

    /// <summary>Gets or sets whether the opacity text input is shown when alpha editing is enabled; true by default.</summary>
    [Parameter] public bool IsAlphaTextInputVisible { get; set; } = true;

    /// <summary>Gets or sets whether the hexadecimal input is shown; true by default.</summary>
    [Parameter] public bool IsHexInputVisible { get; set; } = true;

    /// <summary>Gets or sets an optional previous color shown beside the current preview; it is not edited.</summary>
    [Parameter] public StyleColor? PreviousColor { get; set; }

    /// <summary>Gets or sets the culture of numeric channel input; the current culture by default. Hexadecimal is culture independent.</summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>Gets or sets whether color editing is blocked while text remains selectable and the representation can change.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets or sets whether all interactions are disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the enclosing field, supplied automatically. Its disabled state and labeling apply to the picker group.</summary>
    [CascadingParameter] public FieldContext? Field { get; set; }

    /// <summary>Gets the current RGBA color, including unbound local edits.</summary>
    public StyleColor CurrentColor => color.Value;

    /// <summary>Gets the current HSV model, retaining hue through gray and hue/saturation through black.</summary>
    public HsvColor CurrentHsvColor => hsv;

    /// <summary>Gets the currently displayed numeric representation.</summary>
    public ColorRepresentation CurrentRepresentation => representation.Value;

    /// <summary>Gets the ColorPicker element name.</summary>
    protected override string ElementName => "ColorPicker";

    /// <summary>Gets no composite-root Tab stop; the spectrum, sliders, selector and text fields own their stops.</summary>
    protected override int? ElementTabIndex => null;

    /// <summary>Gets the current and previous preview colors followed by application styles.</summary>
    protected override string? ElementStyle => Css.Join($"--nz-color-picker-color: {ColorModel.ToCss(color.Value)}; "
        + $"--nz-color-picker-previous: {ColorModel.ToCss(PreviousColor ?? color.Value)}", Style);

    private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;
    private bool IsInactive => Disabled || Field?.Disabled == true;

    /// <summary>Validates symbolic colors and enums, synchronizes externally changed values and refreshes numeric formatting.</summary>
    /// <exception cref="ArgumentException">Color or PreviousColor is the symbolic currentColor value.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A representation, orientation or spectrum combination is undefined.</exception>
    protected override void OnParametersSet()
    {
        if (Color.IsCurrentColor || PreviousColor?.IsCurrentColor is true)
            throw new ArgumentException("ColorPicker requires concrete RGBA colors.", nameof(Color));
        if (!Enum.IsDefined(Representation)) throw new ArgumentOutOfRangeException(nameof(Representation));
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        _ = ColorArea.Axes(SpectrumComponents);
        representation.Sync(Representation);
        var changed = color.Sync(Color);
        if (changed || !initialized) hsv = AdoptRgb(color.Value, hsv);
        if (changed || !initialized || shownAlpha != IsAlphaEnabled || !Equals(shownCulture, EffectiveCulture)) ResetText();
        initialized = true;
        shownAlpha = IsAlphaEnabled;
        shownCulture = EffectiveCulture;
    }

    /// <summary>Adds group semantics, orientation, representation and field labeling.</summary>
    /// <param name="builder">The builder inside the picker element.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "disabled", IsInactive);
        builder.AddAttribute(12, "readonly", ReadOnly);
        builder.AddAttribute(13, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(14, "representation", representation.Value.ToString().ToLowerInvariant());
        builder.AddAttribute(15, "alpha-enabled", IsAlphaEnabled);
        builder.AddAttribute(16, "components", SpectrumComponents.ToString().ToLowerInvariant());
        if (AccessibleName is null && Field?.HasLabel != true) builder.AddAttribute(17, "accessible-name", ControlStrings.Get("Color"));
        Field?.ApplyTo(builder, 18, LabeledBy, DescribedBy);
        builder.AddAttribute(20, "spectrum-visible", IsColorSpectrumVisible);
        builder.AddAttribute(21, "preview-visible", IsColorPreviewVisible);
        builder.AddAttribute(22, "channels-visible", IsColorChannelTextInputVisible);
    }

    /// <summary>Composes the reusable controls, clearing the enclosing field cascade for independently labeled internal inputs.</summary>
    /// <param name="builder">The builder inside the picker element.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<FieldContext?>>(1000);
        builder.AddComponentParameter(1001, nameof(CascadingValue<FieldContext?>.Value), null);
        builder.AddComponentParameter(1002, nameof(CascadingValue<FieldContext?>.IsFixed), true);
        builder.AddComponentParameter(1003, nameof(CascadingValue<FieldContext?>.ChildContent), (RenderFragment)RenderParts);
        builder.CloseComponent();
    }

    private void RenderParts(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "ColorPickerVisuals");
        if (IsColorSpectrumVisible)
        {
            builder.OpenComponent<ColorArea>(1);
            builder.AddComponentParameter(2, nameof(ColorArea.As), "ColorPickerSpectrum");
            builder.AddComponentParameter(3, nameof(ColorArea.Components), SpectrumComponents);
            builder.AddComponentParameter(4, nameof(ColorArea.Color), hsv);
            builder.AddComponentParameter(5, nameof(ColorArea.ColorChanged), EventCallback.Factory.Create<HsvColor>(this, next => SetHsvAsync(next, false)));
            builder.AddComponentParameter(6, nameof(ColorArea.OnChange), EventCallback.Factory.Create<HsvColor>(this, _ => CommitAsync()));
            builder.AddComponentParameter(7, nameof(ColorArea.Disabled), IsInactive || ReadOnly);
            builder.CloseComponent();
        }
        if (IsColorSliderVisible) Slider(builder, 10, "ColorPickerSlider", ColorArea.Axes(SpectrumComponents).Remaining);
        if (IsAlphaEnabled && IsAlphaSliderVisible) Slider(builder, 20, "ColorPickerAlphaSlider", ColorChannel.Alpha);
        if (IsColorPreviewVisible)
        {
            builder.OpenElement(30, "ColorPickerPreview");
            Swatch(builder, 31, "ColorPickerPreviewCurrent", color.Value, "NewColor");
            if (PreviousColor is { } previous) Swatch(builder, 36, "ColorPickerPreviewPrevious", previous, "OriginalColor");
            builder.CloseElement();
        }
        builder.CloseElement();
        builder.OpenElement(40, "ColorPickerInputs");
        if (IsColorChannelTextInputVisible)
        {
            builder.OpenElement(41, "ColorPickerRepresentation");
            builder.OpenComponent<Select<ColorRepresentation>>(42);
            builder.AddComponentParameter(43, nameof(Select<ColorRepresentation>.Items), Enum.GetValues<ColorRepresentation>());
            builder.AddComponentParameter(44, nameof(Select<ColorRepresentation>.Value), representation.Value);
            builder.AddComponentParameter(45, nameof(Select<ColorRepresentation>.ValueChanged), EventCallback.Factory.Create<ColorRepresentation>(this, ChangeRepresentationAsync));
            builder.AddComponentParameter(46, nameof(Select<ColorRepresentation>.ItemText), (Func<ColorRepresentation, string>)(value => value.ToString().ToUpperInvariant()));
            builder.AddComponentParameter(47, nameof(Select<ColorRepresentation>.Disabled), IsInactive);
            builder.AddComponentParameter(48, nameof(Select<ColorRepresentation>.AccessibleName), ControlStrings.Get("ColorFormat"));
            builder.CloseComponent();
            builder.CloseElement();
        }
        if (IsColorChannelTextInputVisible || IsAlphaEnabled && IsAlphaTextInputVisible)
        {
            builder.OpenElement(50, "ColorPickerChannels");
            if (IsColorChannelTextInputVisible)
                foreach (var channel in representation.Value == ColorRepresentation.Rgb
                    ? new[] { ColorChannel.Red, ColorChannel.Green, ColorChannel.Blue }
                    : new[] { ColorChannel.Hue, ColorChannel.Saturation, ColorChannel.Value }) Channel(builder, channel);
            if (IsAlphaEnabled && IsAlphaTextInputVisible) Channel(builder, ColorChannel.Alpha);
            builder.CloseElement();
        }
        if (IsHexInputVisible)
        {
            builder.OpenElement(70, "ColorPickerHex");
            LabelFor(builder, 71, fieldId + "-hex-input", ControlStrings.Get("Hex"));
            builder.OpenComponent<TextField>(75);
            builder.AddComponentParameter(76, nameof(TextField.Id), fieldId + "-hex");
            builder.AddComponentParameter(77, nameof(TextField.Value), hexText);
            builder.AddComponentParameter(78, nameof(TextField.ValueChanged), EventCallback.Factory.Create<string>(this, value =>
            {
                if (IsInactive || ReadOnly) return;
                hexText = value;
                hexInvalid = false;
            }));
            builder.AddComponentParameter(79, nameof(TextField.OnChange), EventCallback.Factory.Create<ChangeEventArgs>(this, CommitHexAsync));
            builder.AddComponentParameter(80, nameof(TextField.Disabled), IsInactive);
            builder.AddComponentParameter(81, nameof(TextField.ReadOnly), ReadOnly);
            builder.AddComponentParameter(82, nameof(TextField.Invalid), hexInvalid);
            builder.AddComponentParameter(83, nameof(TextField.MaxLength), 9);
            builder.AddComponentParameter(84, nameof(TextField.AccessibleName), ControlStrings.Get("Hex"));
            builder.AddComponentParameter(85, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, () => { if (hexInvalid) ResetHex(); }));
            builder.CloseComponent();
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    private void Slider(RenderTreeBuilder builder, int sequence, string name, ColorChannel channel)
    {
        builder.OpenComponent<ColorSlider>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(ColorSlider.As), name);
        builder.AddComponentParameter(sequence + 2, nameof(ColorSlider.Color), hsv);
        builder.AddComponentParameter(sequence + 3, nameof(ColorSlider.Channel), channel);
        builder.AddComponentParameter(sequence + 4, nameof(ColorSlider.ColorChanged), EventCallback.Factory.Create<HsvColor>(this, next => SetHsvAsync(next, false)));
        builder.AddComponentParameter(sequence + 5, nameof(ColorSlider.OnChange), EventCallback.Factory.Create<HsvColor>(this, _ => CommitAsync()));
        builder.AddComponentParameter(sequence + 6, nameof(ColorSlider.Disabled), IsInactive || ReadOnly);
        builder.AddComponentParameter(sequence + 7, nameof(ColorSlider.Orientation), Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal);
        builder.CloseComponent();
    }

    private void Channel(RenderTreeBuilder builder, ColorChannel channel)
    {
        var name = ColorSlider.ChannelName(channel);
        var id = fieldId + "-" + name;
        builder.OpenElement(51, "ColorPickerChannel");
        builder.SetKey(channel);
        builder.AddAttribute(52, "channel", name);
        LabelFor(builder, 53, id + "-input", ColorSlider.ChannelLabel(channel));
        builder.OpenComponent<TextField>(57);
        builder.AddComponentParameter(58, nameof(TextField.Id), id);
        builder.AddComponentParameter(59, nameof(TextField.Value), channelText.GetValueOrDefault(channel, ""));
        builder.AddComponentParameter(60, nameof(TextField.ValueChanged), EventCallback.Factory.Create<string>(this, value =>
        {
            if (IsInactive || ReadOnly) return;
            channelText[channel] = value;
            invalidChannels.Remove(channel);
        }));
        builder.AddComponentParameter(61, nameof(TextField.OnChange), EventCallback.Factory.Create<ChangeEventArgs>(this, _ => CommitChannelAsync(channel)));
        builder.AddComponentParameter(62, nameof(TextField.Disabled), IsInactive);
        builder.AddComponentParameter(63, nameof(TextField.ReadOnly), ReadOnly);
        builder.AddComponentParameter(64, nameof(TextField.Invalid), invalidChannels.Contains(channel));
        builder.AddComponentParameter(65, nameof(TextField.Type), TextFieldType.Number);
        builder.AddComponentParameter(66, nameof(TextField.AccessibleName), ColorSlider.ChannelLabel(channel));
        builder.AddComponentParameter(67, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, () =>
        {
            if (invalidChannels.Remove(channel)) channelText[channel] = FormatChannel(channel);
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }

    private static void LabelFor(RenderTreeBuilder builder, int sequence, string id, string text)
    {
        builder.OpenComponent<Label>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(Label.For), id);
        builder.AddComponentParameter(sequence + 2, nameof(Label.ChildContent), (RenderFragment)(content => content.AddContent(0, text)));
        builder.CloseComponent();
    }

    private static void Swatch(RenderTreeBuilder builder, int sequence, string name, StyleColor value, string label)
    {
        builder.OpenElement(sequence, "ColorPickerSwatch");
        builder.AddAttribute(sequence + 1, "role", "image");
        builder.AddAttribute(sequence + 2, "accessible-name", ControlStrings.Get(label) + " " + ColorModel.ToHex(value));
        builder.OpenElement(sequence + 3, name);
        builder.AddAttribute(sequence + 4, "semantic-hidden", true);
        builder.CloseElement();
        builder.CloseElement();
    }

    private async Task ChangeRepresentationAsync(ColorRepresentation next)
    {
        if (IsInactive || !representation.Set(next)) return;
        await RepresentationChanged.InvokeAsync(next);
    }

    private async Task SetHsvAsync(HsvColor next, bool commit)
    {
        if (IsInactive || ReadOnly) return;
        next = next.Clamp();
        if (!IsAlphaEnabled) next = next with { A = color.Value.A / 255.0 };
        if (next == hsv) { ResetText(); return; }
        hsv = next;
        var changed = color.Set(ColorModel.FromHsv(hsv));
        ResetText();
        StateHasChanged();
        if (changed)
        {
            await ColorChanged.InvokeAsync(color.Value);
            await Events.RaiseChangeAsync(Element, color.Value, commit: false);
        }
        if (commit) await CommitAsync();
    }

    private async Task CommitAsync()
    {
        if (IsInactive || ReadOnly) return;
        await OnChange.InvokeAsync(color.Value);
        if (Element.Element is { IsRemoved: false, Root: { } root } element)
            await root.Renderer.DispatchEventAsync(element, "onchange", new ChangeEventArgs { Value = color.Value });
    }

    private Task CommitChannelAsync(ColorChannel channel)
    {
        if (IsInactive || ReadOnly) return Task.CompletedTask;
        if (!double.TryParse(channelText.GetValueOrDefault(channel), NumberStyles.Float | NumberStyles.AllowThousands, EffectiveCulture, out var value) || !double.IsFinite(value))
        {
            invalidChannels.Add(channel);
            return Task.CompletedTask;
        }
        return SetHsvAsync(ColorSlider.Write(hsv, channel, value), commit: true);
    }

    private Task CommitHexAsync(ChangeEventArgs _)
    {
        if (IsInactive || ReadOnly) return Task.CompletedTask;
        if (!ColorModel.TryParseHex(hexText, out var next))
        {
            hexInvalid = true;
            return Task.CompletedTask;
        }
        if (!IsAlphaEnabled) next = next with { A = color.Value.A };
        return SetHsvAsync(AdoptRgb(next, hsv), commit: true);
    }

    private static HsvColor AdoptRgb(StyleColor value, HsvColor previous)
    {
        var next = ColorModel.ToHsv(value);
        return next.V == 0 ? next with { H = previous.H, S = previous.S } : next.S == 0 ? next with { H = previous.H } : next;
    }

    private string FormatChannel(ColorChannel channel) => ColorSlider.Read(hsv, channel).ToString("0.##", EffectiveCulture);

    private void ResetText()
    {
        foreach (var channel in Enum.GetValues<ColorChannel>()) channelText[channel] = FormatChannel(channel);
        invalidChannels.Clear();
        ResetHex();
    }

    private void ResetHex()
    {
        hexText = ColorModel.ToHex(color.Value, IsAlphaEnabled);
        hexInvalid = false;
    }
}
