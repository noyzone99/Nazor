using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// A button that opens a <see cref="Controls.Menu" /> of commands, such as Sort or New ▾: a click, Enter, Space,
/// ArrowDown, Alt+ArrowDown or F4 open the menu (<see cref="Menu" />) next to the button, choosing an item or dismissing
/// the menu closes it and returns the focus to the button. It has the look of a <see cref="Button" /> (appearance, intent,
/// size, shape, leading and trailing content) followed by a <c>MenuButtonChevron</c> part, and the <c>expanded</c>
/// attribute while the menu is open (<c>@bind-IsOpen</c>). <see cref="ButtonBase.OnClick" /> runs each time the button
/// opens or closes the menu; a <see cref="ButtonBase.Command" /> supplies the label, the tooltip and the enabled state,
/// but its action is not run.
/// </summary>
public sealed class MenuButton : ButtonBase, IMenuOwner
{
    private readonly ControllableState<bool> open = new(false);

    /// <summary>Gets or sets the items of the menu.</summary>
    [Parameter] public RenderFragment? Menu { get; set; }

    /// <summary>Gets or sets whether the menu is open.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Gets or sets the callback invoked when the menu opens or closes.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets the preferred side of the button on which the menu opens.</summary>
    [Parameter] public OverlayPlacement Placement { get; set; }

    /// <summary>Gets or sets how the menu lines up with the button.</summary>
    [Parameter] public OverlayAlignment Alignment { get; set; }

    /// <summary>Gets or sets whether the chevron part is shown; true by default.</summary>
    [Parameter] public bool ShowChevron { get; set; } = true;

    /// <summary>Gets or sets the space-separated NSS classes of the menu surface.</summary>
    [Parameter] public string? MenuClass { get; set; }

    /// <summary>Gets whether the menu is open.</summary>
    public bool Opened => open.Value;

    /// <summary>Gets the NSS element name emitted by MenuButton.</summary>
    protected override string ElementName => "MenuButton";

    /// <summary>Validates the parameters and synchronizes the open state.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (!Enum.IsDefined(Alignment)) throw new ArgumentOutOfRangeException(nameof(Alignment));
        open.Sync(IsOpen);
        if (IsInactive) open.Set(false);
    }

    /// <summary>Adds the button role, the expanded state, the activation and the opening keys.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        base.AddAttributes(builder);
        builder.AddAttribute(30, "role", "button");
        builder.AddAttribute(31, "expanded", open.Value);
        builder.AddAttribute(32, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ToggleAsync));
        builder.AddAttribute(33, "__internal_onkeydown#menubutton", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
    }

    /// <summary>Adds the button parts, the chevron and the menu.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        base.AddContent(builder);
        if (ShowChevron)
        {
            builder.OpenElement(1100, "MenuButtonChevron");
            builder.AddAttribute(1101, "glyph", Placement == OverlayPlacement.Top ? "chevron-up" : "chevron-down");
            builder.AddAttribute(1102, "semantic-hidden", true);
            builder.CloseElement();
        }
        OpenDelegatedComponent<Menu>(builder, 1110);
        builder.AddComponentParameter(1111, nameof(Controls.Menu.IsOpen), open.Value);
        builder.AddComponentParameter(1112, nameof(Controls.Menu.IsOpenChanged), EventCallback.Factory.Create<bool>(this, SetOpenAsync));
        builder.AddComponentParameter(1113, nameof(Controls.Menu.Anchor), Element);
        builder.AddComponentParameter(1114, nameof(Controls.Menu.Placement), Placement);
        builder.AddComponentParameter(1115, nameof(Controls.Menu.Alignment), Alignment);
        builder.AddComponentParameter(1116, nameof(Controls.Menu.Owner), this);
        builder.AddComponentParameter(1117, nameof(Controls.Menu.Class), MenuClass);
        builder.AddComponentParameter(1118, nameof(Controls.Menu.AccessibleName), AccessibleName ?? Command?.Text);
        builder.AddComponentParameter(1119, nameof(Controls.Menu.ChildContent), Menu);
        builder.CloseComponent();
    }

    /// <summary>Opens the menu.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task OpenAsync() => IsInactive ? Task.CompletedTask : SetOpenAsync(true);

    /// <summary>Closes the menu.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task CloseAsync() => SetOpenAsync(false);

    private async Task ToggleAsync(PointerEventArgs e)
    {
        if (IsInactive) return;
        await OnClick.InvokeAsync(e);
        await SetOpenAsync(!open.Value);
    }

    private Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || IsInactive || open.Value) return Task.CompletedTask;
        var opens = (e.Key == "ArrowDown" && (e.Modifiers == KeyModifiers.Alt || (e.Modifiers == KeyModifiers.None && !InVerticalBar())))
            || (e.Key == "F4" && e.Modifiers == KeyModifiers.None);
        if (!opens) return Task.CompletedTask;
        e.Handled = true;
        return SetOpenAsync(true);
    }

    private bool InVerticalBar() => ControlTree.Ancestors(Element.Element).Skip(1)
        .Any(node => node.Attributes.GetValueOrDefault("role") is "toolbar" or "menubar" && node.Attributes.GetValueOrDefault("vertical") is true);

    private async Task SetOpenAsync(bool value)
    {
        if (!open.Set(value)) return;
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(value);
    }
}
