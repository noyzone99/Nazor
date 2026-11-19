using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// The menu bar of a window, as in desktop applications: a row of <see cref="MenuBarItem" />, each opening a
/// <see cref="Menu" /> below it. The bar is one Tab stop: Left and Right move between the items, Down, Enter and Space
/// open a menu, Up opens it on its last item. While a menu is open, Left and Right switch to the neighboring menu and
/// resting the pointer on another item opens that one; Escape closes the menu and returns to its item. With
/// <see cref="FocusKeys" /> F10 and Alt move the focus to the bar from anywhere in the window. The bar has the
/// <c>menubar</c> role; its items carry <c>expanded</c> while their menu is open.
/// </summary>
public sealed class MenuBar : ContentControl, IDisposable
{
    private readonly List<MenuBarItem> items = [];
    private readonly List<IDisposable> registrations = [];
    private MenuBarItem? lastFocused;
    private HoverTracker hover = null!;

    /// <summary>Initializes the decorative item-hover tracker.</summary>
    protected override void OnInitialized() => hover = new(StateHasChanged);

    /// <summary>Refreshes the hover geometry after the bar's items change.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender) => hover.Refresh();

    /// <summary>Gets or sets the window's shortcut registry, used for the focus keys.</summary>
    [Inject] public KeyBindings Bindings { get; set; } = null!;

    /// <summary>Gets or sets whether F10 and Alt move the focus to the bar.</summary>
    [Parameter] public bool FocusKeys { get; set; } = true;

    /// <summary>Gets the item whose menu is open, or null.</summary>
    public MenuBarItem? OpenItem { get; private set; }

    /// <summary>Gets the NSS element name emitted by MenuBar.</summary>
    protected override string ElementName => "MenuBar";

    /// <summary>Registers or removes the focus keys.</summary>
    protected override void OnParametersSet()
    {
        var wanted = FocusKeys;
        if (wanted == registrations.Count > 0) return;
        if (!wanted)
        {
            Unregister();
            return;
        }
        var focus = EventCallback.Factory.Create<KeyboardEventArgs>(this, FocusFromKeyAsync);
        registrations.Add(Bindings.Register("F10", KeyModifiers.None, focus));
        registrations.Add(Bindings.Register("Alt", KeyModifiers.Alt, focus));
        registrations.Add(Bindings.Register("Alt", KeyModifiers.None, focus));
    }

    /// <summary>Adds the menu bar role and the accessible name.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "menubar");
        builder.AddMultipleAttributes(20, hover.Attributes(this, () => Element, item => item.Name == "MenuBarItem"));
    }

    /// <summary>Supplies the bar to its items.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Indicator>(990);
        builder.AddComponentParameter(991, nameof(Indicator.As), "HoverIndicator");
        builder.AddComponentParameter(992, nameof(Indicator.Target), hover.Target);
        builder.CloseComponent();
        builder.OpenComponent<CascadingValue<MenuBar>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "IsFixed", true);
        builder.AddComponentParameter(1003, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    /// <summary>Moves the focus to the item focused last, or to the first enabled item.</summary>
    /// <returns>A task that completes when the focus has moved.</returns>
    public Task FocusAsync()
    {
        var target = lastFocused is { Disabled: false } last && items.Contains(last) ? last : Ordered().FirstOrDefault(item => !item.Disabled);
        return target?.Element.Element is { IsRemoved: false } ? target.FocusAsync() : Task.CompletedTask;
    }

    private async Task FocusFromKeyAsync(KeyboardEventArgs e)
    {
        if (e.Repeat) return;
        e.Handled = true;
        if (OpenItem is { } open) await open.CloseAsync();
        await FocusAsync();
    }

    internal void Register(MenuBarItem item) => items.Add(item);

    internal void Unregister(MenuBarItem item)
    {
        items.Remove(item);
        if (OpenItem == item) OpenItem = null;
        if (lastFocused == item) lastFocused = null;
    }

    internal void Focused(MenuBarItem item) => lastFocused = item;

    internal void Opened(MenuBarItem item)
    {
        if (OpenItem is { } other && other != item) other.CloseLocal();
        OpenItem = item;
    }

    internal void Closed(MenuBarItem item)
    {
        if (OpenItem == item) OpenItem = null;
    }

    internal Task MoveAsync(MenuBarItem from, int direction)
    {
        var order = Ordered().Where(item => !item.Disabled || item == from).ToList();
        var index = order.IndexOf(from);
        return index < 0 || order.Count < 2 ? Task.CompletedTask : SwitchAsync(order[(index + direction + order.Count) % order.Count], false);
    }

    internal async Task SwitchAsync(MenuBarItem to, bool last)
    {
        if (to.Element.Element is { IsRemoved: false }) await to.FocusAsync(preventScroll: true);
        to.Open(last);
    }

    private List<MenuBarItem> Ordered()
    {
        if (Element.Element is not { } bar) return items;
        var elements = ControlTree.Descendants(bar).ToList();
        return [.. items.OrderBy(item => item.Element.Element is { } element ? elements.IndexOf(element) : int.MaxValue)];
    }

    private void Unregister()
    {
        foreach (var registration in registrations) registration.Dispose();
        registrations.Clear();
    }

    /// <summary>Removes the focus keys.</summary>
    public void Dispose() => Unregister();
}

