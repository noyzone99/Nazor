using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A button in two parts, such as Paste ▾: the primary part (<c>SplitButtonPrimary</c>, with <c>SplitButtonLeading</c>
/// and <c>SplitButtonLabel</c>) runs the main action, a <c>SplitButtonDivider</c> separates it from the trigger part
/// (<c>SplitButtonTrigger</c> with a <c>SplitButtonChevron</c>) that opens <see cref="Menu" /> with related commands. It
/// is a single Tab stop with the <c>splitbutton</c> role: Enter and Space run the main action, Alt+ArrowDown and F4 open
/// the menu. With <see cref="IsToggle" /> the main action also flips <see cref="IsPressed" /> (<c>@bind-IsPressed</c>),
/// for a split button that keeps a state, such as a text color. The element carries <c>appearance</c>,
/// <c>intent</c>, <c>size</c>, <c>expanded</c> while the menu is open, <c>pressed</c> and, inside a
/// <see cref="ButtonGroup" />, <c>position</c>.
/// </summary>
public sealed class SplitButton : ContentControl, IMenuOwner, IButtonGroupMember, IDisposable
{
    private readonly ControllableState<bool> open = new(false);
    private readonly ControllableState<bool> pressed = new(false);
    private Command? observed;
    private ButtonGroup? joined;
    private GroupPosition? position;

    /// <summary>Gets or sets how strongly the button is filled.</summary>
    [Parameter] public ControlAppearance Appearance { get; set; }

    /// <summary>Gets or sets the meaning of the button's color.</summary>
    [Parameter] public Intent Intent { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets content shown before the label in the primary part, usually an <see cref="Icon" />.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets the items of the menu opened by the trigger part.</summary>
    [Parameter] public RenderFragment? Menu { get; set; }

    /// <summary>Gets or sets the callback invoked when the primary part is activated.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnClick { get; set; }

    /// <summary>
    /// Gets or sets a command run by the primary part; it supplies the label when there is no content, the tooltip and
    /// the enabled state.
    /// </summary>
    [Parameter] public Command? Command { get; set; }

    /// <summary>Gets or sets whether the button rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether the primary part keeps a pressed state.</summary>
    [Parameter] public bool IsToggle { get; set; }

    /// <summary>Gets or sets whether the primary part is pressed, when <see cref="IsToggle" /> is true.</summary>
    [Parameter] public bool IsPressed { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the pressed state.</summary>
    [Parameter] public EventCallback<bool> IsPressedChanged { get; set; }

    /// <summary>Gets or sets whether the menu is open.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Gets or sets the callback invoked when the menu opens or closes.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets the preferred side of the button on which the menu opens.</summary>
    [Parameter] public OverlayPlacement Placement { get; set; }

    /// <summary>Gets or sets how the menu lines up with the button.</summary>
    [Parameter] public OverlayAlignment Alignment { get; set; }

    /// <summary>Gets or sets the tooltip of the trigger part; "More options" in the language of the interface by default.</summary>
    [Parameter] public string? TriggerLabel { get; set; }

    /// <summary>Gets or sets the space-separated NSS classes of the menu surface.</summary>
    [Parameter] public string? MenuClass { get; set; }

    [CascadingParameter] internal ButtonGroup? Group { get; set; }

    /// <summary>Gets whether the menu is open.</summary>
    public bool Opened => open.Value;

    /// <summary>Gets whether the primary part is pressed.</summary>
    public bool Pressed => pressed.Value;

    private bool IsInactive => Disabled || Command is { IsEnabled: false };

    /// <summary>Gets the NSS element name emitted by SplitButton.</summary>
    protected override string ElementName => "SplitButton";

    /// <summary>Validates the parameters, synchronizes the states and follows the command.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Appearance)) throw new ArgumentOutOfRangeException(nameof(Appearance));
        if (!Enum.IsDefined(Intent)) throw new ArgumentOutOfRangeException(nameof(Intent));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (!Enum.IsDefined(Alignment)) throw new ArgumentOutOfRangeException(nameof(Alignment));
        open.Sync(IsOpen);
        pressed.Sync(IsPressed);
        if (IsInactive) open.Set(false);
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

    /// <summary>Adds the role, the look, the states, the main action and the opening keys.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        var appearance = Appearance == ControlAppearance.Standard && Group is not null ? Group.Appearance : Appearance;
        var size = Size == ControlSize.Medium && Group is not null ? Group.Size : Size;
        builder.AddAttribute(10, "role", "splitbutton");
        builder.AddAttribute(11, "appearance", Vocabulary.Name(appearance));
        builder.AddAttribute(12, "intent", Vocabulary.Name(Intent));
        builder.AddAttribute(13, "size", Vocabulary.Name(size));
        builder.AddAttribute(14, "disabled", IsInactive);
        builder.AddAttribute(15, "focusable", !IsInactive);
        builder.AddAttribute(16, "expanded", open.Value);
        builder.AddAttribute(17, "pressed", IsToggle && pressed.Value);
        builder.AddAttribute(18, "toggle", IsToggle);
        builder.AddAttribute(19, "position", position is { } place ? Vocabulary.Name(place) : null);
        builder.AddAttribute(20, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, RunAsync));
        builder.AddAttribute(21, "__internal_onkeydown#splitbutton", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        if (AccessibleName is null && ChildContent is null && Command is not null) builder.AddAttribute(22, "accessible-name", Command.Text);
        if (Tooltip is null && Command is { Gesture: { } gesture } command)
            builder.AddAttribute(23, "tooltip", (command.Description ?? command.Text) + " (" + gesture.ToDisplayString() + ")");
    }

    /// <summary>Adds the primary part, the divider, the trigger part and the menu.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "SplitButtonPrimary");
        builder.AddAttribute(1001, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, RunAsync));
        if (Leading is not null)
        {
            builder.OpenElement(1002, "SplitButtonLeading");
            builder.AddContent(1003, Leading);
            builder.CloseElement();
        }
        if (ChildContent is not null || Command is not null)
        {
            builder.OpenElement(1004, "SplitButtonLabel");
            if (ChildContent is not null) builder.AddContent(1005, ChildContent);
            else builder.AddContent(1006, Command!.Text);
            builder.CloseElement();
        }
        builder.CloseElement();
        builder.OpenElement(1010, "SplitButtonDivider");
        builder.AddAttribute(1011, "semantic-hidden", true);
        builder.CloseElement();
        builder.OpenElement(1020, "SplitButtonTrigger");
        builder.AddAttribute(1021, "expander", true);
        builder.AddAttribute(1022, "tooltip", TriggerLabel ?? ControlStrings.Get("More"));
        builder.AddAttribute(1023, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleMenuAsync));
        builder.OpenElement(1024, "SplitButtonChevron");
        builder.AddAttribute(1025, "glyph", Placement == OverlayPlacement.Top ? "chevron-up" : "chevron-down");
        builder.AddAttribute(1026, "semantic-hidden", true);
        builder.CloseElement();
        builder.CloseElement();
        OpenDelegatedComponent<Menu>(builder, 1100);
        builder.AddComponentParameter(1101, nameof(Controls.Menu.IsOpen), open.Value);
        builder.AddComponentParameter(1102, nameof(Controls.Menu.IsOpenChanged), EventCallback.Factory.Create<bool>(this, SetOpenAsync));
        builder.AddComponentParameter(1103, nameof(Controls.Menu.Anchor), Element);
        builder.AddComponentParameter(1104, nameof(Controls.Menu.Placement), Placement);
        builder.AddComponentParameter(1105, nameof(Controls.Menu.Alignment), Alignment);
        builder.AddComponentParameter(1106, nameof(Controls.Menu.Owner), this);
        builder.AddComponentParameter(1107, nameof(Controls.Menu.Class), MenuClass);
        builder.AddComponentParameter(1108, nameof(Controls.Menu.AccessibleName), TriggerLabel ?? ControlStrings.Get("More"));
        builder.AddComponentParameter(1109, nameof(Controls.Menu.ChildContent), Menu);
        builder.CloseComponent();
    }

    /// <summary>Opens the menu.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task OpenAsync() => IsInactive ? Task.CompletedTask : SetOpenAsync(true);

    /// <summary>Closes the menu.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task CloseAsync() => SetOpenAsync(false);

    private async Task RunAsync(PointerEventArgs e)
    {
        if (IsInactive) return;
        if (IsToggle)
        {
            pressed.Set(!pressed.Value);
            StateHasChanged();
            await IsPressedChanged.InvokeAsync(pressed.Value);
        }
        await OnClick.InvokeAsync(e);
        if (Command is { } command) await command.ExecuteAsync();
        if (IsToggle) await Events.RaiseChangeAsync(Element, pressed.Value);
    }

    private Task ToggleMenuAsync() => IsInactive ? Task.CompletedTask : SetOpenAsync(!open.Value);

    private Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || IsInactive || open.Value) return Task.CompletedTask;
        if (!(e.Key == "ArrowDown" && e.Modifiers == KeyModifiers.Alt) && !(e.Key == "F4" && e.Modifiers == KeyModifiers.None)) return Task.CompletedTask;
        e.Handled = true;
        return SetOpenAsync(true);
    }

    private async Task SetOpenAsync(bool value)
    {
        if (!open.Set(value)) return;
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(value);
    }

    /// <summary>Stops following the command and leaves the group.</summary>
    public void Dispose()
    {
        if (observed is not null) observed.Changed -= Refresh;
        observed = null;
        joined?.Leave(this);
        joined = null;
    }
}
