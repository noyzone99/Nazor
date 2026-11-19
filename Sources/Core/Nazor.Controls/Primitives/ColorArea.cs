using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>A component of a color edited by a <see cref="ColorSlider" />, written as its <c>channel</c> attribute.</summary>
public enum ColorChannel
{
    /// <summary>The hue, from 0 to 360 degrees.</summary>
    Hue,
    /// <summary>The HSV saturation, from 0 to 100 percent.</summary>
    Saturation,
    /// <summary>The HSV value (brightness), from 0 to 100 percent.</summary>
    Value,
    /// <summary>The HSL lightness, from 0 to 100 percent.</summary>
    Lightness,
    /// <summary>The red channel, from 0 to 255.</summary>
    Red,
    /// <summary>The green channel, from 0 to 255.</summary>
    Green,
    /// <summary>The blue channel, from 0 to 255.</summary>
    Blue,
    /// <summary>The opacity, from 0 to 100 percent.</summary>
    Alpha,
}

/// <summary>
/// A two-dimensional rectangular HSV surface without a built-in look. <see cref="Components" /> selects its axes;
/// the default saturation/value plane keeps hue and opacity. A press or drag moves the <c>{name}Thumb</c> part;
/// arrows move along their screen axis by one percent or one hue degree (ten with Shift), Page Up/Down move vertically
/// by ten percent or 30 hue degrees, and Home/End choose horizontal endpoints (vertical with Ctrl). The element
/// (named by <see cref="As" />) has the <c>slider</c> role, exposes its horizontal channel as the range value and both
/// axes in <c>value-text</c>. Hue/saturation planes display full brightness while preserving the actual value. Themes use
/// <c>--nz-color-area-background</c> for the complete gradient layers. Existing saturation/value themes can use the
/// surface from the variables <c>--nz-color-area-hue</c> (the pure hue), <c>--nz-color-area-color</c> (the opaque
/// current color), <c>--nz-color-area-x</c> and <c>--nz-color-area-y</c> (the thumb position as percentages from the
/// left and top edges). Losing pointer capture cancels the drag without <see cref="OnChange" />.
/// </summary>
public sealed class ColorArea : Control
{
    private readonly ControllableState<HsvColor> color = new(new HsvColor(0, 0, 0));
    private bool dragging;
    private bool changed;

    /// <summary>Gets or sets the element name, <c>ColorArea</c> by default.</summary>
    [Parameter] public string As { get; set; } = "ColorArea";

    /// <summary>Gets or sets the two edited HSV axes; SaturationValue preserves the original behavior.</summary>
    [Parameter] public ColorSpectrumComponents Components { get; set; }

    /// <summary>Gets or sets the edited color; the two selected components change while the remaining channel and alpha are preserved.</summary>
    [Parameter] public HsvColor Color { get; set; }

    /// <summary>Gets or sets the callback invoked on every change, while dragging too.</summary>
    [Parameter] public EventCallback<HsvColor> ColorChanged { get; set; }

    /// <summary>Gets or sets the callback invoked with the final color of a drag or a key press.</summary>
    [Parameter] public EventCallback<HsvColor> OnChange { get; set; }

    /// <summary>Gets or sets whether the area rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets the current color.</summary>
    public HsvColor CurrentColor => color.Value;

    /// <summary>Gets whether a pointer drag is in progress.</summary>
    public bool IsDragging => dragging;

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Gets the position and color variables and the caller's declarations.</summary>
    protected override string? ElementStyle
    {
        get
        {
            var c = color.Value;
            var hue = ColorModel.ToCss(ColorModel.FromHsv(new HsvColor(c.H, 1, 1)));
            var opaque = ColorModel.ToCss(ColorModel.FromHsv(DisplayColor(c) with { A = 1 }));
            var (horizontal, vertical, _) = Axes(Components);
            return Css.Join($"--nz-color-area-hue: {hue}; --nz-color-area-color: {opaque}; --nz-color-area-x: {Css.Number(Position(c, horizontal, Components) * 100)}%; "
                + $"--nz-color-area-y: {Css.Number(Position(c, vertical, Components) * 100)}%; --nz-color-area-background: {Background(c, Components)}", Style);
        }
    }