/// <summary>
/// An entry of a <see cref="MenuBar" />: its label (the child content) in the bar and, below it, the menu holding the
/// items of <see cref="Content" />. A click toggles the menu. The item has the <c>menuitem</c> role and the
/// <c>expanded</c> state; its menu is a <c>Menu</c> surface like the one of <see cref="Menu" />.
/// </summary>
public sealed class MenuBarItem : ContentControl, IMenuOwner, IDisposable
{
    private bool open;
    private bool openOnLast;
    private MenuBar? registered;

    /// <summary>Gets or sets the items of the menu.</summary>
    [Parameter, EditorRequired] public RenderFragment? Content { get; set; }

    /// <summary>Gets or sets whether the entry rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets the accessible name of the menu. When null, the text of the menu bar item label is used.</summary>
    [Parameter] public string? MenuAccessibleName { get; set; }

    /// <summary>Gets or sets the space-separated NSS classes of the menu surface.</summary>
    [Parameter] public string? MenuClass { get; set; }

    /// <summary>Gets or sets the callback invoked when the menu opens or closes.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets the enclosing menu bar, supplied automatically.</summary>
    [CascadingParameter] public MenuBar? Bar { get; set; }

    /// <summary>Gets whether the menu is open.</summary>
    public bool Opened => open;

    /// <summary>Gets the NSS element name emitted by MenuBarItem.</summary>
    protected override string ElementName => "MenuBarItem";

    /// <summary>Validates the slots and registers with the bar.</summary>
    protected override void OnParametersSet()
    {
        if (Content is null) throw new InvalidOperationException("MenuBarItem requires Content.");
        if (Disabled && open) CloseLocal();
        if (registered == Bar) return;
        registered?.Unregister(this);
        (registered = Bar)?.Register(this);
    }

