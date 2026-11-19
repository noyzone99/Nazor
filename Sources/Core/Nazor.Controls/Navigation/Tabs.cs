using System.Globalization;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>Where the tab list of <see cref="Tabs" /> sits relative to the panels.</summary>
public enum TabsPlacement
{
    /// <summary>Above the panels.</summary>
    Top,
    /// <summary>Below the panels.</summary>
    Bottom,
    /// <summary>Before the panels, on the leading side, as a vertical list.</summary>
    Start,
    /// <summary>After the panels, on the trailing side, as a vertical list.</summary>
    End,
}

/// <summary>What <see cref="Tabs" /> does with tabs that do not fit in the list.</summary>
public enum TabsOverflow
{
    /// <summary>The list scrolls; scroll buttons appear at its ends while some tabs are out of view.</summary>
    Scroll,
    /// <summary>The tabs that do not fit move into a "More" menu; horizontal lists only, vertical lists scroll.</summary>
    Menu,
    /// <summary>Nothing: the list keeps its natural size and the theme decides (for example wrapping).</summary>
    None,
}

/// <summary>The relationship between a tab strip and its content.</summary>
public enum TabsPresentation
{
    /// <summary>Navigation between related sections of the same view.</summary>
    Navigation,
    /// <summary>Independent documents with a selected sheet surface.</summary>
    Document,
}

/// <summary>How document-tab headers use the available strip width.</summary>
public enum TabWidthMode
{
    /// <summary>Equal widths within theme-defined minimum and maximum bounds.</summary>
    Equal,
    /// <summary>Natural header widths within theme-defined bounds.</summary>
    SizeToContent,
    /// <summary>Natural selected width; other headers hide label and badge while keeping their accessible name.</summary>
    Compact,
}

/// <summary>A request to move a tab outside its current strip. The application owns removal, data transfer and window creation.</summary>
/// <param name="Tab">The stable tab declaration that was dragged.</param>
/// <param name="X">The release's horizontal coordinate in logical window units.</param>
/// <param name="Y">The release's vertical coordinate in logical window units.</param>
public readonly record struct TabDragOutRequest(Tab Tab, double X, double Y);

/// <summary>
/// Pages behind tabs. The <see cref="Tab" /> elements written inside become tabs of a <c>TabsList</c> (role
/// <c>tablist</c>) and the content of the selected one shows in a <c>TabsPanel</c> (role <c>tabpanel</c>, labeled by its
/// tab). The selection is <see cref="SelectedIndex" /> (<c>@bind-SelectedIndex</c>) and, for tabs with a
/// <see cref="Tab.Key" />, <see cref="SelectedKey" /> (<c>@bind-SelectedKey</c>). The list is one Tab stop: the arrows,
/// Home and End move between the enabled tabs and select them, Ctrl+Tab and Ctrl+Page Down select the next tab from
/// anywhere inside, Ctrl+Shift+Tab and Ctrl+Page Up the previous one, Delete closes a closable tab. A
/// <c>TabsIndicator</c> follows the selected tab. Panels are rendered when first selected; with
/// <see cref="KeepMounted" /> they stay rendered, hidden, afterwards. The parts are <c>TabsHeader</c>,
/// <c>TabsScroller</c>, <c>TabsList</c>, <c>TabsScrollBack</c>, <c>TabsScrollForward</c>, <c>TabsOverflow</c> (the
/// "More" button), <c>TabsAdd</c>, <c>TabsIndicator</c> and <c>TabsPanel</c>; the element carries <c>placement</c>,
/// <c>orientation</c>, <c>overflow</c>, <c>size</c>, <c>presentation</c>, <c>width-mode</c>, <c>reorderable</c>,
/// <c>dragging</c> and <c>indicator-ready</c>. <see cref="Presentation"/> chooses section navigation or document sheets.
/// Opt-in reorder and drag-out requests require stable unique tab keys and application-owned keyed declarations.
/// Ctrl+T invokes an available add action; Ctrl+F4 closes an enabled closable selected tab with a handler.
/// </summary>
public sealed class Tabs : Control, IDisposable
{
    private readonly List<Tab> tabs = [];
    private readonly HashSet<Tab> visited = [];
    private readonly ControllableState<int> index = new(0);
    private readonly ControllableState<string?> key = new(null);
    private IndicatorTracker<Tab>? tracker;
    private ElementReference scroller;
    private ElementReference moreButton;
    private double scrollOffset;
    private double viewport;
    private double extent;
    private bool reveal;
    private bool menuOpen;
    private readonly ItemReorderController reorder;
    private ElementReference header;
    private Tab? pendingFocus;
    private bool reportSelection;
    private bool reportKey;
    private bool disposed;
    private bool focusEmpty;
    private ElementReference addButton;
    private bool indicatorReady;

