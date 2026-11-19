using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>A surface that menu items close when they run; implemented by menus and the controls that own one.</summary>
public interface IMenuOwner
{
    /// <summary>Closes the menu and every menu that contains it.</summary>
    /// <returns>A task that completes when the menus are closed.</returns>
    Task CloseAsync();
}

/// <summary>
/// A list of commands that opens next to an anchor: the arrows move between items, typing jumps to an item, Enter or a
/// click runs one and closes the menu, Escape, Tab or a press outside close it, and the focus returns where it was.
/// Arrow navigation stops at the first and last enabled commands and scrolls the focused command into view.
/// Fill it with <see cref="MenuItem" />, <see cref="MenuCheckItem" />, <see cref="MenuRadioGroup{TValue}" />,
/// <see cref="Submenu" />, <see cref="MenuSection" /> and <see cref="MenuSeparator" />. Menu buttons, split buttons, menu
/// bars and context menus open one; it can also be opened directly with <see cref="IsOpen" /> and <see cref="Anchor" />.
/// </summary>
public sealed class Menu : ComponentBase, IMenuOwner
{
    private readonly ControllableState<bool> open = new(false);
    private HoverTracker hover = null!;
    private PopoverContainer? surface;

    /// <summary>Initializes the decorative hover tracker shared by the menu's command rows.</summary>
    protected override void OnInitialized() => hover = new(StateHasChanged);

    /// <summary>Gets or sets whether the menu is open.</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>Gets or sets the callback invoked when the menu opens or closes from inside.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets the element the menu opens next to.</summary>
    [Parameter] public ElementReference? Anchor { get; set; }

    /// <summary>Gets or sets a point in window coordinates used when there is no anchor, such as a right click.</summary>
    [Parameter] public (double X, double Y)? AnchorPoint { get; set; }

    /// <summary>Gets or sets the preferred side of the anchor.</summary>
    [Parameter] public OverlayPlacement Placement { get; set; }

    /// <summary>Gets or sets the alignment with the anchor.</summary>
    [Parameter] public OverlayAlignment Alignment { get; set; }

    /// <summary>Gets or sets the distance from the anchor.</summary>
    [Parameter] public double Offset { get; set; }

    /// <summary>Gets or sets the callback invoked when the user dismisses the menu.</summary>
    [Parameter] public EventCallback OnDismiss { get; set; }

    /// <summary>Gets or sets the space-separated NSS classes of the menu surface.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Gets or sets the accessible name of the menu.</summary>
    [Parameter] public string? AccessibleName { get; set; }

    /// <summary>Gets or sets the items.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the menu that contains this one, supplied automatically.</summary>
    [CascadingParameter] public IMenuOwner? Parent { get; set; }

    /// <summary>Gets or sets whether this menu is nested in another one.</summary>
    [Parameter] public bool IsSubmenu { get; set; }

    /// <summary>Gets or sets the owner closed together with this menu, such as the button that opened it.</summary>
    [Parameter] public IMenuOwner? Owner { get; set; }

    /// <summary>Gets whether the menu is open.</summary>
    public bool Opened => open.Value;

    /// <summary>Synchronizes the open state.</summary>
    protected override void OnParametersSet() => open.Sync(IsOpen);

    /// <summary>Refreshes hovered command geometry after item changes.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender) => hover.Refresh();