    /// <summary>Synchronizes the color with the parameter.</summary>
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(As)) throw new ArgumentException("ColorArea requires an element name.", nameof(As));
        if (!Enum.IsDefined(Components)) throw new ArgumentOutOfRangeException(nameof(Components));
        color.Sync(Color.Clamp());
        if (Disabled) (dragging, changed) = (false, false);
    }

    /// <summary>Adds the slider semantics, the variables and the pointer and keyboard handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var c = color.Value;
        var (horizontal, vertical, _) = Axes(Components);
        var text = Components == ColorSpectrumComponents.SaturationValue
            ? ControlStrings.Format("ColorAreaValue", Math.Round(c.S * 100), Math.Round(c.V * 100))
            : ControlStrings.Format("ColorAreaAxesValue", ColorSlider.ChannelLabel(horizontal), AxisText(c, horizontal),
                ColorSlider.ChannelLabel(vertical), AxisText(c, vertical));
        builder.AddAttribute(10, "role", "slider");
        builder.AddAttribute(11, "range-min", 0.0);
        builder.AddAttribute(12, "range-max", ColorSlider.RangeOf(horizontal).Maximum);
        builder.AddAttribute(13, "range-value", Math.Round(ColorSlider.Read(c, horizontal), 2));
        builder.AddAttribute(14, "range-step", 1.0);
        builder.AddAttribute(15, "value-text", text);
        if (AccessibleName is null) builder.AddAttribute(16, "accessible-name", ControlStrings.Get("Color"));
        if (AccessibleDescription is null) builder.AddAttribute(17, "accessible-description", text);
        builder.AddAttribute(18, "focusable", !Disabled);
        builder.AddAttribute(19, "disabled", Disabled);
        builder.AddAttribute(20, "dragging", dragging);
        builder.AddAttribute(21, "__internal_onpointerdown#colorarea", EventCallback.Factory.Create<PointerEventArgs>(this, DownAsync));
        builder.AddEventPreventDefaultAttribute(22, "onpointerdown", true);
        builder.AddAttribute(23, "__internal_onpointermove#colorarea", EventCallback.Factory.Create<PointerEventArgs>(this, MoveAsync));
        builder.AddAttribute(24, "__internal_onpointerup#colorarea", EventCallback.Factory.Create<PointerEventArgs>(this, UpAsync));
        builder.AddAttribute(25, "__internal_onkeydown#colorarea", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(26, "__internal_onlostpointercapture#colorarea", EventCallback.Factory.Create<PointerEventArgs>(this,
            e => { if (e.OriginalTarget == Element.Element) CancelDrag(); }));
        builder.AddAttribute(27, "components", Components.ToString().ToLowerInvariant());
        builder.AddAttribute(28, "x-channel", ColorSlider.ChannelName(horizontal));
        builder.AddAttribute(29, "y-channel", ColorSlider.ChannelName(vertical));
    }

    /// <summary>Adds the thumb.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, As + "Thumb");
        builder.AddAttribute(1001, "semantic-hidden", true);
        builder.CloseElement();
    }

    /// <summary>Maps a point to saturation and value within a box, clamping it to the box.</summary>
    /// <param name="x">The horizontal position.</param>
    /// <param name="y">The vertical position.</param>
    /// <param name="bounds">The box of the area.</param>
    /// <returns>The saturation and the value, each from 0 to 1.</returns>
    public static (double Saturation, double Value) MapPoint(double x, double y, (double X, double Y, double Width, double Height) bounds) =>
        (bounds.Width > 0 ? ColorModel.Unit((x - bounds.X) / bounds.Width) : 0, bounds.Height > 0 ? ColorModel.Unit(1 - (y - bounds.Y) / bounds.Height) : 0);

    /// <summary>Maps a point into the selected HSV axes, preserving the remaining channel and opacity.</summary>
    /// <param name="color">The current HSV color.</param>
    /// <param name="components">The horizontal and vertical axes.</param>
    /// <param name="x">The horizontal pointer position.</param>
    /// <param name="y">The vertical pointer position.</param>
    /// <param name="bounds">The area bounds.</param>
    /// <returns>The color with the selected channels clamped to the area.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The component combination is undefined.</exception>
    public static HsvColor MapPoint(HsvColor color, ColorSpectrumComponents components, double x, double y,
        (double X, double Y, double Width, double Height) bounds)
    {
        var (horizontal, vertical, _) = Axes(components);
        var xFraction = bounds.Width > 0 ? ColorModel.Unit((x - bounds.X) / bounds.Width) : Reversed(horizontal, components) ? 1 : 0;
        var yFraction = bounds.Height > 0 ? ColorModel.Unit((y - bounds.Y) / bounds.Height) : Reversed(vertical, components) ? 1 : 0;
        return WritePosition(WritePosition(color.Clamp(), horizontal, components, xFraction), vertical, components, yFraction);
    }

    internal static (ColorChannel Horizontal, ColorChannel Vertical, ColorChannel Remaining) Axes(ColorSpectrumComponents components) => components switch
    {
        ColorSpectrumComponents.SaturationValue => (ColorChannel.Saturation, ColorChannel.Value, ColorChannel.Hue),
        ColorSpectrumComponents.HueSaturation => (ColorChannel.Hue, ColorChannel.Saturation, ColorChannel.Value),
        ColorSpectrumComponents.HueValue => (ColorChannel.Hue, ColorChannel.Value, ColorChannel.Saturation),
        ColorSpectrumComponents.SaturationHue => (ColorChannel.Saturation, ColorChannel.Hue, ColorChannel.Value),
        ColorSpectrumComponents.ValueHue => (ColorChannel.Value, ColorChannel.Hue, ColorChannel.Saturation),
        ColorSpectrumComponents.ValueSaturation => (ColorChannel.Value, ColorChannel.Saturation, ColorChannel.Hue),
        _ => throw new ArgumentOutOfRangeException(nameof(components)),
    };

    private static bool Reversed(ColorChannel channel, ColorSpectrumComponents components) => channel == ColorChannel.Value ||
        channel == ColorChannel.Saturation && components is ColorSpectrumComponents.HueSaturation or ColorSpectrumComponents.SaturationHue;

    private static double Position(HsvColor color, ColorChannel channel, ColorSpectrumComponents components)
    {
        var fraction = ColorSlider.Read(color, channel) / ColorSlider.RangeOf(channel).Maximum;
        return Reversed(channel, components) ? 1 - fraction : fraction;
    }

    private static string AxisText(HsvColor color, ColorChannel channel) =>
        Math.Round(ColorSlider.Read(color, channel)).ToString(System.Globalization.CultureInfo.CurrentCulture) + (channel == ColorChannel.Hue ? "°" : "%");

    private static HsvColor WritePosition(HsvColor color, ColorChannel channel, ColorSpectrumComponents components, double position) =>
        ColorSlider.Write(color, channel, (Reversed(channel, components) ? 1 - position : position) * ColorSlider.RangeOf(channel).Maximum);

    private HsvColor DisplayColor(HsvColor color) => Components is ColorSpectrumComponents.HueSaturation or ColorSpectrumComponents.SaturationHue ? color with { V = 1 } : color;

    private static string Background(HsvColor color, ColorSpectrumComponents components)
    {
        var (horizontal, vertical, remaining) = Axes(components);
        string Direction(ColorChannel channel) => channel == horizontal ? "to right" : "to bottom";
        if (remaining == ColorChannel.Hue)
        {
            return $"linear-gradient({Direction(ColorChannel.Value)}, transparent, #000000), "
                + $"linear-gradient({Direction(ColorChannel.Saturation)}, #ffffff, transparent), {ColorModel.ToCss(ColorModel.FromHsv(new HsvColor(color.H, 1, 1)))}";
        }
        var hue = "linear-gradient(" + Direction(ColorChannel.Hue) + ", "
            + string.Join(", ", Enumerable.Range(0, 7).Select(index => ColorModel.ToCss(ColorModel.FromHsv(new HsvColor(index == 6 ? 0 : index * 60, 1, 1))))) + ")";
        return remaining == ColorChannel.Value
            ? $"linear-gradient({Direction(ColorChannel.Saturation)}, transparent, #ffffff), {hue}"
            : $"linear-gradient({Direction(ColorChannel.Value)}, transparent, #000000), linear-gradient(rgba(255, 255, 255, {Css.Number(1 - color.S)}), rgba(255, 255, 255, {Css.Number(1 - color.S)})), {hue}";
    }

    private async Task DownAsync(PointerEventArgs e)
    {
        if (Disabled || e.Button != PointerButton.Primary || Element.GetBounds() is not { } bounds) return;
        await Element.FocusAsync(preventScroll: true);
        dragging = Element.CapturePointer();
        changed = false;
        await PointAsync(e, bounds, commit: !dragging);
    }

    private Task MoveAsync(PointerEventArgs e) =>
        dragging && Element.GetBounds() is { } bounds ? PointAsync(e, bounds, commit: false) : Task.CompletedTask;

    private async Task UpAsync(PointerEventArgs e)
    {
        if (!dragging) return;
        var shouldCommit = changed;
        dragging = false;
        changed = false;
        Element.ReleasePointerCapture();
        StateHasChanged();
        if (shouldCommit) await CommitAsync();
    }

    private void CancelDrag()
    {
        if (!dragging && !changed) return;
        (dragging, changed) = (false, false);
        StateHasChanged();
    }

    private Task PointAsync(PointerEventArgs e, (double X, double Y, double Width, double Height) bounds, bool commit)
    {
        return SetAsync(MapPoint(color.Value, Components, e.X, e.Y, bounds), commit);
    }

    private async Task SetAsync(HsvColor next, bool commit)
    {
        if (color.Set(next.Clamp()))
        {
            changed = true;
            StateHasChanged();
            await ColorChanged.InvokeAsync(color.Value);
            await Events.RaiseChangeAsync(Element, color.Value, commit: false);
        }
        if (commit && changed) await CommitAsync();
    }

    private async Task CommitAsync()
    {
        changed = false;
        await OnChange.InvokeAsync(color.Value);
        if (Element.Element is { IsRemoved: false, Root: { } root } element)
            await root.Renderer.DispatchEventAsync(element, "onchange", new ChangeEventArgs { Value = color.Value });
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (Disabled || e.Handled) return;
        var shift = (e.Modifiers & KeyModifiers.Shift) != 0;
        var control = (e.Modifiers & KeyModifiers.Control) != 0;
        var c = color.Value;
        var (horizontal, vertical, _) = Axes(Components);
        var channel = e.Key is "ArrowLeft" or "ArrowRight" || e.Key is "Home" or "End" && !control ? horizontal : vertical;
        var step = channel == ColorChannel.Hue ? (shift ? 10 : 1) / 360.0 : shift ? 0.1 : 0.01;
        var largeStep = channel == ColorChannel.Hue ? 30 / 360.0 : 0.1;
        var position = Position(c, channel, Components);
        double? next = e.Key switch
        {
            "ArrowLeft" or "ArrowUp" => position - step,
            "ArrowRight" or "ArrowDown" => position + step,
            "PageUp" => position - largeStep,
            "PageDown" => position + largeStep,
            "Home" => 0,
            "End" => 1,
            _ => null,
        };
        if (next is not { } fraction) return;
        e.Handled = true;
        var value = WritePosition(c, channel, Components, ColorModel.Unit(fraction));
        await SetAsync(new HsvColor(value.H, Math.Round(ColorModel.Unit(value.S), 4), Math.Round(ColorModel.Unit(value.V), 4), value.A), commit: true);
    }
}