    /// <summary>Creates a tab strip with shared pointer and keyboard reorder cancellation.</summary>
    public Tabs()
    {
        reorder = new(() => Element, "Tab", position => position >= 0 && position < tabs.Count ? tabs[position].Key : null,
            CanMove, ReorderAsync, () => { if (!disposed) StateHasChanged(); });
        reorder.DropBounds = () => header.GetBounds();
        reorder.DropOutside = DragOutAsync;
    }

    /// <summary>Gets or sets the <see cref="Tab" /> declarations.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the zero-based index of the selected tab.</summary>
    [Parameter] public int SelectedIndex { get; set; }

    /// <summary>Gets or sets the callback invoked when the user selects another tab, with its index.</summary>
    [Parameter] public EventCallback<int> SelectedIndexChanged { get; set; }

    /// <summary>Gets or sets the <see cref="Tab.Key" /> of the selected tab; when it matches a tab it wins over <see cref="SelectedIndex" />.</summary>
    [Parameter] public string? SelectedKey { get; set; }

    /// <summary>Gets or sets the callback invoked when the user selects another tab, with its key.</summary>
    [Parameter] public EventCallback<string?> SelectedKeyChanged { get; set; }

    /// <summary>Gets or sets where the tab list sits relative to the panels.</summary>
    [Parameter] public TabsPlacement Placement { get; set; }

    /// <summary>Gets or sets the direction of the tab list; null follows <see cref="Placement" /> (vertical at the start and end).</summary>
    [Parameter] public Orientation? Orientation { get; set; }

    /// <summary>Gets or sets what happens to tabs that do not fit.</summary>
    [Parameter] public TabsOverflow Overflow { get; set; }

    /// <summary>Gets or sets whether panels stay rendered, hidden, after they were first selected.</summary>
    [Parameter] public bool KeepMounted { get; set; }

    /// <summary>Gets or sets the size step.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets navigation versus document presentation; Navigation by default.</summary>
    [Parameter] public TabsPresentation Presentation { get; set; }

    /// <summary>Gets or sets how document headers use strip width; Equal by default. Themes own the width limits.</summary>
    [Parameter] public TabWidthMode WidthMode { get; set; }

    /// <summary>Gets or sets whether the strip shows its add button. The application creates the new tab in <see cref="OnAdd"/>.</summary>
    [Parameter] public bool ShowAddButton { get; set; }

    /// <summary>Gets or sets the callback for the add button or Ctrl+T. Without a callback the button is disabled.</summary>
    [Parameter] public EventCallback OnAdd { get; set; }

    /// <summary>Gets or sets whether dragging and Ctrl+Shift+arrows can request a move; requires <see cref="OnTabReorder"/> and stable unique non-null tab keys.</summary>
    [Parameter] public bool CanReorderTabs { get; set; }

    /// <summary>Gets or sets a reorder request. The application moves its keyed declarations; the supplied index is the final index after removal.</summary>
    [Parameter] public EventCallback<ItemReorderRequest<Tab>> OnTabReorder { get; set; }

    /// <summary>Gets or sets an optional release-outside-strip request, independently of reordering. It does not transfer a live component or start OS drag-and-drop.</summary>
    [Parameter] public EventCallback<TabDragOutRequest> OnTabDragOut { get; set; }

    /// <summary>Gets the index of the selected tab, or -1 when no tab can be selected.</summary>
    public int CurrentIndex => Current();

    /// <summary>Gets the tabs in declaration order.</summary>
    public IReadOnlyList<Tab> Items => tabs;

    /// <summary>Gets the direction of the tab list.</summary>
    public Orientation EffectiveOrientation => Orientation ?? (Placement is TabsPlacement.Start or TabsPlacement.End ? Controls.Orientation.Vertical : Controls.Orientation.Horizontal);

    /// <summary>Gets the NSS element name emitted by Tabs.</summary>
    protected override string ElementName => "Tabs";

    /// <summary>Publishes the realized tab count for theme width calculation.</summary>
    protected override string? ElementStyle => Css.Join("--nz-tabs-count: " + tabs.Count.ToString(CultureInfo.InvariantCulture), Style);

    internal long DeclarationVersion { get; private set; }

    private bool Vertical => EffectiveOrientation == Controls.Orientation.Vertical;

    private bool UsesMenu => Overflow == TabsOverflow.Menu && !Vertical;

    private bool Overflowing => Overflow == TabsOverflow.Scroll && extent > viewport + 0.5 && viewport > 0;