    /// <summary>Renders the anchored menu surface while open.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        OpenDelegatedComponent<PopoverContainer>(builder, 0);
        builder.AddComponentParameter(1, nameof(PopoverContainer.As), "Menu");
        builder.AddComponentParameter(2, nameof(PopoverContainer.IsOpen), open.Value);
        builder.AddComponentParameter(3, nameof(PopoverContainer.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => value ? Task.CompletedTask : SetOpenAsync(false)));
        builder.AddComponentParameter(4, nameof(PopoverContainer.Anchor), Anchor);
        builder.AddComponentParameter(5, nameof(PopoverContainer.AnchorPoint), AnchorPoint);
        builder.AddComponentParameter(6, nameof(PopoverContainer.Placement), IsSubmenu && Placement == OverlayPlacement.Bottom ? OverlayPlacement.Right : Placement);
        builder.AddComponentParameter(7, nameof(PopoverContainer.Alignment), Alignment);
        builder.AddComponentParameter(8, nameof(PopoverContainer.Offset), Offset);
        builder.AddComponentParameter(9, nameof(PopoverContainer.Role), "menu");
        builder.AddComponentParameter(10, nameof(PopoverContainer.IsMenu), true);
        builder.AddComponentParameter(11, nameof(PopoverContainer.IsSubmenu), IsSubmenu);
        builder.AddComponentParameter(12, nameof(PopoverContainer.OnDismiss), OnDismiss);
        builder.AddComponentParameter(13, nameof(Control.Class), Class);
        builder.AddComponentParameter(14, nameof(Control.AccessibleName), AccessibleName);
        builder.AddComponentParameter(15, nameof(PopoverContainer.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<Indicator>(0);
            content.AddComponentParameter(1, nameof(Indicator.As), "HoverIndicator");
            content.AddComponentParameter(2, nameof(Indicator.Target), hover.Target);
            content.CloseComponent();
            content.OpenComponent<CascadingValue<IMenuOwner>>(10);
            content.AddComponentParameter(11, "Value", this);
            content.AddComponentParameter(12, "IsFixed", true);
            content.AddComponentParameter(13, "ChildContent", ChildContent);
            content.CloseComponent();
        }));
        builder.AddComponentParameter(16, nameof(PopoverContainer.AdditionalAttributes), hover.Attributes(this, () => surface?.Element,
            item => item.Attributes.GetValueOrDefault("role") is "menuitem" or "menuitemcheckbox" or "menuitemradio"));
        builder.AddComponentReferenceCapture(17, component => surface = (PopoverContainer)component);
        builder.CloseComponent();
    }

    /// <summary>Opens the menu.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task OpenAsync() => SetOpenAsync(true);

    /// <summary>Closes this menu only, leaving the menus that contain it open.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task CloseOwnAsync() => SetOpenAsync(false);

    /// <summary>Closes this menu, its owner and the menus that contain it.</summary>
    /// <returns>A task that completes when the menus are closed.</returns>
    public async Task CloseAsync()
    {
        await SetOpenAsync(false);
        if (Owner is not null) await Owner.CloseAsync();
        else if (IsSubmenu && Parent is not null) await Parent.CloseAsync();
    }

    private async Task SetOpenAsync(bool value)
    {
        if (!open.Set(value)) return;
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(value);
    }
}

/// <summary>
/// The look and behavior shared by menu items: optional leading content, the label (or the command's text), a
/// description, the shortcut and the danger intent. The parts are <c>MenuItemLeading</c>, <c>MenuItemLabel</c>,
/// <c>MenuItemDescription</c>, <c>MenuItemShortcut</c> and <c>MenuItemTrailing</c>; the element carries <c>intent</c>.
/// </summary>
public abstract class MenuItemBase : ContentControl, IDisposable
{
    private Command? observed;

    /// <summary>Gets or sets content shown before the label, usually an <see cref="Icon" />.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets a secondary line shown below the label.</summary>
    [Parameter] public RenderFragment? Description { get; set; }

    /// <summary>Gets or sets the shortcut shown at the end of the item; the command's shortcut is used when null.</summary>
    [Parameter] public KeyGesture? Shortcut { get; set; }

    /// <summary>Gets or sets the meaning of the item's color, such as <see cref="Intent.Danger" /> for Delete.</summary>
    [Parameter] public Intent Intent { get; set; }

    /// <summary>Gets or sets whether the item rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether running the item closes the menu.</summary>
    [Parameter] public bool CloseOnSelect { get; set; } = true;

    /// <summary>Gets or sets a command that supplies the label, the shortcut, the enabled state and the action.</summary>
    [Parameter] public Command? Command { get; set; }

    /// <summary>Gets or sets the callback invoked when the item runs.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnClick { get; set; }

    /// <summary>Gets or sets the menu that contains the item, supplied automatically.</summary>
    [CascadingParameter] public IMenuOwner? Menu { get; set; }

    /// <summary>Gets whether the item rejects interaction.</summary>
    protected bool IsInactive => Disabled || Command is { IsEnabled: false };

    /// <summary>Gets the accessible role of the item.</summary>
    protected abstract string Role { get; }

