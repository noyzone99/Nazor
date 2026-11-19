using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>Configures the action a <see cref="Button" /> requests from its nearest <see cref="Form" />.</summary>
public enum ButtonType
{
    /// <summary>Invokes only the button action.</summary>
    Button,
    /// <summary>Requests submission of the nearest form.</summary>
    Submit,
    /// <summary>Requests the application-owned reset of the nearest form.</summary>
    Reset,
}

/// <summary>
/// The shared look and content of <see cref="Button" /> and <see cref="ToggleButton" />: appearance, intent, size and
/// shape, optional leading and trailing content around the label, a busy state and an optional <see cref="Command" />.
/// The parts are <c>ButtonLeading</c>, <c>ButtonLabel</c>, <c>ButtonTrailing</c> and, while busy, <c>ButtonSpinner</c>;
/// a button without a label has the <c>icon-only</c> attribute. Inside a <see cref="ButtonGroup" /> the button carries
/// the <c>position</c> attribute (<c>first</c>, <c>middle</c>, <c>last</c> or <c>only</c>) and takes the group's
/// appearance and size when it leaves its own at the default.
/// </summary>
public abstract class ButtonBase : ContentControl, IDisposable, IButtonGroupMember
{
    private Command? observed;
    private ButtonGroup? joined;
    private GroupPosition? position;
    private (double X, double Y)? pressOrigin;
    private bool hasPressOrigin;
    private bool pressReplay;

    /// <summary>Gets or sets how strongly the button is filled.</summary>
    [Parameter] public ControlAppearance Appearance { get; set; }

    /// <summary>Gets or sets the meaning of the button's color.</summary>
    [Parameter] public Intent Intent { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets the outline.</summary>
    [Parameter] public Shape Shape { get; set; }

    /// <summary>Gets or sets content shown before the label, usually an <see cref="Icon" />.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets content shown after the label, such as a chevron or a badge.</summary>
    [Parameter] public RenderFragment? Trailing { get; set; }

    /// <summary>Gets or sets whether the button shows work in progress; it then rejects activation.</summary>
    [Parameter] public bool IsBusy { get; set; }

    /// <summary>Gets or sets whether the button rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets a command that supplies the label (when there is no content), the tooltip with its shortcut, the
    /// enabled state and the action.
    /// </summary>
    [Parameter] public Command? Command { get; set; }

    /// <summary>Gets or sets the callback invoked when the button is activated.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnClick { get; set; }

    /// <summary>Gets whether the button currently rejects activation.</summary>
    protected bool IsInactive => Disabled || IsBusy || Command is { IsEnabled: false };

    [CascadingParameter] internal ButtonGroup? Group { get; set; }

    internal ControlAppearance EffectiveAppearance => Appearance == ControlAppearance.Standard && Group is not null ? Group.Appearance : Appearance;

    internal ControlSize EffectiveSize => Size == ControlSize.Medium && Group is not null ? Group.Size : Size;

    /// <summary>Gets whether the button shows a label part: it has content or a command.</summary>
    protected virtual bool HasLabel => ChildContent is not null || Command is not null;

    /// <summary>Publishes the most recent press origin for theme-owned effects, using local logical coordinates for pointer presses and the center for synthetic keyboard/accessibility activation.</summary>
    protected override string? ElementStyle => hasPressOrigin
        ? Css.Join($"--nz-press-x: {(pressOrigin is { } point ? Css.Px(point.X) : "50%")}; --nz-press-y: {(pressOrigin is { } origin ? Css.Px(origin.Y) : "50%")}", Style)
        : Style;

    /// <summary>Validates the parameters and follows the command's changes.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Intent)) throw new ArgumentOutOfRangeException(nameof(Intent));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (!Enum.IsDefined(Shape)) throw new ArgumentOutOfRangeException(nameof(Shape));
        if (!ReferenceEquals(joined, Group))
        {
            joined?.Leave(this);
            joined = Group;
            joined?.Join(this);
            position = null;
        }
        if (ReferenceEquals(observed, Command)) return;
        if (observed is not null) observed.Changed -= Refresh;
        observed = Command;
        if (observed is not null) observed.Changed += Refresh;
    }

    private void Refresh() => InvokeAsync(StateHasChanged);