    /// <summary>Validates the parameters and synchronizes the selection.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (Orientation is { } orientation && !Enum.IsDefined(orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        if (!Enum.IsDefined(Overflow)) throw new ArgumentOutOfRangeException(nameof(Overflow));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (!Enum.IsDefined(Presentation)) throw new ArgumentOutOfRangeException(nameof(Presentation));
        if (!Enum.IsDefined(WidthMode)) throw new ArgumentOutOfRangeException(nameof(WidthMode));
        DeclarationVersion++;
        tracker ??= new IndicatorTracker<Tab>(StateHasChanged);
        if (index.Sync(SelectedIndex)) reveal = true;
        if (key.Sync(SelectedKey)) reveal = true;
        if (reorder.Horizontal != !Vertical) reorder.Cancel();
        reorder.Horizontal = !Vertical;
        reorder.Enabled = CanReorderTabs && OnTabReorder.HasDelegate || OnTabDragOut.HasDelegate;
        reorder.Synchronize();
    }

    private int Current()
    {
        if (tabs.Count == 0) return -1;
        var wanted = key.Value is { } k && tabs.FindIndex(tab => tab.Key == k) is var found and >= 0 ? found : index.Value;
        if (wanted >= 0 && wanted < tabs.Count && !tabs[wanted].Disabled) return wanted;
        return tabs.FindIndex(tab => !tab.Disabled);
    }

    /// <summary>Adds the layout attributes and the keyboard shortcuts.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "placement", Placement.ToString().ToLowerInvariant());
        builder.AddAttribute(11, "orientation", Vocabulary.Name(EffectiveOrientation));
        builder.AddAttribute(12, "overflow", Overflow.ToString().ToLowerInvariant());
        builder.AddAttribute(13, "size", Vocabulary.Name(Size));
        builder.AddAttribute(14, "__internal_onkeydown#tabs", EventCallback.Factory.Create<KeyboardEventArgs>(this, KeyDownAsync));
        builder.AddAttribute(15, "presentation", Presentation.ToString().ToLowerInvariant());
        builder.AddAttribute(16, "width-mode", WidthMode.ToString().ToLowerInvariant());
        builder.AddAttribute(17, "reorderable", CanReorderTabs && OnTabReorder.HasDelegate);
        builder.AddAttribute(18, "dragging", reorder.IsDragging);
        builder.AddAttribute(19, "__internal_onpointermove#tabs", EventCallback.Factory.Create<PointerEventArgs>(this, reorder.Move));
        builder.AddAttribute(20, "__internal_onpointerup#tabs", EventCallback.Factory.Create<PointerEventArgs>(this, reorder.UpAsync));
        builder.AddAttribute(21, "__internal_onlostpointercapture#tabs", EventCallback.Factory.Create<PointerEventArgs>(this, reorder.Lost));
        builder.AddAttribute(22, "focusable", true);
        if (TabIndex is null) builder.AddAttribute(23, "tabindex", -1);
        builder.AddAttribute(25, "indicator-ready", indicatorReady);
    }

    /// <summary>Adds the header, the panels and the declarations.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var current = Current();
        if (current >= 0) visited.Add(tabs[current]);
        var headerFirst = Placement is TabsPlacement.Top or TabsPlacement.Start;
        if (headerFirst)
        {
            builder.OpenRegion(1000);
            RenderHeader(builder, current);
            builder.CloseRegion();
        }
        builder.OpenRegion(1100);
        RenderPanels(builder, current);
        builder.CloseRegion();
        if (!headerFirst)
        {
            builder.OpenRegion(1200);
            RenderHeader(builder, current);
            builder.CloseRegion();
        }
        builder.OpenComponent<CascadingValue<Tabs>>(1300);
        builder.AddComponentParameter(1301, "Value", this);
        builder.AddComponentParameter(1302, "IsFixed", true);
        builder.AddComponentParameter(1303, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    private void RenderHeader(RenderTreeBuilder builder, int current)
    {
        builder.OpenElement(0, "TabsHeader");
        builder.AddAttribute(1, "orientation", Vocabulary.Name(EffectiveOrientation));
        builder.AddAttribute(2, "overflowing", Overflowing);
        builder.AddElementReferenceCapture(3, reference => header = reference);
        if (UsesMenu)
        {
            var selected = current >= 0 ? tabs[current] : null;
            builder.OpenComponent<OverflowContainer<Tab>>(10);
            builder.AddComponentParameter(11, nameof(OverflowContainer<Tab>.As), "TabsList");
            builder.AddComponentParameter(12, nameof(OverflowContainer<Tab>.Items), tabs.ToArray());
            builder.AddComponentParameter(13, nameof(OverflowContainer<Tab>.Role), "tablist");
            builder.AddComponentParameter(14, nameof(OverflowContainer<Tab>.Priority), (Func<Tab, int>)(tab => tab == selected ? int.MaxValue : 0));
            builder.AddComponentParameter(15, nameof(OverflowContainer<Tab>.ItemAttributes), (Func<Tab, IEnumerable<KeyValuePair<string, object?>>?>)(tab => [tracker!.Attributes(this, tab)]));
            builder.AddComponentParameter(16, nameof(OverflowContainer<Tab>.ItemTemplate), (RenderFragment<Tab>)(tab => b => RenderTab(b, tab, current, false)));
            builder.AddComponentParameter(17, nameof(OverflowContainer<Tab>.OverflowTemplate), (RenderFragment<IReadOnlyList<Tab>>)(hidden => b => RenderMore(b, hidden)));
            builder.AddComponentParameter(18, nameof(OverflowContainer<Tab>.ChildContent), (RenderFragment)(b => RenderIndicator(b, selected)));
            builder.AddComponentParameter(19, nameof(Control.AdditionalAttributes), new Dictionary<string, object?> { ["orientation"] = "horizontal" });
            builder.CloseComponent();
        }
        else
        {
            if (Overflowing) RenderScrollButton(builder, 20, back: true);
            builder.OpenElement(30, "TabsScroller");
            builder.AddAttribute(31, "orientation", Vocabulary.Name(EffectiveOrientation));
            builder.AddAttribute(32, "__internal_onscroll#tabs", EventCallback.Factory.Create<ScrollEventArgs>(this, Scrolled));
            builder.AddAttribute(33, "__internal_onresize#tabs", EventCallback.Factory.Create<ResizeEventArgs>(this, e => Measure(ref viewport, Vertical ? e.Height : e.Width)));
            builder.AddElementReferenceCapture(34, reference => scroller = reference);
            builder.OpenElement(40, "TabsList");
            builder.AddAttribute(41, "role", "tablist");
            builder.AddAttribute(42, "orientation", Vocabulary.Name(EffectiveOrientation));
            builder.AddAttribute(43, "__internal_onresize#tabs", EventCallback.Factory.Create<ResizeEventArgs>(this, e => Measure(ref extent, Vertical ? e.Height : e.Width)));
            for (var i = 0; i < tabs.Count; i++) RenderTab(builder, tabs[i], current, true);
            RenderIndicator(builder, current >= 0 ? tabs[current] : null);
            builder.CloseElement();
            builder.CloseElement();
            if (Overflowing) RenderScrollButton(builder, 50, back: false);
        }
        if (ShowAddButton)
        {
            builder.OpenElement(300, "TabsAdd");
            builder.AddAttribute(301, "role", "button");
            builder.AddAttribute(302, "focusable", OnAdd.HasDelegate);
            builder.AddAttribute(303, "disabled", !OnAdd.HasDelegate);
            builder.AddAttribute(304, "accessible-name", ControlStrings.Get("AddTab"));
            builder.AddAttribute(305, "tooltip", ControlStrings.Get("AddTab"));
            builder.AddAttribute(307, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, AddAsync));
            builder.AddElementReferenceCapture(308, reference => addButton = reference);
            builder.AddContent(309, "+");
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    private void RenderTab(RenderTreeBuilder builder, Tab tab, int current, bool tracked)
    {
        var position = tabs.IndexOf(tab);
        var selected = position == current;
        builder.OpenElement(100, "Tab");
        builder.SetKey(tab);
        builder.AddAttribute(101, "id", tab.TabId);
        builder.AddAttribute(102, "class", tab.Class);
        builder.AddAttribute(103, "role", "tab");
        builder.AddAttribute(104, "selected", selected);
        builder.AddAttribute(105, "disabled", tab.Disabled);
        builder.AddAttribute(106, "focusable", !tab.Disabled);
        builder.AddAttribute(107, "tabindex", selected ? 0 : -1);
        builder.AddAttribute(108, "position-in-set", position + 1);
        builder.AddAttribute(109, "size-of-set", tabs.Count);
        builder.AddAttribute(110, "closable", tab.Closable);
        var compact = Presentation == TabsPresentation.Document && WidthMode == TabWidthMode.Compact && !selected;
        builder.AddAttribute(111, "accessible-name", tab.AccessibleName ?? (compact ? tab.Label : null));
        builder.AddAttribute(112, "tooltip", tab.Tooltip ?? (compact ? tab.Label : null));
        builder.AddAttribute(113, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, e => reorder.SuppressClick(e) ? Task.CompletedTask : SelectAsync(position)));
        if (tracked) builder.AddAttribute(114, tracker!.Attributes(this, tab).Key, tracker.Attributes(this, tab).Value);
        builder.AddAttribute(115, "index", position);
        builder.AddAttribute(116, "compact", compact);
        builder.AddAttribute(117, "dragging", reorder.Source == position);
        builder.AddAttribute(118, "drop-target", reorder.Target == position);
        builder.AddAttribute(119, "drop-position", reorder.Target == position ? reorder.After ? "after" : "before" : null);
        builder.AddAttribute(160, "__internal_onpointerdown#tabs", EventCallback.Factory.Create<PointerEventArgs>(this, e =>
        {
            if (!ControlTree.Ancestors(e.OriginalTarget).TakeWhile(node => node.Name != "Tab").Any(node => node.Name == "TabClose")) reorder.Down(position, e);
        }));
        if (CanReorderTabs && OnTabReorder.HasDelegate)
        {
            builder.AddAttribute(161, "layout-transition", true);
            builder.AddAttribute(162, "style", "transform: none; transform-origin: 0 0");
        }
        if (tab.Leading is not null)
        {
            builder.OpenElement(120, "TabLeading");
            builder.AddContent(121, tab.Leading);
            builder.CloseElement();
        }
        builder.OpenElement(130, "TabLabel");
        if (tab.Header is not null) builder.AddContent(131, tab.Header);
        else builder.AddContent(132, tab.Label);
        builder.CloseElement();
        if (tab.Badge is not null)
        {
            builder.OpenElement(140, "TabBadge");
            builder.AddContent(141, tab.Badge);
            builder.CloseElement();
        }
        if (tab.Closable)
        {
            builder.OpenElement(150, "TabClose");
            builder.AddAttribute(151, "role", "button");
            builder.AddAttribute(152, "accessible-name", ControlStrings.Get("CloseTab"));
            builder.AddAttribute(153, "tooltip", ControlStrings.Get("CloseTab"));
            builder.AddAttribute(154, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => CloseAsync(tab)));
            builder.AddAttribute(156, "disabled", !tab.OnClose.HasDelegate);
            builder.AddContent(155, "×");
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    private void RenderIndicator(RenderTreeBuilder builder, Tab? selected)
    {
        builder.OpenComponent<Indicator>(200);
        builder.AddComponentParameter(201, nameof(Indicator.As), "TabsIndicator");
        builder.AddComponentParameter(202, nameof(Indicator.Target), selected is null ? null : tracker!.BoxOf(selected));
        builder.AddComponentParameter(203, nameof(Control.AdditionalAttributes), new Dictionary<string, object?>
        {
            ["__internal_onresize#indicator-motion"] = EventCallback.Factory.Create<ResizeEventArgs>(this, e =>
            {
                if (indicatorReady || e.Width <= 0 || e.Height <= 0) return;
                indicatorReady = true;
                StateHasChanged();
            }),
        });
        builder.CloseComponent();
    }

    private void RenderScrollButton(RenderTreeBuilder builder, int sequence, bool back)
    {
        var atEdge = back ? scrollOffset <= 0.5 : scrollOffset >= extent - viewport - 0.5;
        builder.OpenElement(sequence, back ? "TabsScrollBack" : "TabsScrollForward");
        builder.AddAttribute(sequence + 1, "role", "button");
        builder.AddAttribute(sequence + 2, "accessible-name", ControlStrings.Get(back ? "Previous" : "Next"));
        builder.AddAttribute(sequence + 3, "glyph", (back, Vertical) switch { (true, true) => "chevron-up", (true, false) => "chevron-left", (false, true) => "chevron-down", _ => "chevron-right" });
        builder.AddAttribute(sequence + 4, "disabled", atEdge);
        builder.AddAttribute(sequence + 5, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => ScrollBy(back ? -1 : 1)));
        builder.CloseElement();
    }

    private void RenderMore(RenderTreeBuilder builder, IReadOnlyList<Tab> hidden)
    {
        builder.OpenElement(0, "TabsOverflow");
        builder.AddAttribute(1, "role", "button");
        builder.AddAttribute(2, "focusable", true);
        builder.AddAttribute(3, "expanded", menuOpen);
        builder.AddAttribute(4, "accessible-name", ControlStrings.Get("More"));
        builder.AddAttribute(5, "tooltip", ControlStrings.Get("More"));
        builder.AddAttribute(6, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => { menuOpen = !menuOpen; }));
        builder.AddElementReferenceCapture(7, reference => moreButton = reference);
        builder.OpenElement(8, "TabsOverflowGlyph");
        builder.AddAttribute(9, "glyph", "chevron-down");
        builder.AddAttribute(10, "semantic-hidden", true);
        builder.CloseElement();
        builder.CloseElement();
        builder.OpenComponent<Menu>(20);
        builder.AddComponentParameter(21, nameof(Menu.IsOpen), menuOpen);
        builder.AddComponentParameter(22, nameof(Menu.IsOpenChanged), EventCallback.Factory.Create<bool>(this, value => { menuOpen = value; }));
        builder.AddComponentParameter(23, nameof(Menu.Anchor), moreButton);
        builder.AddComponentParameter(24, nameof(Menu.Alignment), OverlayAlignment.End);
        builder.AddComponentParameter(25, nameof(Menu.AccessibleName), ControlStrings.Get("More"));
        builder.AddComponentParameter(26, nameof(Menu.ChildContent), (RenderFragment)(content =>
        {
            foreach (var tab in hidden)
            {
                var position = tabs.IndexOf(tab);
                content.OpenComponent<MenuItem>(0);
                content.SetKey(tab);
                content.AddComponentParameter(1, nameof(MenuItem.Class), "TabsOverflowItem");
                content.AddComponentParameter(2, nameof(MenuItem.Disabled), tab.Disabled);
                content.AddComponentParameter(3, nameof(MenuItem.Leading), tab.Leading);
                content.AddComponentParameter(4, nameof(MenuItem.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => SelectAsync(position)));
                content.AddComponentParameter(5, nameof(MenuItem.ChildContent), tab.Header ?? (b => b.AddContent(0, tab.Label)));
                content.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }

    private void RenderPanels(RenderTreeBuilder builder, int current)
    {
        for (var i = 0; i < tabs.Count; i++)
        {
            var tab = tabs[i];
            var selected = i == current;
            if (!selected && !(KeepMounted && visited.Contains(tab))) continue;
            builder.OpenElement(0, "TabsPanel");
            builder.SetKey(tab);
            builder.AddAttribute(1, "id", tab.PanelId);
            builder.AddAttribute(2, "role", "tabpanel");
            builder.AddAttribute(3, "labeled-by", tab.TabId);
            builder.AddAttribute(4, "selected", selected);
            if (!selected) builder.AddAttribute(5, "style", "display: none");
            builder.AddContent(6, tab.ChildContent);
            builder.CloseElement();
        }
    }

    private void Measure(ref double target, double value)
    {
        if (Math.Abs(target - value) < 0.01) return;
        var before = Overflowing;
        target = value;
        if (before != Overflowing || Overflow == TabsOverflow.Scroll) StateHasChanged();
    }

    private void Scrolled(ScrollEventArgs e)
    {
        var offset = Vertical ? e.ScrollTop : e.ScrollLeft;
        if (Math.Abs(offset - scrollOffset) < 0.01) return;
        scrollOffset = offset;
        StateHasChanged();
    }

    private void ScrollBy(int direction)
    {
        var target = Math.Clamp(scrollOffset + direction * Math.Max(40, viewport * 0.8), 0, Math.Max(0, extent - viewport));
        if (Vertical) scroller.ScrollTo(null, target);
        else scroller.ScrollTo(target, null);
    }

    /// <summary>Scrolls the selected tab into view after the selection changed.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    protected override void OnAfterRender(bool firstRender)
    {
        if (SynchronizeDeclarations())
        {
            StateHasChanged();
            return;
        }
        ValidateDragKeys();
        reorder.Synchronize();
        if (pendingFocus is { } focus)
        {
            pendingFocus = null;
            FocusTab(focus);
        }
        if (focusEmpty)
        {
            focusEmpty = false;
            var emptyFocusTarget = ShowAddButton && OnAdd.HasDelegate ? addButton : Element;
            if (emptyFocusTarget.Element is { } node && node.GetFeature<IElementInteraction>() is { } input) _ = input.FocusAsync(node, preventScroll: true);
        }
        if (!reveal || Overflow != TabsOverflow.Scroll || UsesMenu) return;
        var current = Current();
        if (current < 0 || tracker!.BoxOf(tabs[current]) is not { } box || viewport <= 0) return;
        reveal = false;
        var (start, length) = Vertical ? (box.Y, box.Height) : (box.X, box.Width);
        double? target = start < scrollOffset ? start : start + length > scrollOffset + viewport ? start + length - viewport : null;
        if (target is not { } value) return;
        if (Vertical) scroller.ScrollTo(null, value);
        else scroller.ScrollTo(value, null);
    }

    /// <summary>Reports selection indexes and keys normalized by declaration reordering or removal.</summary>
    /// <param name="firstRender">Whether this is the first render.</param>
    /// <returns>A task that completes when the structural selection notifications finish.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!reportSelection) return;
        var notifyKey = reportKey;
        reportSelection = reportKey = false;
        var current = Current();
        var selectedKey = current >= 0 ? tabs[current].Key : null;
        await SelectedIndexChanged.InvokeAsync(current);
        if (notifyKey) await SelectedKeyChanged.InvokeAsync(selectedKey);
    }

    private bool SynchronizeDeclarations()
    {
        if (Element.Element is not { } root) return false;
        var ordered = new List<Tab>();
        Visit(root.LogicalChildren);
        if (ordered.Count != tabs.Count || ordered.SequenceEqual(tabs)) return false;
        var before = Current();
        var selected = before >= 0 ? tabs[before] : null;
        tabs.Clear();
        tabs.AddRange(ordered);
        if (selected is not null)
        {
            var next = tabs.IndexOf(selected);
            index.Set(next);
            if (next != before) reportSelection = true;
        }
        reveal = true;
        return true;

        void Visit(IEnumerable<Node>? nodes)
        {
            if (nodes is null) return;
            foreach (var node in nodes)
            {
                if (node is ComponentNode { Component: Tab tab } && tab.Owner == this) ordered.Add(tab);
                else if (node is not ComponentNode { Component: Tabs }) Visit(node.LogicalChildren);
            }
        }
    }

    private void ValidateDragKeys()
    {
        if (!reorder.Enabled) return;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        if (tabs.Any(tab => string.IsNullOrEmpty(tab.Key) || !keys.Add(tab.Key)))
            throw new InvalidOperationException("Dragging tabs requires unique nonempty Tab.Key values and keyed declarations.");
    }

    private bool CanMove(int from, int to) => from >= 0 && from < tabs.Count && to >= 0 && to < tabs.Count &&
        !tabs[from].Disabled && !tabs[to].Disabled && (from == to || CanReorderTabs && OnTabReorder.HasDelegate);

    private Task ReorderAsync(int from, int to) => CanMove(from, to)
        ? OnTabReorder.InvokeAsync(new(tabs[from], from, to)) : Task.CompletedTask;

    private Task DragOutAsync(int position, double x, double y) => position >= 0 && position < tabs.Count && !tabs[position].Disabled && OnTabDragOut.HasDelegate
        ? OnTabDragOut.InvokeAsync(new(tabs[position], x, y)) : Task.CompletedTask;

    private Task AddAsync() => ShowAddButton && OnAdd.HasDelegate ? OnAdd.InvokeAsync() : Task.CompletedTask;

    internal void Register(Tab tab)
    {
        tabs.Add(tab);
        if (!disposed) StateHasChanged();
    }

    internal void Unregister(Tab tab)
    {
        var before = Current();
        var selected = before >= 0 ? tabs[before] : null;
        var removed = tabs.IndexOf(tab);
        if (removed < 0) return;
        var hadFocus = Element.Element is { } root && ControlTree.Descendants(root).Any(node =>
            (node.Id == tab.TabId || node.Id == tab.PanelId) && node.States.HasFlag(ElementStates.FocusWithin));
        tabs.RemoveAt(removed);
        visited.Remove(tab);
        tracker?.Forget(tab);
        if (selected == tab)
        {
            var next = Enumerable.Range(0, tabs.Count).OrderBy(position => Math.Abs(position - removed)).FirstOrDefault(position => !tabs[position].Disabled, -1);
            var nextKey = next >= 0 ? tabs[next].Key : null;
            reportKey |= key.Set(nextKey);
            index.Set(next);
            reportSelection = true;
            if (hadFocus && next >= 0) pendingFocus = tabs[next];
            if (hadFocus && next < 0) focusEmpty = true;
        }
        else if (selected is not null)
        {
            var next = tabs.IndexOf(selected);
            index.Set(next);
            reportSelection |= before != next;
            if (hadFocus) pendingFocus = selected;
        }
        else if (hadFocus) focusEmpty = true;
        reorder.Synchronize();
        if (!disposed) StateHasChanged();
    }

    internal void Refresh() { if (!disposed) StateHasChanged(); }

    /// <summary>Selects an enabled tab as the user would and reports the change.</summary>
    /// <param name="position">The zero-based index of the tab.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SelectAsync(int position)
    {
        if (position < 0 || position >= tabs.Count || tabs[position].Disabled) return;
        menuOpen = false;
        var tab = tabs[position];
        var changedIndex = position != Current() | index.Set(position);
        var changedKey = key.Set(tab.Key);
        if (!changedIndex && !changedKey)
        {
            StateHasChanged();
            return;
        }
        reveal = true;
        StateHasChanged();
        await SelectedIndexChanged.InvokeAsync(position);
        if (changedKey) await SelectedKeyChanged.InvokeAsync(tab.Key);
        await Events.RaiseChangeAsync(Element, position);
    }

    private async Task CloseAsync(Tab tab)
    {
        if (tab.Disabled || !tab.Closable || !tab.OnClose.HasDelegate) return;
        await tab.OnClose.InvokeAsync();
    }

    private async Task KeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Handled || !OwnsKeyboardFocus()) return;
        if (reorder.CancelFromKey(e)) return;
        if (FocusedTab() is { } tab && CanReorderTabs && OnTabReorder.HasDelegate && await reorder.KeyAsync(e, tabs.IndexOf(tab), tabs.Count)) return;
        var control = (e.Modifiers & KeyModifiers.Control) != 0;
        if (control && (e.Modifiers & ~(KeyModifiers.Control | KeyModifiers.Shift)) == 0 && e.Key.Equals("t", StringComparison.OrdinalIgnoreCase) &&
            (e.Modifiers & KeyModifiers.Shift) == 0 && ShowAddButton && OnAdd.HasDelegate)
        {
            e.Handled = true;
            await AddAsync();
            return;
        }
        var selected = Current();
        if (control && e.Modifiers == KeyModifiers.Control && e.Key == "F4" && selected >= 0 && tabs[selected] is { Closable: true, Disabled: false } document && document.OnClose.HasDelegate)
        {
            e.Handled = true;
            await CloseAsync(document);
            return;
        }
        if (tabs.Count == 0) return;
        if (e.Key == "Delete" && e.Modifiers == KeyModifiers.None && FocusedTab() is { Closable: true } focused && focused.OnClose.HasDelegate)
        {
            e.Handled = true;
            await CloseAsync(focused);
            return;
        }
        if (!control || e.Key is not ("Tab" or "PageDown" or "PageUp")) return;
        var step = e.Key == "PageUp" || (e.Key == "Tab" && (e.Modifiers & KeyModifiers.Shift) != 0) ? -1 : 1;
        var current = Math.Max(0, Current());
        for (var i = 1; i <= tabs.Count; i++)
        {
            var next = ((current + step * i) % tabs.Count + tabs.Count) % tabs.Count;
            if (tabs[next].Disabled) continue;
            e.Handled = true;
            var hadFocus = FocusedTab() is not null;
            await SelectAsync(next);
            if (hadFocus) FocusTab(tabs[next]);
            return;
        }
    }

    private Tab? FocusedTab()
    {
        if (Element.Element is not { } root) return null;
        var focused = ControlTree.Descendants(root).FirstOrDefault(node => node.Name == "Tab" && (node.States & ElementStates.Focus) != 0);
        return focused is null ? null : tabs.FirstOrDefault(tab => Equals(focused.Attributes.GetValueOrDefault("id"), tab.TabId));
    }

    private bool OwnsKeyboardFocus()
    {
        if (Element.Element is not { } root) return false;
        var focused = ControlTree.Descendants(root).FirstOrDefault(node => node.States.HasFlag(ElementStates.Focus));
        return focused is null || ControlTree.Ancestors(focused).FirstOrDefault(node => node.Name == "Tabs") == root;
    }

    private void FocusTab(Tab tab)
    {
        if (Element.Element is { } root && ControlTree.Descendants(root).FirstOrDefault(node => Equals(node.Attributes.GetValueOrDefault("id"), tab.TabId)) is { } node)
            _ = new ElementReference(node).FocusAsync();
    }

    /// <summary>Cancels captured tab gestures when the strip is removed.</summary>
    public void Dispose()
    {
        disposed = true;
        reorder.Cancel();
    }
}