    /// <summary>Follows the command's changes.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Intent)) throw new ArgumentOutOfRangeException(nameof(Intent));
        if (ReferenceEquals(observed, Command)) return;
        if (observed is not null) observed.Changed -= Refresh;
        observed = Command;
        if (observed is not null) observed.Changed += Refresh;
    }

    private void Refresh() => InvokeAsync(StateHasChanged);

    /// <summary>Adds the role, the states and the activation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", Role);
        builder.AddAttribute(11, "disabled", IsInactive);
        builder.AddAttribute(12, "focusable", !IsInactive);
        builder.AddAttribute(13, "intent", Vocabulary.Name(Intent));
        builder.AddAttribute(14, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, RunAsync));
        if (AccessibleName is null && ChildContent is null && Command is not null) builder.AddAttribute(15, "accessible-name", Command.Text);
        AddItemAttributes(builder);
    }

    /// <summary>Adds attributes specific to the item, with sequence numbers from 20 to 899.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected virtual void AddItemAttributes(RenderTreeBuilder builder)
    {
    }

    /// <summary>Adds the parts of the item.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "MenuItemLeading");
        AddLeading(builder);
        builder.CloseElement();
        builder.OpenElement(1010, "MenuItemLabel");
        if (ChildContent is not null) builder.AddContent(1011, ChildContent);
        else builder.AddContent(1012, Command?.Text);
        builder.CloseElement();
        if (Description is not null)
        {
            builder.OpenElement(1020, "MenuItemDescription");
            builder.AddContent(1021, Description);
            builder.CloseElement();
        }
        if ((Shortcut ?? Command?.Gesture) is { } gesture)
        {
            builder.OpenComponent<ShortcutLabel>(1030);
            builder.AddComponentParameter(1031, nameof(ShortcutLabel.Gesture), gesture);
            builder.AddComponentParameter(1032, nameof(ShortcutLabel.Class), "MenuItemShortcut");
            builder.CloseComponent();
        }
        AddTrailing(builder);
    }

    /// <summary>Adds the content of the leading part.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected virtual void AddLeading(RenderTreeBuilder builder) => builder.AddContent(1001, Leading);

    /// <summary>Adds parts after the shortcut.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected virtual void AddTrailing(RenderTreeBuilder builder)
    {
    }

    /// <summary>Runs the item: the item's own behavior, the click callback, the command, then closes the menu.</summary>
    /// <param name="e">The activation event.</param>
    /// <returns>A task that completes when the item has run.</returns>
    protected virtual async Task RunAsync(PointerEventArgs e)
    {
        if (IsInactive) return;
        await OnClick.InvokeAsync(e);
        if (Command is { } command) await command.ExecuteAsync();
        if (CloseOnSelect && Menu is not null) await Menu.CloseAsync();
    }

    /// <summary>Stops following the command and releases resources owned by derived items.</summary>
    public void Dispose()
    {
        if (observed is not null) observed.Changed -= Refresh;
        observed = null;
        OnDispose();
    }

    /// <summary>Releases resources owned by a derived menu item.</summary>
    protected virtual void OnDispose() { }
}

/// <summary>A command of a <see cref="Menu" />.</summary>
public sealed class MenuItem : MenuItemBase
{
    /// <summary>Gets the NSS element name emitted by MenuItem.</summary>
    protected override string ElementName => "MenuItem";

    /// <summary>Gets the menu item role.</summary>
    protected override string Role => "menuitem";
}

/// <summary>
/// A menu item with a checked state, such as View › Status Bar: activation flips <see cref="IsChecked" />
/// (<c>@bind-IsChecked</c>). The leading part shows a <c>MenuItemCheck</c> mark while checked.
/// </summary>
public sealed class MenuCheckItem : MenuItemBase
{
    private readonly ControllableState<bool> isChecked = new(false);