    /// <summary>Adds the role, the states and the keyboard and pointer behavior.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "menuitem");
        builder.AddAttribute(11, "disabled", Disabled);
        builder.AddAttribute(12, "focusable", !Disabled);
        builder.AddAttribute(13, "expanded", open);
        builder.AddAttribute(15, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, Toggle));
        builder.AddAttribute(16, "__internal_onkeydown#menubar", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(17, "__internal_onpointerenter#menubar", EventCallback.Factory.Create<PointerEventArgs>(this, PointerEnteredAsync));
        builder.AddAttribute(18, "__internal_onfocus#menubar", EventCallback.Factory.Create<FocusEventArgs>(this, () => Bar?.Focused(this)));
    }

    /// <summary>Adds the label and, while open, the menu.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "MenuBarItemLabel");
        builder.AddContent(1001, ChildContent);
        builder.CloseElement();
        builder.OpenComponent<PopoverContainer>(1100);
        builder.AddComponentParameter(1101, nameof(PopoverContainer.As), "Menu");
        builder.AddComponentParameter(1102, nameof(PopoverContainer.IsOpen), open);
        builder.AddComponentParameter(1103, nameof(PopoverContainer.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => { if (!value) CloseLocal(); }));
        builder.AddComponentParameter(1104, nameof(PopoverContainer.Anchor), Element);
        builder.AddComponentParameter(1105, nameof(PopoverContainer.Placement), OverlayPlacement.Bottom);
        builder.AddComponentParameter(1106, nameof(PopoverContainer.Role), "menu");
        builder.AddComponentParameter(1107, nameof(PopoverContainer.IsMenu), true);
        builder.AddComponentParameter(1108, nameof(PopoverContainer.InitialFocus), (Func<ElementReference?>)InitialItem);
        builder.AddComponentParameter(1109, nameof(Control.Class), MenuClass);
        var label = Element.Element?.Children.OfType<ElementNode>().FirstOrDefault(child => child.Name == "MenuBarItemLabel");
        builder.AddComponentParameter(1110, nameof(Control.AccessibleName), MenuAccessibleName ?? (label is null ? null : ControlTree.Text(label)));
        builder.AddComponentParameter(1111, "__internal_onkeydown#menubar", EventCallback.Factory.Create<KeyboardEventArgs>(this, MenuKeyDownAsync));
        builder.AddComponentParameter(1112, nameof(PopoverContainer.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<CascadingValue<IMenuOwner>>(0);
            content.AddComponentParameter(1, "Value", this);
            content.AddComponentParameter(2, "IsFixed", true);
            content.AddComponentParameter(3, "ChildContent", Content);
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }

    /// <summary>Opens the menu and closes the other menu of the bar.</summary>
    public void Open() => Open(false);

    internal void Open(bool last)
    {
        if (Disabled || open) return;
        open = true;
        openOnLast = last;
        Bar?.Opened(this);
        StateHasChanged();
        _ = IsOpenChanged.InvokeAsync(true);
    }

    /// <summary>Closes the menu.</summary>
    /// <returns>A completed task.</returns>
    public Task CloseAsync()
    {
        CloseLocal();
        return Task.CompletedTask;
    }

    internal void CloseLocal()
    {
        if (!open) return;
        open = false;
        Bar?.Closed(this);
        StateHasChanged();
        _ = IsOpenChanged.InvokeAsync(false);
    }

    private void Toggle()
    {
        if (open) CloseLocal();
        else Open(false);
    }

    private ElementReference? InitialItem()
    {
        if (!openOnLast || Element.Element is not { } self) return null;
        var menu = ControlTree.Descendants(self).FirstOrDefault(e => e.Attributes.GetValueOrDefault("overlay") is true && e.Attributes.GetValueOrDefault("role") is "menu");
        if (menu is null) return null;
        var last = ControlTree.Descendants(menu).LastOrDefault(e => ControlTree.IsFocusable(e) &&
            e.Attributes.GetValueOrDefault("role") is "menuitem" or "menuitemcheckbox" or "menuitemradio" &&
            ControlTree.Ancestors(e.ParentElement).First(a => a.Attributes.GetValueOrDefault("overlay") is true) == menu);
        return last is null ? null : new ElementReference(last);
    }

    private Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || Disabled || e.Modifiers != KeyModifiers.None) return Task.CompletedTask;
        if (!open && e.Key is "ArrowDown" or "ArrowUp")
        {
            e.Handled = true;
            Open(e.Key == "ArrowUp");
        }
        else if (open && e.Key is "ArrowLeft" or "ArrowRight" && Bar is { } bar)
        {
            e.Handled = true;
            return bar.MoveAsync(this, e.Key == "ArrowRight" ? 1 : -1);
        }
        return Task.CompletedTask;
    }

    private Task MenuKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || e.Modifiers != KeyModifiers.None || e.Key is not ("ArrowLeft" or "ArrowRight") || Bar is not { } bar) return Task.CompletedTask;
        e.Handled = true;
        return bar.MoveAsync(this, e.Key == "ArrowRight" ? 1 : -1);
    }

    private Task PointerEnteredAsync() =>
        !Disabled && Bar is { OpenItem: { } other } bar && other != this ? bar.SwitchAsync(this, false) : Task.CompletedTask;

    /// <summary>Leaves the bar.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }
}