/// <summary>
/// A one-dimensional slider over one <see cref="Channel" /> of <see cref="Color" /> (<c>@bind-Color</c>): hue, HSV
/// saturation or value, HSL lightness, red, green, blue or opacity. Pointer and keyboard come from the engine's slider
/// (arrows, Page Up and Page Down, Home and End). The element (named by <see cref="As" />) has the <c>slider</c> role
/// and the <c>channel</c> and <c>orientation</c> attributes; its parts are <c>{name}Track</c> and <c>{name}Thumb</c>.
/// Themes draw the track from the variables <c>--nz-color-slider-gradient</c> (a complete <c>linear-gradient()</c> of
/// the channel along the slider), <c>--nz-color-slider-start</c> and <c>--nz-color-slider-end</c> (the colors at the
/// ends), <c>--nz-color-slider-color</c> (the current color) and <c>--nz-color-slider-position</c> (the thumb position
/// as a percentage along the track).
/// </summary>
public sealed class ColorSlider : Control
{
    private readonly ControllableState<HsvColor> color = new(new HsvColor(0, 1, 1));

    /// <summary>Gets or sets the element name, <c>ColorSlider</c> by default.</summary>
    [Parameter] public string As { get; set; } = "ColorSlider";

    /// <summary>Gets or sets the channel the slider edits.</summary>
    [Parameter] public ColorChannel Channel { get; set; }