    /// <summary>Lets the enclosing group place the button once it is rendered.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (joined is not null && (firstRender || position is null)) joined.Arrange();
    }

    ElementNode? IButtonGroupMember.Node => Element.Element;

    void IButtonGroupMember.Place(GroupPosition? value)
    {
        if (position == value) return;
        position = value;
        StateHasChanged();
    }

    /// <summary>Gets the tooltip, falling back to the command's description and shortcut.</summary>
    protected string? EffectiveTooltip => Tooltip ?? (Command is { } command && (command.Description is not null || command.Gesture is not null)
        ? string.Join(" ", new[] { command.Description ?? (ChildContent is null ? null : command.Text), command.Gesture is { } g ? "(" + g.ToDisplayString() + ")" : null }.Where(s => s is not null))
        : null);

    /// <summary>Adds the shared look attributes.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "appearance", Vocabulary.Name(EffectiveAppearance));
        builder.AddAttribute(11, "intent", Vocabulary.Name(Intent));
        builder.AddAttribute(12, "size", Vocabulary.Name(EffectiveSize));
        builder.AddAttribute(13, "shape", Vocabulary.Name(Shape));
        builder.AddAttribute(14, "disabled", Disabled || Command is { IsEnabled: false });
        builder.AddAttribute(15, "busy", IsBusy);
        builder.AddAttribute(16, "focusable", !(Disabled || Command is { IsEnabled: false }));
        builder.AddAttribute(17, "icon-only", !HasLabel && (Leading is not null || Trailing is not null));
        if (Tooltip is null && EffectiveTooltip is { } tip) builder.AddAttribute(18, "tooltip", tip);
        if (AccessibleName is null && ChildContent is null && Command is not null) builder.AddAttribute(19, "accessible-name", Command.Text);
        builder.AddAttribute(20, "position", position is { } place ? Vocabulary.Name(place) : null);
        builder.AddAttribute(21, "press-effect", hasPressOrigin && !IsInactive);
        builder.AddAttribute(22, "press-replay", pressReplay ? "a" : "b");
        builder.AddAttribute(23, "__internal_onpointerdown#press-origin", EventCallback.Factory.Create<PointerEventArgs>(this, PressAt));
    }

    /// <summary>Adds the leading content, the label, the trailing content and the busy spinner.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Leading is not null)
        {
            builder.OpenElement(1000, "ButtonLeading");
            builder.AddContent(1001, Leading);
            builder.CloseElement();
        }
        if (HasLabel)
        {
            builder.OpenElement(1010, "ButtonLabel");
            if (ChildContent is not null) builder.AddContent(1011, ChildContent);
            else builder.AddContent(1012, Command!.Text);
            builder.CloseElement();
        }
        if (Trailing is not null)
        {
            builder.OpenElement(1020, "ButtonTrailing");
            builder.AddContent(1021, Trailing);
            builder.CloseElement();
        }
        if (IsBusy)
        {
            builder.OpenElement(1030, "ButtonSpinner");
            builder.AddAttribute(1031, "glyph", "ring");
            builder.AddAttribute(1032, "ring-fraction", 0.25);
            builder.AddAttribute(1033, "semantic-hidden", true);
            builder.CloseElement();
        }
    }

    /// <summary>Runs the click callback and the command.</summary>
    /// <param name="e">The activation event.</param>
    /// <returns>A task that completes when the action has finished.</returns>
    protected async Task InvokeActionAsync(PointerEventArgs e)
    {
        if (e.IsSynthetic) PressAt(e);
        await OnClick.InvokeAsync(e);
        if (Command is { } command) await command.ExecuteAsync();
    }

    private void PressAt(PointerEventArgs e)
    {
        if (IsInactive || Element.Element?.IsDisabled is true || (!e.IsSynthetic && e.Button != PointerButton.Primary) ||
            !double.IsFinite(e.X) || !double.IsFinite(e.Y)) return;
        pressOrigin = e.IsSynthetic ? null : Element.GetLocalPoint(e.X, e.Y);
        hasPressOrigin = true;
        pressReplay = !pressReplay;
        StateHasChanged();
    }

    /// <summary>Stops following the command and releases timers.</summary>
    public void Dispose()
    {
        if (observed is not null) observed.Changed -= Refresh;
        observed = null;
        joined?.Leave(this);
        joined = null;
        OnDispose();
    }

    /// <summary>Releases resources owned by a derived button.</summary>
    protected virtual void OnDispose()
    {
    }
}

/// <summary>
/// A pressable control that runs an action: <see cref="ButtonBase.OnClick" />, a <see cref="ButtonBase.Command" />, or the
/// submission or reset of its nearest <see cref="Form" /> (<see cref="Type" />). <see cref="IsDefault" /> makes it the
/// button pressed by Enter in its window. With <see cref="RepeatInterval" /> the action repeats while the pointer holds it.
/// </summary>
public sealed class Button : ButtonBase
{
    private Pressable.RepeatState? repeat;
    private TimeProvider? repeatTime;

    /// <summary>Gets or sets the action requested in the nearest form.</summary>
    [Parameter] public ButtonType Type { get; set; }

    /// <summary>
    /// Gets or sets whether this is the default button of the window: Enter presses it when the focused control does
    /// not use the key. Themes usually draw it with the accent appearance.
    /// </summary>
    [Parameter] public bool IsDefault { get; set; }

    /// <summary>Gets or sets the interval between held pointer or Enter/Space actions, or null for ordinary click activation. Repetition fires once on press and suppresses release activation.</summary>
    [Parameter] public TimeSpan? RepeatInterval { get; set; }

    /// <summary>Gets or sets the delay before held pointer or Enter/Space repetition; 400ms by default. The initial action runs on press.</summary>
    [Parameter] public TimeSpan RepeatDelay { get; set; } = TimeSpan.FromMilliseconds(400);

    /// <summary>Gets or sets the cascading timer provider for held input; TimeProvider.System by default.</summary>
    [CascadingParameter] public TimeProvider Time { get; set; } = TimeProvider.System;