/// <summary>
/// A page of <see cref="Tabs" />: its tab shows <see cref="Leading" />, <see cref="Label" /> (or <see cref="Header" />) and
/// <see cref="Badge" />; its content is the page. A <see cref="Closable" /> tab shows a <c>TabClose</c> part that raises
/// <see cref="OnClose" />, as does Delete on the focused tab; the owner removes the tab.
/// </summary>
public sealed class Tab : ComponentBase, IDisposable
{
    private Tabs? registered;
    private object? shown;
    private long shownVersion;

    /// <summary>Gets or sets the plain-text label of the tab.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Gets or sets custom label content, used instead of <see cref="Label" />.</summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>Gets or sets content shown before the label, usually an <see cref="Icon" />.</summary>
    [Parameter] public RenderFragment? Leading { get; set; }

    /// <summary>Gets or sets content shown after the label, such as a count.</summary>
    [Parameter] public RenderFragment? Badge { get; set; }

    /// <summary>Gets or sets the key that identifies the tab for <see cref="Tabs.SelectedKey" />. Reordering and drag-out require unique nonempty stable keys and keyed declarations.</summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>Gets or sets whether the tab cannot be selected; keyboard navigation skips it.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether the tab shows a close part.</summary>
    [Parameter] public bool Closable { get; set; }