    /// <summary>Gets or sets whether the item is checked.</summary>
    [Parameter] public bool IsChecked { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the checked state.</summary>
    [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

    /// <summary>Gets the NSS element name emitted by MenuCheckItem.</summary>
    protected override string ElementName => "MenuCheckItem";

    /// <summary>Gets the checkable menu item role.</summary>
    protected override string Role => "menuitemcheckbox";

    /// <summary>Synchronizes the checked state.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        isChecked.Sync(IsChecked);
    }

    /// <summary>Adds the checked state.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddItemAttributes(RenderTreeBuilder builder) => builder.AddAttribute(20, "checked", isChecked.Value);

    /// <summary>Adds the check mark.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddLeading(RenderTreeBuilder builder)
    {
        builder.OpenElement(1001, "MenuItemCheck");
        builder.AddAttribute(1002, "glyph", isChecked.Value ? "check" : null);
        builder.CloseElement();
        builder.AddContent(1003, Leading);
    }

    /// <summary>Flips the checked state, reports it and runs the item.</summary>
    /// <param name="e">The activation event.</param>
    /// <returns>A task that completes when the item has run.</returns>
    protected override async Task RunAsync(PointerEventArgs e)
    {
        if (IsInactive) return;
        isChecked.Set(!isChecked.Value);
        StateHasChanged();
        await IsCheckedChanged.InvokeAsync(isChecked.Value);
        await base.RunAsync(e);
    }
}

/// <summary>
/// A set of exclusive <see cref="MenuRadioItem{TValue}" /> choices in a menu, such as View › Sort by: <see cref="Value" />
/// (<c>@bind-Value</c>) is the checked item's value. It renders a <c>MenuRadioGroup</c> group.
/// </summary>
/// <typeparam name="TValue">The type of the values.</typeparam>
[CascadingTypeParameter(nameof(TValue))]
public sealed class MenuRadioGroup<TValue> : ContentControl
{
    private readonly ControllableState<TValue?> value = new(default);

    /// <summary>Gets or sets the value of the checked item.</summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>Gets or sets the callback invoked when the user checks another item.</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>Gets the NSS element name emitted by MenuRadioGroup.</summary>
    protected override string ElementName => "MenuRadioGroup";

    /// <summary>Synchronizes the value.</summary>
    protected override void OnParametersSet() => value.Sync(Value);

    /// <summary>Adds the group role.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "role", "group");

    /// <summary>Supplies the group to its items.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<MenuRadioGroup<TValue>>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    internal bool IsChecked(TValue candidate) => EqualityComparer<TValue>.Default.Equals(value.Value!, candidate);

    internal async Task CheckAsync(TValue candidate)
    {
        if (!value.Set(candidate)) return;
        StateHasChanged();
        await ValueChanged.InvokeAsync(candidate);
    }
}

/// <summary>One choice of a <see cref="MenuRadioGroup{TValue}" />; the leading part shows a <c>MenuItemRadio</c> mark.</summary>
/// <typeparam name="TValue">The type of the values.</typeparam>
public sealed class MenuRadioItem<TValue> : MenuItemBase
{
    /// <summary>Gets or sets the value checked by this item.</summary>
    [Parameter] public TValue Value { get; set; } = default!;

    /// <summary>Gets or sets the enclosing group, supplied automatically.</summary>
    [CascadingParameter] public MenuRadioGroup<TValue>? Group { get; set; }

    private bool Checked => Group?.IsChecked(Value) == true;

    /// <summary>Gets the NSS element name emitted by MenuRadioItem.</summary>
    protected override string ElementName => "MenuRadioItem";

    /// <summary>Gets the exclusive menu item role.</summary>
    protected override string Role => "menuitemradio";

    /// <summary>Requires an enclosing group.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Group is null) throw new InvalidOperationException("MenuRadioItem must be inside a MenuRadioGroup of the same value type.");
    }

    /// <summary>Adds the checked state.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddItemAttributes(RenderTreeBuilder builder) => builder.AddAttribute(20, "checked", Checked);

    /// <summary>Adds the radio mark.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddLeading(RenderTreeBuilder builder)
    {
        builder.OpenElement(1001, "MenuItemRadio");
        builder.AddAttribute(1002, "checked", Checked);
        builder.CloseElement();
        builder.AddContent(1003, Leading);
    }

    /// <summary>Checks this item and runs it.</summary>
    /// <param name="e">The activation event.</param>
    /// <returns>A task that completes when the item has run.</returns>
    protected override async Task RunAsync(PointerEventArgs e)
    {
        if (IsInactive || Group is null) return;
        await Group.CheckAsync(Value);
        await base.RunAsync(e);
    }
}