    /// <summary>Gets or sets the edited color.</summary>
    [Parameter] public HsvColor Color { get; set; } = new(0, 1, 1);

    /// <summary>Gets or sets the callback invoked on every change, while dragging too.</summary>
    [Parameter] public EventCallback<HsvColor> ColorChanged { get; set; }

    /// <summary>Gets or sets the callback invoked with the final color of a drag or a key press.</summary>
    [Parameter] public EventCallback<HsvColor> OnChange { get; set; }

    /// <summary>Gets or sets the direction of the track; a vertical slider grows upwards.</summary>
    [Parameter] public Orientation Orientation { get; set; }

    /// <summary>Gets or sets whether the slider rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets the current color.</summary>
    public HsvColor CurrentColor => color.Value;

    /// <summary>Gets the element name.</summary>
    protected override string ElementName => As;

    /// <summary>Gets the lower and upper bounds of a channel as shown to the user.</summary>
    /// <param name="channel">The channel.</param>
    /// <returns>The minimum and the maximum.</returns>
    public static (double Minimum, double Maximum) RangeOf(ColorChannel channel) => channel switch
    {
        ColorChannel.Hue => (0, 360),
        ColorChannel.Red or ColorChannel.Green or ColorChannel.Blue => (0, 255),
        _ => (0, 100),
    };

