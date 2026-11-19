using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>How an application navigation pane uses its available width.</summary>
public enum NavigationViewPaneDisplayMode
{
    /// <summary>Minimal through the compact threshold, Compact below the expanded threshold, otherwise Left.</summary>
    Auto,
    /// <summary>An expanded left pane; a user collapse keeps a compact root strip.</summary>
    Left,
    /// <summary>A compact root strip that opens a covering pane.</summary>
    Compact,
    /// <summary>A toggle that opens a covering pane without reserving compact width.</summary>
    Minimal,
    /// <summary>A horizontal root strip above content, with scrolling and child menus.</summary>
    Top,
}

/// <summary>
/// Adaptive application navigation with stable keyed hierarchy and independently mounted application content.
/// Left navigation composes TreeView; compact and top roots compose roving buttons and Menu/Submenu.
/// Selection does not own routing: the application handles OnItemInvoked and supplies ChildContent.
/// </summary>
/// <typeparam name="TItem">The application navigation item type.</typeparam>
public sealed class NavigationView<TItem> : ContentControl where TItem : notnull
{
    private sealed class Entry(TItem item, object key)
    {
        public TItem Item = item;
        public readonly object Key = key;
        public string Text = "";
        public bool Disabled;
        public Entry? Parent;
        public IReadOnlyList<Entry> Children = [];
        public ElementReference Root;
    }

    private readonly Dictionary<object, Entry> entries = [];
    private readonly ControllableState<TItem?> selection = new(default);
    private readonly ControllableState<bool> open = new(true);
    private readonly RovingFocusController rootKeys = new() { Typeahead = true };
    private readonly IndicatorTracker<Entry> rootIndicator;
    private IReadOnlyList<Entry> roots = [];
    private TreeView<Entry>? tree;
    private ElementReference pane, treeHost;
    private Pressable? toggleControl;
    private bool paneFocused;
    private ItemBox? treeTarget;
    private Entry? menuEntry;
    private double availableWidth = double.PositiveInfinity;
    private double rootViewport;
    private NavigationViewPaneDisplayMode mode = NavigationViewPaneDisplayMode.Left;
    private bool reportPane, returnToToggle, focusRoot, focusTree, revealSelection;
    private bool treeReady, rootReady;

    /// <summary>Creates navigation with stable hierarchy and shared selection measurement.</summary>
    public NavigationView() => rootIndicator = new(StateHasChanged);