/// <summary>A line between groups of menu items.</summary>
public sealed class MenuSeparator : Control
{
    /// <summary>Gets the NSS element name emitted by MenuSeparator.</summary>
    protected override string ElementName => "MenuSeparator";

    /// <summary>Adds the separator role.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder) => builder.AddAttribute(10, "role", "separator");
}

/// <summary>A titled group of menu items; the title is a non-interactive <c>MenuSectionHeader</c> part.</summary>
public sealed class MenuSection : ContentControl
{
    /// <summary>Gets or sets the title of the section.</summary>
    [Parameter] public string? Header { get; set; }

    /// <summary>Gets the NSS element name emitted by MenuSection.</summary>
    protected override string ElementName => "MenuSection";

    /// <summary>Adds the group role and the section name.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        if (AccessibleName is null) builder.AddAttribute(11, "accessible-name", Header);
    }

    /// <summary>Adds the header and the items.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (Header is not null)
        {
            builder.OpenElement(1000, "MenuSectionHeader");
            builder.AddAttribute(1001, "semantic-hidden", true);
            builder.AddContent(1002, Header);
            builder.CloseElement();
        }
        builder.AddContent(1010, ChildContent);
    }
}

/// <summary>
/// A menu item that opens a nested <see cref="Menu" />: a click, the right arrow or resting the pointer on it opens the
/// nested menu, the left arrow or Escape closes it and returns to the item. The item shows a <c>MenuItemSubmenuArrow</c>
/// part; <see cref="Content" /> holds the nested items.
/// </summary>
public sealed class Submenu : MenuItemBase
{
    private bool open;
    private HoverIntent? hover;
    private TimeProvider? hoverTime;

    /// <summary>Gets or sets the cascading timer provider used for pointer-hover intent. The default is TimeProvider.System; keyboard and explicit opening do not use a timer.</summary>
    [CascadingParameter] public TimeProvider Time { get; set; } = TimeProvider.System;

    /// <summary>Gets or sets the nested items.</summary>
    [Parameter, EditorRequired] public RenderFragment? Content { get; set; }

    /// <summary>Gets whether the nested menu is open.</summary>
    public bool Opened => open;

    /// <summary>Gets the NSS element name emitted by Submenu.</summary>
    protected override string ElementName => "Submenu";

    /// <summary>Gets the menu item role.</summary>
    protected override string Role => "menuitem";