    /// <summary>Reads a channel of a color in the units of <see cref="RangeOf" />.</summary>
    /// <param name="color">The color.</param>
    /// <param name="channel">The channel.</param>
    /// <returns>The channel value.</returns>
    public static double Read(HsvColor color, ColorChannel channel)
    {
        var c = color.Clamp();
        return channel switch
        {
            ColorChannel.Hue => c.H,
            ColorChannel.Saturation => c.S * 100,
            ColorChannel.Value => c.V * 100,
            ColorChannel.Lightness => ColorModel.ToHsl(c).L * 100,
            ColorChannel.Red => ColorModel.FromHsv(c).R,
            ColorChannel.Green => ColorModel.FromHsv(c).G,
            ColorChannel.Blue => ColorModel.FromHsv(c).B,
            _ => c.A * 100,
        };
    }

    /// <summary>
    /// Writes a channel of a color in the units of <see cref="RangeOf" />, clamped; the hue is kept when an RGB change
    /// makes the color gray.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <param name="channel">The channel.</param>
    /// <param name="value">The new channel value.</param>
    /// <returns>The changed color.</returns>
    public static HsvColor Write(HsvColor color, ColorChannel channel, double value)
    {
        var c = color.Clamp();
        var (min, max) = RangeOf(channel);
        value = double.IsFinite(value) ? Math.Clamp(value, min, max) : min;
        switch (channel)
        {
            case ColorChannel.Hue: return c with { H = ColorModel.WrapHue(value >= 360 ? 359.999 : value) };
            case ColorChannel.Saturation: return c with { S = value / 100 };
            case ColorChannel.Value: return c with { V = value / 100 };
            case ColorChannel.Alpha: return c with { A = value / 100 };
            case ColorChannel.Lightness: return ColorModel.ToHsv(ColorModel.ToHsl(c) with { L = value / 100 });
        }
        var rgb = ColorModel.FromHsv(c);
        var b = (byte)Math.Round(value);
        rgb = channel switch { ColorChannel.Red => rgb with { R = b }, ColorChannel.Green => rgb with { G = b }, _ => rgb with { B = b } };
        var next = ColorModel.ToHsv(rgb) with { A = c.A };
        return next.S == 0 || next.V == 0 ? next with { H = c.H, S = next.V == 0 ? c.S : next.S } : next;
    }

    /// <summary>Gets the variables of the track and the caller's declarations.</summary>
    protected override string? ElementStyle
    {
        get
        {
            var c = color.Value;
            var (min, max) = RangeOf(Channel);
            var stops = Stops(c);
            var direction = Orientation == Orientation.Vertical ? "to top" : "to right";
            var gradient = "linear-gradient(" + direction + ", " + string.Join(", ", stops) + ")";
            var position = RangeMath.Fraction(Read(c, Channel), min, max) * 100;
            return Css.Join($"--nz-color-slider-gradient: {gradient}; --nz-color-slider-start: {stops[0]}; --nz-color-slider-end: {stops[^1]}; "
                + $"--nz-color-slider-color: {ColorModel.ToCss(ColorModel.FromHsv(c))}; --nz-color-slider-position: {Css.Number(position)}%", Style);
        }
    }