    /// <summary>Gets the NSS element name emitted by Button.</summary>
    protected override string ElementName => "Button";

    /// <summary>Validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(Type)) throw new ArgumentOutOfRangeException(nameof(Type));
        ArgumentNullException.ThrowIfNull(Time);
        if (RepeatInterval is { } interval) Pressable.RepeatState.ValidateDuration(interval, nameof(RepeatInterval), positive: true);
        Pressable.RepeatState.ValidateDuration(RepeatDelay, nameof(RepeatDelay));
        if (!ReferenceEquals(repeatTime, Time)) { repeat?.SetTime(Time); repeatTime = Time; }
        if (IsInactive || RepeatInterval is null) repeat?.Cancel();
    }

    /// <summary>Adds the button role, the default state and the activation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        base.AddAttributes(builder);
        builder.AddAttribute(30, "role", "button");
        builder.AddAttribute(31, "default", IsDefault);
        builder.AddAttribute(32, "button-type", Type.ToString().ToLowerInvariant());
        builder.AddAttribute(33, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ActivateAsync));
        if (RepeatInterval is { } interval)
        {
            repeat ??= new Pressable.RepeatState(work => InvokeAsync(work), InvokeActionAsync, time: Time,
                owner: () => Element, available: () => !IsInactive);
            if (IsInactive) repeat.Cancel();
            builder.AddAttribute(34, "__internal_onpointerdown#repeat", EventCallback.Factory.Create<PointerEventArgs>(this,
                e => repeat.PointerDownAsync(e, interval, RepeatDelay, TimeSpan.Zero)));
            builder.AddAttribute(35, "__internal_onpointerup#repeat", EventCallback.Factory.Create<PointerEventArgs>(this, repeat.PointerUp));
            builder.AddAttribute(36, "__internal_onpointerleave#repeat", EventCallback.Factory.Create<PointerEventArgs>(this, () => repeat.Cancel()));
            builder.AddAttribute(37, "__internal_onlostpointercapture#repeat", EventCallback.Factory.Create<PointerEventArgs>(this, repeat.Lost));
            builder.AddAttribute(38, "__internal_onblur#repeat", EventCallback.Factory.Create<FocusEventArgs>(this, () => repeat.Cancel()));
            builder.AddAttribute(39, "__internal_onkeydown#repeat", EventCallback.Factory.Create<KeyboardEventArgs>(this,
                e => repeat.KeyDownAsync(e, interval, RepeatDelay)));
            builder.AddAttribute(40, "__internal_onkeyup#repeat", EventCallback.Factory.Create<KeyboardEventArgs>(this, repeat.KeyUp));
        }
    }

    /// <summary>Stops the repetition timer.</summary>
    protected override void OnDispose() => repeat?.Dispose();

    private async Task ActivateAsync(PointerEventArgs e)
    {
        if (IsInactive) return;
        if (repeat?.ConsumeClick() is true && !e.IsSynthetic) return;
        await InvokeActionAsync(e);
        if (Type == ButtonType.Button || Element.Element is not { } element) return;
        var form = ControlTree.Ancestors(element).TakeWhile(node => node.Attributes.GetValueOrDefault("overlay") is not true)
            .FirstOrDefault(node => node.Attributes.GetValueOrDefault("role") is "form");
        if (form?.Attributes.GetValueOrDefault(Type == ButtonType.Reset ? "__form-reset" : "__form-submit") is EventCallback callback)
            await callback.InvokeAsync();
    }
}

/// <summary>
/// A button that keeps a pressed state, such as Bold in an editor: activation flips <see cref="IsPressed" />
/// (<c>@bind-IsPressed</c>) and runs the optional click callback and command. The element has the <c>pressed</c> attribute.
/// </summary>
public sealed class ToggleButton : ButtonBase
{
    private readonly ControllableState<bool> pressed = new(false);

    /// <summary>Gets or sets whether the button is pressed.</summary>
    [Parameter] public bool IsPressed { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the pressed state.</summary>
    [Parameter] public EventCallback<bool> IsPressedChanged { get; set; }

    /// <summary>Gets whether the button is currently pressed.</summary>
    public bool Pressed => pressed.Value;

    /// <summary>Gets the NSS element name emitted by ToggleButton.</summary>
    protected override string ElementName => "ToggleButton";

    /// <summary>Synchronizes the pressed state with the parameter.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        pressed.Sync(IsPressed);
    }

    /// <summary>Adds the toggle role, the pressed state and the activation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        base.AddAttributes(builder);
        builder.AddAttribute(30, "role", "togglebutton");
        builder.AddAttribute(31, "pressed", pressed.Value);
        builder.AddAttribute(32, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleAsync));
    }

    private async Task ToggleAsync(PointerEventArgs e)
    {
        if (IsInactive) return;
        pressed.Set(!pressed.Value);
        StateHasChanged();
        await IsPressedChanged.InvokeAsync(pressed.Value);
        await InvokeActionAsync(e);
        await Events.RaiseChangeAsync(Element, pressed.Value);
    }
}