    /// <summary>Validates the slots.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Time);
        if (!ReferenceEquals(hoverTime, Time))
        {
            hover?.Dispose();
            hover = null;
            hoverTime = Time;
        }
        if (Content is null) throw new InvalidOperationException("Submenu requires Content.");
        if (IsInactive || Menu is Controls.Menu { Opened: false }) { hover?.Cancel(); open = false; }
    }

    /// <summary>Adds the expanded state and the opening behavior.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddItemAttributes(RenderTreeBuilder builder)
    {
        hover ??= new HoverIntent(work => InvokeAsync(work), () => SetOpenAsync(true), () => Task.CompletedTask, Time);
        builder.AddAttribute(20, "expanded", open);
        builder.AddAttribute(21, "__internal_onkeydown#submenu", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(22, "__internal_onpointerenter#submenu", EventCallback.Factory.Create<PointerEventArgs>(this, Entered));
        builder.AddAttribute(23, "__internal_onpointerleave#submenu", EventCallback.Factory.Create<PointerEventArgs>(this, () => hover.Cancel()));
    }

    /// <summary>Adds the arrow and the nested menu.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddTrailing(RenderTreeBuilder builder)
    {
        builder.OpenElement(1040, "MenuItemSubmenuArrow");
        builder.AddAttribute(1041, "glyph", "chevron-right");
        builder.AddAttribute(1042, "semantic-hidden", true);
        builder.CloseElement();
        builder.OpenComponent<Menu>(1050);
        builder.AddComponentParameter(1051, nameof(Controls.Menu.IsOpen), open);
        builder.AddComponentParameter(1052, nameof(Controls.Menu.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => SetOpenAsync(value)));
        builder.AddComponentParameter(1053, nameof(Controls.Menu.Anchor), Element);
        builder.AddComponentParameter(1054, nameof(Controls.Menu.Placement), OverlayPlacement.Right);
        builder.AddComponentParameter(1055, nameof(Controls.Menu.IsSubmenu), true);
        builder.AddComponentParameter(1056, nameof(Controls.Menu.ChildContent), Content);
        builder.CloseComponent();
    }

    /// <summary>Opens the nested menu instead of running a command.</summary>
    /// <param name="e">The activation event.</param>
    /// <returns>A task that completes when the menu is open.</returns>
    protected override Task RunAsync(PointerEventArgs e) => IsInactive ? Task.CompletedTask : SetOpenAsync(true);

    /// <summary>Cancels pending hover work when the item is removed.</summary>
    protected override void OnDispose() => hover?.Dispose();

    private void Entered()
    {
        if (IsInactive || Menu is Controls.Menu { Opened: false } || Element.Element is not { } trigger) return;
        var provider = ControlTree.Ancestors(trigger).Select(node => node.GetFeature<IOverlayMotion>()).FirstOrDefault(value => value is not null);
        hover!.OpenDelay = provider?.GetSubmenuOpenDelay(trigger) ?? TimeSpan.FromMilliseconds(250);
        hover.Enter();
    }

    private Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || e.Modifiers != KeyModifiers.None || IsInactive) return Task.CompletedTask;
        if (e.Key == "ArrowRight" && !open)
        {
            e.Handled = true;
            return SetOpenAsync(true);
        }
        if (e.Key == "ArrowLeft" && open)
        {
            e.Handled = true;
            return SetOpenAsync(false);
        }
        return Task.CompletedTask;
    }

    private Task SetOpenAsync(bool value)
    {
        hover?.Cancel();
        if (value && (IsInactive || Element.Element?.IsDisabled is true || Menu is Controls.Menu { Opened: false })) return Task.CompletedTask;
        if (open == value) return Task.CompletedTask;
        open = value;
        StateHasChanged();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Opens a <see cref="Menu" /> where the user asks for one on its <see cref="Trigger" /> content: a right click, Shift+F10
/// or the menu key (below the focused control). The trigger keeps its own left-click behavior. Call
/// <see cref="ShowAsync" /> to open it from other code, such as a long press.
/// </summary>
public sealed class ContextMenu : ComponentBase, IMenuOwner
{
    private bool open;
    private (double X, double Y) point;

    /// <summary>Gets or sets the content that opens the menu.</summary>
    [Parameter] public RenderFragment? Trigger { get; set; }

    /// <summary>Gets or sets the items.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets whether the menu is unavailable; the system text menu is not shown either.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the callback invoked when the menu opens or closes.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets the space-separated NSS classes of the menu surface.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Gets whether the menu is open.</summary>
    public bool Opened => open;

    /// <summary>Renders the trigger wrapper and the menu.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Trigger is not null)
        {
            builder.OpenElement(0, "ContextMenuTrigger");
            builder.AddAttribute(1, "__internal_oncontextmenu", EventCallback.Factory.Create<PointerEventArgs>(this, ShowAsync));
            builder.AddEventPreventDefaultAttribute(2, "oncontextmenu", true);
            builder.AddContent(3, Trigger);
            builder.CloseElement();
        }
        builder.OpenComponent<Menu>(10);
        builder.AddComponentParameter(11, nameof(Menu.IsOpen), open);
        builder.AddComponentParameter(12, nameof(Menu.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => SetOpenAsync(value)));
        builder.AddComponentParameter(13, nameof(Menu.AnchorPoint), point);
        builder.AddComponentParameter(14, nameof(Menu.Class), Class);
        builder.AddComponentParameter(15, nameof(Menu.ChildContent), ChildContent);
        builder.CloseComponent();
    }

    /// <summary>Opens the menu at the position of a pointer or keyboard request.</summary>
    /// <param name="e">The context-menu event.</param>
    /// <returns>A task that completes when the menu is open.</returns>
    public Task ShowAsync(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (Disabled) return Task.CompletedTask;
        point = (e.X, e.Y);
        return SetOpenAsync(true);
    }

    /// <summary>Closes the menu.</summary>
    /// <returns>A task that completes when the menu is closed.</returns>
    public Task CloseAsync() => SetOpenAsync(false);

    private async Task SetOpenAsync(bool value)
    {
        if (open == value) return;
        open = value;
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(value);
    }
}