    /// <summary>Validates the parameters and synchronizes the color.</summary>
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(As)) throw new ArgumentException("ColorSlider requires an element name.", nameof(As));
        if (!Enum.IsDefined(Channel)) throw new ArgumentOutOfRangeException(nameof(Channel));
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        color.Sync(Color.Clamp());
    }

    /// <summary>Adds the slider semantics and the change handling.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var (min, max) = RangeOf(Channel);
        builder.AddAttribute(10, "role", "slider");
        builder.AddAttribute(11, "channel", ChannelName(Channel));
        builder.AddAttribute(12, "orientation", Vocabulary.Name(Orientation));
        builder.AddAttribute(13, "vertical", Orientation == Orientation.Vertical);
        RangeMath.AddRange(builder, 14, Math.Round(Read(color.Value, Channel), 2), min, max);
        builder.AddAttribute(17, "range-step", 1.0);
        builder.AddAttribute(18, "range-large-step", Channel == ColorChannel.Hue ? 30.0 : Channel is ColorChannel.Red or ColorChannel.Green or ColorChannel.Blue ? 16.0 : 10.0);
        if (AccessibleName is null) builder.AddAttribute(19, "accessible-name", ChannelLabel(Channel));
        builder.AddAttribute(20, "focusable", !Disabled);
        builder.AddAttribute(21, "disabled", Disabled);
        builder.AddAttribute(22, "__internal_oninput#colorslider", EventCallback.Factory.Create<ChangeEventArgs>(this, InputAsync));
        builder.AddAttribute(23, "__internal_onchange#colorslider", EventCallback.Factory.Create<ChangeEventArgs>(this, ChangeAsync));
    }

    /// <summary>Adds the track and the thumb.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, As + "Track");
        builder.AddAttribute(1001, "range-track", true);
        builder.OpenElement(1002, As + "Thumb");
        builder.AddAttribute(1003, "semantic-hidden", true);
        builder.CloseElement();
        builder.CloseElement();
    }

    internal static string ChannelName(ColorChannel channel) => channel.ToString().ToLowerInvariant();

    internal static string ChannelLabel(ColorChannel channel) => channel switch
    {
        ColorChannel.Hue => ControlStrings.Get("Hue"),
        ColorChannel.Saturation => ControlStrings.Get("Saturation"),
        ColorChannel.Value => ControlStrings.Get("Brightness"),
        ColorChannel.Lightness => ControlStrings.Get("Lightness"),
        ColorChannel.Red => ControlStrings.Get("Red"),
        ColorChannel.Green => ControlStrings.Get("Green"),
        ColorChannel.Blue => ControlStrings.Get("Blue"),
        _ => ControlStrings.Get("Alpha"),
    };

    private string[] Stops(HsvColor c)
    {
        string Css(HsvColor value) => ColorModel.ToCss(ColorModel.FromHsv(value));
        return Channel switch
        {
            ColorChannel.Hue => [.. Enumerable.Range(0, 7).Select(i => Css(new HsvColor(i == 6 ? 0 : i * 60, 1, 1)))],
            ColorChannel.Lightness => [Css(Write(c, Channel, 0) with { A = 1 }), Css(Write(c, Channel, 50) with { A = 1 }), Css(Write(c, Channel, 100) with { A = 1 })],
            ColorChannel.Alpha => [Css(c with { A = 0 }), Css(c with { A = 1 })],
            _ => [Css(Write(c, Channel, RangeOf(Channel).Minimum) with { A = 1 }), Css(Write(c, Channel, RangeOf(Channel).Maximum) with { A = 1 })],
        };
    }

    private async Task InputAsync(ChangeEventArgs e)
    {
        if (Disabled || e.Value is not double value) return;
        if (!color.Set(Write(color.Value, Channel, value))) return;
        StateHasChanged();
        await ColorChanged.InvokeAsync(color.Value);
    }

    private Task ChangeAsync(ChangeEventArgs e) => Disabled || e.Value is not double ? Task.CompletedTask : OnChange.InvokeAsync(color.Value);
}