    /// <summary>Gets or sets the callback invoked when the user asks to close the tab.</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>Gets or sets NSS classes added to the tab element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Gets or sets the name of the tab for assistive technologies when the label is not text.</summary>
    [Parameter] public string? AccessibleName { get; set; }

    /// <summary>Gets or sets a short text shown when the pointer rests on the tab.</summary>
    [Parameter] public string? Tooltip { get; set; }

    /// <summary>Gets or sets the content of the page.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the enclosing tabs, supplied automatically.</summary>
    [CascadingParameter] public Tabs? Owner { get; set; }

    internal string TabId { get; } = UniqueId.Create("tab");

    internal string PanelId { get; } = UniqueId.Create("tabpanel");

    /// <summary>Joins the enclosing tabs and refreshes them when the tab changes.</summary>
    protected override void OnParametersSet()
    {
        if (Owner is null) throw new InvalidOperationException("Tab must be inside Tabs.");
        object current = (Label, Key, Disabled, Closable, Header, Leading, Badge, Class, AccessibleName, Tooltip, ChildContent);
        if (registered != Owner)
        {
            registered?.Unregister(this);
            (registered = Owner).Register(this);
        }
        else if (shownVersion != Owner.DeclarationVersion && !Equals(shown, current)) registered.Refresh();
        shown = current;
        shownVersion = Owner.DeclarationVersion;
    }

    /// <summary>Renders nothing; the tabs render the tab and its panel.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }

    /// <summary>Leaves the enclosing tabs.</summary>
    public void Dispose()
    {
        registered?.Unregister(this);
        registered = null;
    }
}