    /// <summary>Gets or sets top-level navigation items.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];
    /// <summary>Gets or sets known child items; null means all items are leaves.</summary>
    [Parameter] public Func<TItem, IReadOnlyList<TItem>?>? Children { get; set; }
    /// <summary>Gets or sets each item's plain text, including typeahead and accessible names.</summary>
    [Parameter] public Func<TItem, string>? ItemText { get; set; }
    /// <summary>Gets or sets leading icon content. Without it compact roots use the first text element.</summary>
    [Parameter] public RenderFragment<TItem>? ItemIcon { get; set; }
    /// <summary>Gets or sets stable, globally unique non-null item keys; item equality is used when null.</summary>
    [Parameter] public Func<TItem, object>? ItemKey { get; set; }
    /// <summary>Gets or sets whether individual navigation items reject interaction.</summary>
    [Parameter] public Func<TItem, bool>? ItemDisabled { get; set; }
    /// <summary>Gets or sets the selected application item; null selects none.</summary>
    [Parameter] public TItem? SelectedItem { get; set; }
    /// <summary>Gets or sets the callback reporting a different selected application item.</summary>
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }
    /// <summary>Gets or sets the primary pointer/Enter activation callback. The application owns routing and content changes.</summary>
    [Parameter] public EventCallback<TItem> OnItemInvoked { get; set; }
    /// <summary>Gets or sets the pane presentation; Auto by default. Explicit Top does not adapt to width.</summary>
    [Parameter] public NavigationViewPaneDisplayMode PaneDisplayMode { get; set; }
    /// <summary>Gets or sets the greatest automatic Minimal width; 640 by default.</summary>
    [Parameter] public double CompactModeThresholdWidth { get; set; } = 640;
    /// <summary>Gets or sets the least automatic expanded width; 1008 by default.</summary>
    [Parameter] public double ExpandedModeThresholdWidth { get; set; } = 1008;
    /// <summary>Gets or sets the open pane width; 320 logical units by default.</summary>
    [Parameter] public double OpenPaneLength { get; set; } = 320;
    /// <summary>Gets or sets the closed compact width; 48 logical units by default.</summary>
    [Parameter] public double CompactPaneLength { get; set; } = 48;
    /// <summary>Gets or sets whether the pane is open; true initially. Adaptive mode changes request the corresponding default.</summary>
    [Parameter] public bool IsPaneOpen { get; set; } = true;
    /// <summary>Gets or sets pane changes from toggling, dismissal or adaptation.</summary>
    [Parameter] public EventCallback<bool> IsPaneOpenChanged { get; set; }
    /// <summary>Gets or sets content in the persistent application header.</summary>
    [Parameter] public RenderFragment? Header { get; set; }
    /// <summary>Gets or sets persistent pane header content, hidden while compact/minimal is closed.</summary>
    [Parameter] public RenderFragment? PaneHeader { get; set; }
    /// <summary>Gets or sets persistent pane footer content, hidden while compact/minimal is closed.</summary>
    [Parameter] public RenderFragment? PaneFooter { get; set; }
    /// <summary>Gets or sets whether navigation actions are unavailable. Application content remains active.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Gets the resolved presentation; never Auto.</summary>
    public NavigationViewPaneDisplayMode EffectivePaneDisplayMode => mode;
    /// <summary>Gets whether the current pane is open.</summary>
    public bool PaneOpen => mode == NavigationViewPaneDisplayMode.Top || open.Value;
    /// <summary>Gets the current canonical application item, or default when unavailable.</summary>
    public TItem? CurrentItem => SelectedEntry is { } entry ? entry.Item : default;
    /// <summary>Gets the retained element name.</summary>
    protected override string ElementName => "NavigationView";
    /// <summary>Publishes pane dimensions before caller declarations.</summary>
    protected override string? ElementStyle => Css.Join("--nz-navigation-open-width: " + Css.Px(OpenPaneLength) + "; --nz-navigation-compact-width: " + Css.Px(CompactPaneLength), Style);

    private bool Top => mode == NavigationViewPaneDisplayMode.Top;
    private bool ShowTree => !Top && PaneOpen;
    private bool ShowRoots => Top || !PaneOpen && mode != NavigationViewPaneDisplayMode.Minimal;
    private bool Covers => PaneOpen && mode is NavigationViewPaneDisplayMode.Compact or NavigationViewPaneDisplayMode.Minimal;
    private bool PaneHidden => !Top && !PaneOpen && mode == NavigationViewPaneDisplayMode.Minimal;
    private Entry? SelectedEntry => selection.Value is { } value && entries.TryGetValue(KeyOf(value), out var entry) && !entry.Disabled ? entry : null;
    private Entry? SelectedRoot { get { var entry = SelectedEntry; while (entry?.Parent is { } parent) entry = parent; return entry; } }
    private object KeyOf(TItem item) => ItemKey is null ? item : ItemKey(item) ?? throw new InvalidOperationException("Navigation item keys cannot be null.");
    private NavigationViewPaneDisplayMode ResolveMode() => PaneDisplayMode != NavigationViewPaneDisplayMode.Auto ? PaneDisplayMode :
        availableWidth <= CompactModeThresholdWidth ? NavigationViewPaneDisplayMode.Minimal :
        availableWidth < ExpandedModeThresholdWidth ? NavigationViewPaneDisplayMode.Compact : NavigationViewPaneDisplayMode.Left;

    /// <summary>Validates dimensions and keys, refreshes the keyed hierarchy and synchronizes state.</summary>
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Items);
        if (!Enum.IsDefined(PaneDisplayMode)) throw new ArgumentOutOfRangeException(nameof(PaneDisplayMode));
        if (!double.IsFinite(CompactModeThresholdWidth) || CompactModeThresholdWidth < 0) throw new ArgumentOutOfRangeException(nameof(CompactModeThresholdWidth));
        if (!double.IsFinite(ExpandedModeThresholdWidth) || ExpandedModeThresholdWidth < CompactModeThresholdWidth) throw new ArgumentOutOfRangeException(nameof(ExpandedModeThresholdWidth));
        if (!double.IsFinite(CompactPaneLength) || CompactPaneLength < 0) throw new ArgumentOutOfRangeException(nameof(CompactPaneLength));
        if (!double.IsFinite(OpenPaneLength) || OpenPaneLength <= 0 || OpenPaneLength < CompactPaneLength) throw new ArgumentOutOfRangeException(nameof(OpenPaneLength));
        open.Sync(IsPaneOpen);
        revealSelection |= selection.Sync(SelectedItem);
        SynchronizeItems();
        SetMode(ResolveMode());
        if (Disabled || menuEntry is { } active && (active.Disabled || !entries.ContainsKey(active.Key) || !ShowRoots)) menuEntry = null;
    }

    private void SynchronizeItems()
    {
        var seen = new HashSet<object>();
        roots = Build(Items, null, 0);
        foreach (var key in entries.Keys.Where(key => !seen.Contains(key)).ToArray())
        {
            var removed = entries[key];
            if (tree is not null) _ = tree.Model.SetExpandedAsync(removed, false);
            rootIndicator.Forget(removed);
            entries.Remove(key);
        }
        return;
        IReadOnlyList<Entry> Build(IReadOnlyList<TItem> items, Entry? parent, int depth)
        {
            if (depth > 128) throw new InvalidOperationException("Navigation hierarchy exceeds 128 levels.");
            var result = new List<Entry>(items.Count);
            foreach (var item in items)
            {
                ArgumentNullException.ThrowIfNull(item);
                var key = KeyOf(item);
                if (!seen.Add(key)) throw new InvalidOperationException("Navigation items require globally unique keys and an acyclic hierarchy.");
                if (!entries.TryGetValue(key, out var entry)) entries.Add(key, entry = new(item, key));
                entry.Item = item; entry.Parent = parent;
                entry.Text = ItemText?.Invoke(item) ?? item.ToString() ?? "";
                entry.Disabled = parent?.Disabled == true || ItemDisabled?.Invoke(item) == true;
                entry.Children = Build(Children?.Invoke(item) ?? [], entry, depth + 1);
                result.Add(entry);
            }
            return result;
        }
    }

    private void SetMode(NavigationViewPaneDisplayMode value)
    {
        if (mode == value) return;
        var hadFocus = paneFocused || pane.Element?.States.HasFlag(ElementStates.FocusWithin) == true;
        var focused = pane.Element is { } paneNode ? ControlTree.Descendants(paneNode).FirstOrDefault(node => node.States.HasFlag(ElementStates.Focus)) : null;
        var slotFocus = ControlTree.Ancestors(focused).Any(node => node.Name is "NavigationViewPaneHeader" or "NavigationViewPaneFooter");
        mode = value; menuEntry = null;
        if (open.Set(value is NavigationViewPaneDisplayMode.Left or NavigationViewPaneDisplayMode.Top)) reportPane = true;
        if (hadFocus && !(slotFocus && PaneOpen)) { if (Top) focusRoot = true; else if (PaneOpen) { focusTree = true; revealSelection = true; } else returnToToggle = true; }
        treeReady = rootReady = false;
    }

    /// <summary>Adds adaptive state and scoped Escape handling.</summary>
    /// <param name="builder">The builder receiving attributes.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "display-mode", mode.ToString().ToLowerInvariant());
        builder.AddAttribute(12, "pane-open", PaneOpen);
        builder.AddAttribute(13, "compact", !Top && !PaneOpen);
        builder.AddAttribute(14, "tree-ready", treeReady);
        builder.AddAttribute(15, "roots-ready", rootReady);
        builder.AddAttribute(16, "focusable", true);
        if (TabIndex is null) builder.AddAttribute(17, "tabindex", -1);
        builder.AddAttribute(18, "__internal_onresize#navigation", EventCallback.Factory.Create<ResizeEventArgs>(this, e =>
        {
            if (!double.IsFinite(e.Width) || Math.Abs(availableWidth - e.Width) < .01) return;
            availableWidth = e.Width; SetMode(ResolveMode()); StateHasChanged();
        }));
        builder.AddAttribute(19, "__internal_onkeydown#navigation", EventCallback.Factory.Create<KeyboardEventArgs>(this, async e =>
        {
            if (e.Handled || e.Key != "Escape" || !Covers) return;
            e.Handled = true; await SetPaneOpenAsync(false);
        }));
    }

    /// <summary>Renders one persistent SplitView and application content across all modes.</summary>
    /// <param name="builder">The builder receiving content.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        RenderHeader(builder);
        builder.OpenComponent<SplitView>(2000);
        builder.AddComponentParameter(2001, nameof(SplitView.DisplayMode), Top || mode == NavigationViewPaneDisplayMode.Left && PaneOpen ? SplitViewDisplayMode.Inline : SplitViewDisplayMode.Compact);
        builder.AddComponentParameter(2002, nameof(SplitView.Orientation), Top ? Orientation.Vertical : Orientation.Horizontal);
        builder.AddComponentParameter(2003, nameof(SplitView.IsResizable), false);
        builder.AddComponentParameter(2004, nameof(SplitView.PaneSize), Top ? 48 : OpenPaneLength);
        builder.AddComponentParameter(2005, nameof(SplitView.CompactPaneSize), mode == NavigationViewPaneDisplayMode.Minimal ? 0 : CompactPaneLength);
        builder.AddComponentParameter(2006, nameof(SplitView.IsPaneOpen), PaneOpen);
        builder.AddComponentParameter(2007, nameof(SplitView.IsPaneOpenChanged), EventCallback.Factory.Create<bool>(this, SetPaneOpenAsync));
        builder.AddComponentParameter(2008, nameof(SplitView.Pane), (RenderFragment)RenderPane);
        builder.AddComponentParameter(2009, nameof(SplitView.ChildContent), (RenderFragment)(content =>
        {
            content.OpenElement(0, "NavigationViewHeader");
            content.AddAttribute(1, "empty", Header is null);
            content.AddContent(2, Header);
            content.CloseElement();
            content.OpenElement(10, "NavigationViewContent"); content.AddContent(11, ChildContent); content.CloseElement();
        }));
        builder.CloseComponent();
    }

    private void RenderHeader(RenderTreeBuilder builder)
    {
        builder.OpenElement(1000, "NavigationViewChrome");
        builder.AddAttribute(1001, "empty", Top && Header is null);
        builder.OpenComponent<Pressable>(1010);
        builder.AddComponentParameter(1011, nameof(Pressable.As), "NavigationViewToggle");
        builder.AddComponentParameter(1012, nameof(Pressable.Disabled), Disabled || Top);
        builder.AddComponentParameter(1013, nameof(Control.AccessibleName), ControlStrings.Get("ToggleNavigationPane"));
        builder.AddComponentParameter(1014, nameof(Control.AdditionalAttributes), new Dictionary<string, object?> { ["expanded"] = PaneOpen });
        builder.AddComponentParameter(1015, nameof(Pressable.OnPress), EventCallback.Factory.Create<PointerEventArgs>(this, () => TogglePaneAsync()));
        builder.AddComponentParameter(1016, nameof(Pressable.ChildContent), (RenderFragment)(content => content.AddContent(0, "☰")));
        builder.AddComponentReferenceCapture(1017, component => toggleControl = (Pressable)component);
        builder.CloseComponent();
        builder.CloseElement();
    }

    private void RenderPane(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "NavigationViewPane");
        builder.AddAttribute(1, "role", "group");
        builder.AddAttribute(2, "accessible-name", ControlStrings.Get("Navigation"));
        builder.AddAttribute(3, "disabled", Disabled || PaneHidden);
        builder.AddAttribute(4, "semantic-hidden", PaneHidden);
        builder.AddAttribute(5, "__internal_onfocusin#navigation", EventCallback.Factory.Create<FocusEventArgs>(this, () => paneFocused = true));
        builder.AddAttribute(6, "__internal_onfocusout#navigation", EventCallback.Factory.Create<FocusEventArgs>(this, e =>
        {
            if (e.RelatedTarget is not null && !ControlTree.Ancestors(e.RelatedTarget).Contains(pane.Element!))
            {
                if (Covers && e.RelatedTarget == Element.Element) { returnToToggle = true; _ = SetPaneOpenAsync(false); return; }
                paneFocused = false;
                if (Covers && !ControlTree.Ancestors(e.RelatedTarget).Contains(toggleControl?.Element.Element!)) _ = SetPaneOpenAsync(false);
            }
        }));
        builder.AddElementReferenceCapture(7, reference => pane = reference);
        Slot(builder, 10, "NavigationViewPaneHeader", PaneHeader);
        builder.OpenElement(20, "NavigationViewTreeHost");
        builder.AddAttribute(21, "style", ShowTree ? null : "display: none");
        builder.AddAttribute(22, "disabled", !ShowTree || Disabled);
        builder.AddAttribute(23, "semantic-hidden", !ShowTree);
        builder.AddAttribute(24, "__internal_onresize#navigation-selection", EventCallback.Factory.Create<ResizeEventArgs>(this, () => RefreshTreeIndicator()));
        builder.AddAttribute(25, "__internal_onscroll#navigation-selection", EventCallback.Factory.Create<ScrollEventArgs>(this, () => RefreshTreeIndicator()));
        builder.AddElementReferenceCapture(26, reference => treeHost = reference);
        builder.OpenComponent<TreeView<Entry>>(30);
        builder.AddComponentParameter(31, nameof(TreeView<Entry>.Roots), roots);
        builder.AddComponentParameter(32, nameof(TreeView<Entry>.Children), (Func<Entry, IReadOnlyList<Entry>?>)(entry => entry.Children));
        builder.AddComponentParameter(33, nameof(TreeView<Entry>.ItemText), (Func<Entry, string>)(entry => entry.Text));
        builder.AddComponentParameter(34, nameof(TreeView<Entry>.ItemDisabled), (Func<Entry, bool>)(entry => entry.Disabled));
        builder.AddComponentParameter(35, nameof(TreeView<Entry>.SelectedItem), SelectedEntry);
        builder.AddComponentParameter(36, nameof(TreeView<Entry>.SelectedItemChanged), EventCallback.Factory.Create<Entry?>(this, TreeSelectedAsync));
        builder.AddComponentParameter(37, nameof(TreeView<Entry>.Disabled), Disabled || !ShowTree);
        builder.AddComponentParameter(38, nameof(TreeView<Entry>.ItemTemplate), (RenderFragment<Entry>)(entry => content => RenderTreeContent(content, entry)));
        builder.AddComponentParameter(39, nameof(Control.AccessibleName), ControlStrings.Get("Navigation"));
        builder.AddComponentParameter(40, nameof(Control.AdditionalAttributes), new Dictionary<string, object?>
        {
            ["__internal_onclick#navigation-invoke"] = EventCallback.Factory.Create<PointerEventArgs>(this, TreeClickedAsync),
            ["__internal_onkeydown"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, TreeKeyDownAsync),
        });
        builder.AddComponentReferenceCapture(41, component => { tree = (TreeView<Entry>)component; tree.AllowEmptySelection = false; });
        builder.CloseComponent();
        RenderIndicator(builder, 50, "NavigationViewTreeIndicator", treeTarget, forTree: true);
        builder.CloseElement();
        RenderRoots(builder);
        Slot(builder, 200, "NavigationViewPaneFooter", PaneFooter);
        builder.CloseElement();
    }

    private void Slot(RenderTreeBuilder builder, int sequence, string name, RenderFragment? content)
    {
        builder.OpenElement(sequence, name);
        builder.AddAttribute(sequence + 1, "disabled", Disabled || !PaneOpen);
        builder.AddAttribute(sequence + 2, "semantic-hidden", !PaneOpen);
        builder.AddContent(sequence + 3, content);
        builder.CloseElement();
    }

    private void RenderTreeContent(RenderTreeBuilder builder, Entry entry)
    {
        builder.OpenElement(0, "NavigationViewItemContent");
        builder.AddAttribute(1, "__internal_onresize#navigation-selection", EventCallback.Factory.Create<ResizeEventArgs>(this, () => RefreshTreeIndicator()));
        IconAndLabel(builder, entry);
        builder.CloseElement();
    }

    private void IconAndLabel(RenderTreeBuilder builder, Entry entry)
    {
        builder.OpenElement(10, "NavigationViewItemIcon");
        builder.AddAttribute(11, "semantic-hidden", true);
        if (ItemIcon is not null) builder.AddContent(12, ItemIcon(entry.Item));
        else
        {
            var text = entry.Text.Trim();
            builder.AddContent(13, text.Length == 0 ? "•" : StringInfo.GetNextTextElement(text));
        }
        builder.CloseElement();
        builder.OpenElement(20, "NavigationViewItemLabel"); builder.AddContent(21, entry.Text); builder.CloseElement();
    }

    private void RenderRoots(RenderTreeBuilder builder)
    {
        builder.OpenElement(100, "NavigationViewRootScroller");
        builder.AddAttribute(101, "style", Css.Join(ShowRoots ? null : "display: none", "--nz-navigation-root-viewport: " + Css.Px(rootViewport)));
        builder.AddAttribute(102, "disabled", Disabled || !ShowRoots);
        builder.AddAttribute(103, "semantic-hidden", !ShowRoots);
        builder.AddAttribute(104, "__internal_onresize#navigation-viewport", EventCallback.Factory.Create<ResizeEventArgs>(this, e =>
        {
            if (!double.IsFinite(e.Width) || Math.Abs(rootViewport - e.Width) < .01) return;
            rootViewport = e.Width; StateHasChanged();
        }));
        builder.OpenElement(110, "NavigationViewRoots");
        builder.AddAttribute(111, "role", "group");
        builder.AddAttribute(112, "__internal_onkeydown#navigation-roots", EventCallback.Factory.Create<KeyboardEventArgs>(this, RootKeyDownAsync));
        var current = SelectedRoot ?? roots.FirstOrDefault(entry => !entry.Disabled);
        foreach (var entry in roots)
        {
            builder.OpenElement(120, "NavigationViewRootItem"); builder.SetKey(entry.Key);
            builder.AddAttribute(121, "role", "button");
            builder.AddAttribute(122, "focusable", ShowRoots && !Disabled && !entry.Disabled);
            builder.AddAttribute(123, "disabled", Disabled || entry.Disabled || !ShowRoots);
            builder.AddAttribute(124, "tabindex", entry == current ? 0 : -1);
            builder.AddAttribute(125, "selected", entry == SelectedRoot);
            builder.AddAttribute(126, "expanded", entry.Children.Count == 0 ? (bool?)null : menuEntry == entry);
            builder.AddAttribute(127, "accessible-name", entry.Text);
            builder.AddAttribute(128, "tooltip", !Top ? entry.Text : null);
            builder.AddAttribute(129, "index", roots.ToList().IndexOf(entry));
            var measure = rootIndicator.Attributes(this, entry); builder.AddAttribute(130, measure.Key, measure.Value);
            builder.AddAttribute(131, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this,
                () => entry.Children.Count == 0 ? ChooseAsync(entry) : OpenMenuAsync(entry)));
            builder.AddElementReferenceCapture(132, reference => entry.Root = reference);
            IconAndLabel(builder, entry);
            if (entry.Children.Count > 0)
            {
                builder.OpenElement(140, "NavigationViewRootChevron");
                builder.AddAttribute(141, "glyph", Top ? "chevron-down" : "chevron-right");
                builder.AddAttribute(142, "semantic-hidden", true); builder.CloseElement();
            }
            builder.CloseElement();
            builder.OpenComponent<Menu>(150); builder.SetKey(entry);
            builder.AddComponentParameter(151, nameof(Menu.IsOpen), menuEntry == entry && ShowRoots);
            builder.AddComponentParameter(152, nameof(Menu.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value =>
            {
                if (!value && menuEntry == entry) { menuEntry = null; StateHasChanged(); }
            }));
            builder.AddComponentParameter(153, nameof(Menu.Anchor), entry.Root);
            builder.AddComponentParameter(154, nameof(Menu.Placement), Top ? OverlayPlacement.Bottom : OverlayPlacement.Right);
            builder.AddComponentParameter(155, nameof(Menu.AccessibleName), entry.Text);
            builder.AddComponentParameter(156, nameof(Menu.Class), "NavigationViewChildMenu");
            builder.AddComponentParameter(157, nameof(Menu.ChildContent), (RenderFragment)(content => RenderMenu(content, entry.Children)));
            builder.CloseComponent();
        }
        RenderIndicator(builder, 180, "NavigationViewRootIndicator", rootIndicator.BoxOf(SelectedRoot), forTree: false);
        builder.CloseElement(); builder.CloseElement();
    }

    private void RenderMenu(RenderTreeBuilder builder, IReadOnlyList<Entry> items)
    {
        foreach (var entry in items)
        {
            builder.OpenComponent(0, entry.Children.Count == 0 ? typeof(MenuItem) : typeof(Submenu)); builder.SetKey(entry.Key);
            builder.AddComponentParameter(1, nameof(MenuItemBase.Disabled), Disabled || entry.Disabled);
            builder.AddComponentParameter(2, nameof(MenuItemBase.Leading), ItemIcon is null ? null : (RenderFragment)(content => content.AddContent(0, ItemIcon(entry.Item))));
            builder.AddComponentParameter(3, nameof(MenuItemBase.ChildContent), (RenderFragment)(content => content.AddContent(0, entry.Text)));
            if (entry.Children.Count == 0) builder.AddComponentParameter(4, nameof(MenuItemBase.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => ChooseAsync(entry)));
            else builder.AddComponentParameter(5, nameof(Submenu.Content), (RenderFragment)(content => RenderMenu(content, entry.Children)));
            builder.CloseComponent();
        }
    }

    private void RenderIndicator(RenderTreeBuilder builder, int sequence, string name, ItemBox? target, bool forTree)
    {
        builder.OpenComponent<Indicator>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(Indicator.As), name);
        builder.AddComponentParameter(sequence + 2, nameof(Indicator.Target), target);
        builder.AddComponentParameter(sequence + 3, nameof(Control.AdditionalAttributes), new Dictionary<string, object?>
        {
            ["__internal_onresize#navigation-ready"] = EventCallback.Factory.Create<ResizeEventArgs>(this, e =>
            {
                if (e.Width <= 0 || e.Height <= 0 || (forTree ? treeReady : rootReady)) return;
                if (forTree) treeReady = true; else rootReady = true;
                StateHasChanged();
            }),
        });
        builder.CloseComponent();
    }

    private void RefreshTreeIndicator()
    {
        ItemBox? next = null;
        if (ShowTree && SelectedEntry is { } entry && tree?.Element.Element is { } root)
        {
            var index = tree.VisibleRows.ToList().FindIndex(row => row.Item == entry);
            var row = ControlTree.Descendants(root).FirstOrDefault(node => node.Name == "TreeViewItem" && node.Attributes.GetValueOrDefault("index") is int value && value == index);
            if (row is not null && new ElementReference(row).GetBounds() is { Width: > 0, Height: > 0 } bounds &&
                treeHost.GetLocalPoint(bounds.X, bounds.Y) is { } start && treeHost.GetLocalPoint(bounds.X + bounds.Width, bounds.Y + bounds.Height) is { } end)
                next = new(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
        }
        if (next == treeTarget) return;
        treeTarget = next;
        if (next is null) treeReady = false;
        StateHasChanged();
    }

    private Entry? EntryOf(ElementNode? target)
    {
        if (tree is null) return null;
        var row = ControlTree.Ancestors(target).FirstOrDefault(node => node.Name == "TreeViewItem");
        if (row is null || ControlTree.Ancestors(row.ParentElement).FirstOrDefault(node => node.Name == "TreeView") != tree.Element.Element) return null;
        return row.Attributes.GetValueOrDefault("index") is int index && index >= 0 && index < tree.VisibleRows.Count ? tree.VisibleRows[index].Item : null;
    }

    private Task TreeClickedAsync(PointerEventArgs e)
    {
        var ancestors = ControlTree.Ancestors(e.OriginalTarget).TakeWhile(node => node.Name != "TreeViewItem");
        if (ancestors.Any(node => node.Name == "TreeViewExpander" || node.Attributes.GetValueOrDefault("focusable") is true || node.Attributes.GetValueOrDefault("editable") is true)) return Task.CompletedTask;
        return EntryOf(e.OriginalTarget) is { } entry ? ChooseAsync(entry) : Task.CompletedTask;
    }

    private Task TreeSelectedAsync(Entry? entry) => entry is null ? Task.CompletedTask : SetSelectionAsync(entry);

    private Task TreeKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || e.Key != "Enter" || e.Modifiers != KeyModifiers.None || tree?.Element.Element is not { } root) return Task.CompletedTask;
        var focused = ControlTree.Descendants(root).FirstOrDefault(node => node.States.HasFlag(ElementStates.Focus));
        if (focused?.Name != "TreeViewItem" || EntryOf(focused) is not { } entry) return Task.CompletedTask;
        e.Handled = true; return ChooseAsync(entry);
    }

    private async Task RootKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || Disabled || !ShowRoots) return;
        var at = roots.ToList().FindIndex(entry => entry.Root.Element?.States.HasFlag(ElementStates.Focus) == true);
        if (at < 0) return;
        if (e.Modifiers == KeyModifiers.None && e.Key == (Top ? "ArrowDown" : "ArrowRight") && roots[at].Children.Count > 0)
        {
            e.Handled = true; await OpenMenuAsync(roots[at]); return;
        }
        rootKeys.Orientation = Top ? Orientation.Horizontal : Orientation.Vertical;
        if (rootKeys.Move(e, at, roots.Count, index => roots[index].Disabled, index => roots[index].Text) is not { } next) return;
        e.Handled = true;
        await roots[next].Root.FocusAsync();
    }

    private Task OpenMenuAsync(Entry entry)
    {
        if (Disabled || entry.Disabled || !entries.ContainsKey(entry.Key)) return Task.CompletedTask;
        menuEntry = menuEntry == entry ? null : entry; StateHasChanged(); return Task.CompletedTask;
    }

    private async Task SetSelectionAsync(Entry entry)
    {
        if (Disabled || entry.Disabled || !entries.TryGetValue(entry.Key, out var current) || current != entry) return;
        if (!selection.Set(entry.Item)) return;
        revealSelection = true; StateHasChanged();
        await SelectedItemChanged.InvokeAsync(entry.Item);
        await Events.RaiseChangeAsync(Element, entry.Item);
    }

    private async Task ChooseAsync(Entry entry)
    {
        if (Disabled || entry.Disabled || !entries.TryGetValue(entry.Key, out var current) || current != entry) return;
        var invocation = OnItemInvoked.InvokeAsync(entry.Item);
        var changed = SetSelectionAsync(entry);
        if (Covers && entry.Children.Count == 0) await SetPaneOpenAsync(false);
        await Task.WhenAll(invocation, changed);
    }

    /// <summary>Opens or closes the pane and reports the change. Top remains visible; disabling navigation prevents opening.</summary>
    /// <param name="value">The requested pane state.</param>
    /// <returns>A task completing after the pane callback.</returns>
    public async Task SetPaneOpenAsync(bool value)
    {
        if (Top || Disabled && value || !open.Set(value)) return;
        if (!value) { returnToToggle |= paneFocused; menuEntry = null; }
        StateHasChanged(); await IsPaneOpenChanged.InvokeAsync(value);
    }

    /// <summary>Toggles the current non-Top pane.</summary>
    /// <returns>A task completing after the pane callback.</returns>
    public Task TogglePaneAsync() => SetPaneOpenAsync(!PaneOpen);

    /// <summary>Refreshes in-place item mutations while preserving stable keyed expansion and application content.</summary>
    public void Refresh() { SynchronizeItems(); tree?.Refresh(); StateHasChanged(); }

    /// <summary>Restores hidden pane focus, reveals the selected hierarchy and focused root, and refreshes selection geometry.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (revealSelection && SelectedEntry is { } selected && tree is not null)
        {
            revealSelection = false; tree.Reveal(selected);
        }
        if (returnToToggle && !Top && toggleControl is not null)
        {
            returnToToggle = false; paneFocused = false;
            if (toggleControl.Element.Element is { IsDisabled: false }) _ = toggleControl.Element.FocusAsync(true);
        }
        if (focusRoot && ShowRoots)
        {
            focusRoot = false;
            if ((SelectedRoot ?? roots.FirstOrDefault(entry => !entry.Disabled)) is { } entry) _ = entry.Root.FocusAsync();
            else _ = Element.FocusAsync(true);
        }
        if (ShowRoots && !Disabled && roots.FirstOrDefault(entry => entry.Root.Element?.States.HasFlag(ElementStates.Focus) == true) is { } focusedRoot)
            focusedRoot.Root.ScrollIntoView();
        if (focusTree && ShowTree && tree?.Element.Element is { } treeRoot)
        {
            var index = SelectedEntry is { } selectedEntry ? tree.VisibleRows.ToList().FindIndex(row => row.Item == selectedEntry) : -1;
            var target = ControlTree.Descendants(treeRoot).FirstOrDefault(node => node.Name == "TreeViewItem" && !node.IsDisabled &&
                (index < 0 || node.Attributes.GetValueOrDefault("index") is int rowIndex && rowIndex == index));
            if (target is not null) { focusTree = false; _ = new ElementReference(target).FocusAsync(true); }
            else if (tree.VisibleRows.Count == 0) { focusTree = false; returnToToggle = true; StateHasChanged(); }
        }
        RefreshTreeIndicator();
    }

    /// <summary>Reports adaptive pane-state requests after rendering.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>A task completing after the pane callback.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!reportPane) return;
        reportPane = false; await IsPaneOpenChanged.InvokeAsync(open.Value);
    }
}
