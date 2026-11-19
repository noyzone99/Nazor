using System.Text;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Accessibility;

/// <summary>
/// What a semantic node is for assistive technologies; controls declare it with the <c>role</c> attribute.
/// </summary>
public enum SemanticRole
{
    /// <summary>Exposes the window role to assistive technologies.</summary>
    Window,

    /// <summary>Exposes the group role to assistive technologies.</summary>
    Group,

    /// <summary>Exposes the text role to assistive technologies.</summary>
    Text,

    /// <summary>Exposes the button role to assistive technologies.</summary>
    Button,

    /// <summary>Exposes the check box role to assistive technologies.</summary>
    CheckBox,

    /// <summary>Exposes the text box role to assistive technologies.</summary>
    TextBox,

    /// <summary>Exposes the image role to assistive technologies.</summary>
    Image,

    /// <summary>Exposes the menu role to assistive technologies.</summary>
    Menu,

    /// <summary>Exposes the menu item role to assistive technologies.</summary>
    MenuItem,

    /// <summary>Exposes the tool tip role to assistive technologies.</summary>
    ToolTip,

    /// <summary>Exposes the radio button role to assistive technologies.</summary>
    RadioButton,

    /// <summary>Exposes the switch role to assistive technologies.</summary>
    Switch,

    /// <summary>Exposes the slider role to assistive technologies.</summary>
    Slider,

    /// <summary>Exposes the progress bar role to assistive technologies.</summary>
    ProgressBar,

    /// <summary>Exposes the combo box role to assistive technologies.</summary>
    ComboBox,

    /// <summary>Exposes the list role to assistive technologies.</summary>
    List,

    /// <summary>Exposes the list item role to assistive technologies.</summary>
    ListItem,

    /// <summary>Exposes the tree role to assistive technologies.</summary>
    Tree,

    /// <summary>Exposes the tree item role to assistive technologies.</summary>
    TreeItem,

    /// <summary>Exposes the tab list role to assistive technologies.</summary>
    TabList,

    /// <summary>Exposes the tab item role to assistive technologies.</summary>
    TabItem,

    /// <summary>Exposes the splitter role to assistive technologies.</summary>
    Splitter,

    /// <summary>Exposes the hyperlink role to assistive technologies.</summary>
    Hyperlink,

    /// <summary>Exposes the status role to assistive technologies.</summary>
    Status,

    /// <summary>Exposes the split button role to assistive technologies.</summary>
    SplitButton,

    /// <summary>Exposes the menu bar role to assistive technologies.</summary>
    MenuBar,

    /// <summary>Exposes the tool bar role to assistive technologies.</summary>
    ToolBar,

    /// <summary>Exposes the calendar role to assistive technologies.</summary>
    Calendar,

    /// <summary>Exposes the title bar role to assistive technologies.</summary>
    TitleBar,

    /// <summary>A document list without selection behavior.</summary>
    DocumentList,

    /// <summary>An item in a document list.</summary>
    DocumentListItem,

    /// <summary>A collection of terms and descriptions.</summary>
    DefinitionList,

    /// <summary>A term in a definition list.</summary>
    DefinitionTerm,

    /// <summary>A description associated with a term.</summary>
    DefinitionDescription,

    /// <summary>A document heading with a level.</summary>
    Heading,

    /// <summary>A named form and its submission scope.</summary>
    Form,

    /// <summary>A window-local dialog.</summary>
    Dialog,

    /// <summary>A dialog requiring a decision or acknowledgement.</summary>
    AlertDialog,

    /// <summary>A button with persistent pressed state.</summary>
    ToggleButton,

    /// <summary>A read-only measurement within a range.</summary>
    Meter,

    /// <summary>A static semantic table.</summary>
    Table,

    /// <summary>A table row group.</summary>
    RowGroup,

    /// <summary>A table row.</summary>
    Row,

    /// <summary>A table data cell.</summary>
    Cell,

    /// <summary>A table column header.</summary>
    ColumnHeader,

    /// <summary>A table row header.</summary>
    RowHeader,

    /// <summary>A checked menu command.</summary>
    MenuCheckBox,

    /// <summary>An exclusive checked menu command.</summary>
    MenuRadio,

    /// <summary>A noninteractive semantic separator.</summary>
    Separator,
}

/// <summary>Properties whose changes are reported by <see cref="SemanticTree.PropertyChanged" />.</summary>
public enum SemanticProperty
{
    /// <summary>Identifies changes to the accessible name.</summary>
    Name,

    /// <summary>Identifies changes to descriptive help text.</summary>
    Description,

    /// <summary>Identifies changes to required-value semantics.</summary>
    IsRequired,

    /// <summary>Identifies changes to invalid-value semantics.</summary>
    IsInvalid,

    /// <summary>Identifies changes to the enabled state.</summary>
    IsEnabled,

    /// <summary>Identifies changes to whether keyboard focus is supported.</summary>
    IsFocusable,

    /// <summary>Identifies changes to the checked state.</summary>
    IsChecked,

    /// <summary>Identifies changes to the exposed text or control value.</summary>
    Value,

    /// <summary>Identifies changes to whether the node is outside the visible viewport.</summary>
    IsOffscreen,

    /// <summary>Identifies changes to the numeric range value.</summary>
    RangeValue,

    /// <summary>Identifies changes to the expanded or collapsed state.</summary>
    IsExpanded,

    /// <summary>Identifies changes to the accessible text selection.</summary>
    TextSelection,

    /// <summary>Identifies changes to the availability of an NSS scroll container.</summary>
    ScrollAvailable,
    /// <summary>Identifies changes to horizontal scroll progress in percent, or -1 when that axis cannot scroll.</summary>
    HorizontalScrollPercent,
    /// <summary>Identifies changes to vertical scroll progress in percent, or -1 when that axis cannot scroll.</summary>
    VerticalScrollPercent,
    /// <summary>Identifies changes to the horizontal viewport as a percentage of content width.</summary>
    HorizontalViewSize,
    /// <summary>Identifies changes to the vertical viewport as a percentage of content height.</summary>
    VerticalViewSize,
    /// <summary>Identifies changes to whether content overflows the horizontal scroll viewport.</summary>
    HorizontallyScrollable,
    /// <summary>Identifies changes to whether content overflows the vertical scroll viewport.</summary>
    VerticallyScrollable,
}

/// <summary>
/// How a region whose content changes is announced (the <c>live</c> attribute, as <c>aria-live</c>): polite waits for
/// the current speech, assertive interrupts it.
/// </summary>
public enum LiveSetting
{
    /// <summary>Disables automatic announcements for changes in the live region.</summary>
    Off,

    /// <summary>Announces changes when assistive technology finishes its current speech.</summary>
    Polite,

    /// <summary>Requests an immediate announcement that may interrupt current speech.</summary>
    Assertive,
}

/// <summary>How an announcement is spoken: after the current speech, or at once.</summary>
public enum AnnouncementPriority
{
    /// <summary>Queues the announcement after current speech.</summary>
    Polite,

    /// <summary>Requests an immediate announcement that may interrupt current speech.</summary>
    Assertive,
}

/// <summary>The range of a slider or a progress bar, as UI Automation reports it.</summary>
/// <param name="Minimum">The minimum (double).</param>
/// <param name="Maximum">The maximum (double).</param>
/// <param name="Value">The numeric value or payload represented by this record.</param>
/// <param name="SmallChange">The small change (double).</param>
/// <param name="LargeChange">The large change (double).</param>
/// <param name="IsReadOnly">The is read only (bool).</param>
public sealed record RangeInfo(double Minimum, double Maximum, double Value, double SmallChange, double LargeChange, bool IsReadOnly);

/// <summary>
/// A node of the semantic tree: role, name, states and geometry of a control. The same element keeps the same node
/// (and <see cref="Id" />) for its whole life; a removed element detaches it.
/// </summary>
public sealed class SemanticNode
{
    private readonly List<SemanticNode> children = [];

    internal SemanticNode(int id, ElementNode? element, SemanticRole role)
    {
        Id = id;
        Element = element;
        Role = role;
    }

    /// <summary>Unique in the tree and never reused.</summary>
    public int Id { get; }

    /// <summary>
    /// The element the node describes; for the window root, the <c>Window</c> element if there is one.
    /// </summary>
    public ElementNode? Element { get; internal set; }

    /// <summary>Gets the role exposed to assistive technologies.</summary>
    public SemanticRole Role { get; internal set; }

    /// <summary>Gets the document heading level, or null for other roles.</summary>
    public int? HeadingLevel { get; internal set; }

    /// <summary>Gets whether this field requires a value.</summary>
    public bool IsRequired { get; internal set; }

    /// <summary>Gets whether this field exposes an invalid value.</summary>
    public bool IsInvalid { get; internal set; }

    /// <summary>Gets the zero-based table row index.</summary>
    public int? RowIndex { get; internal set; }

    /// <summary>Gets the zero-based table column index.</summary>
    public int? ColumnIndex { get; internal set; }

    /// <summary>Gets the number of table rows occupied by this cell.</summary>
    public int RowSpan { get; internal set; } = 1;

    /// <summary>Gets the number of table columns occupied by this cell.</summary>
    public int ColumnSpan { get; internal set; } = 1;

    /// <summary>Gets the table row count.</summary>
    public int? RowCount { get; internal set; }

    /// <summary>Gets the table column count.</summary>
    public int? ColumnCount { get; internal set; }

    /// <summary>Gets the parent node, or null for a root node.</summary>
    public SemanticNode? Parent { get; internal set; }

    /// <summary>Gets the ordered child nodes.</summary>
    public IReadOnlyList<SemanticNode> Children => children;
    internal List<SemanticNode> ChildList => children;

    /// <summary>
    /// The accessible name: <c>AccessibleName</c> when set, otherwise the text of the control that labels it (<c>LabeledBy</c>),
    /// the text content for buttons, check boxes and text, the title for the window; a control still without a name
    /// (a button with only an icon) takes its tooltip. A text box is never named after its value.
    /// </summary>
    public string Name { get; internal set; } = "";

    /// <summary>A longer description than the name (<c>AccessibleDescription</c>); null when there is none.</summary>
    public string? Description { get; internal set; }

    /// <summary>Help about the control: its tooltip when it is not the name, otherwise its description.</summary>
    public string? HelpText { get; internal set; }

    /// <summary>
    /// The node whose text names this one (<c>LabeledBy</c>, the <c>Id</c> of a text); null when there is none.
    /// </summary>
    public SemanticNode? LabeledBy { get; internal set; }

    /// <summary>
    /// What the control is, in words, when its role does not say it (a switch is a button for UI Automation).
    /// </summary>
    public string? RoleDescription { get; internal set; }

    /// <summary>
    /// The position from 1 of an item in its set (the options of a list, the tree items of a level under the same
    /// parent, the tabs, the radio buttons of a group, the items of a menu); null for the other roles.
    /// </summary>
    public int? PositionInSet { get; internal set; }

    /// <summary>The number of items of the set of <see cref="PositionInSet" />.</summary>
    public int? SizeOfSet { get; internal set; }

    /// <summary>The selection and caret of a text box (not of a password box); null for the other roles.</summary>
    public TextSelection? TextSelection { get; internal set; }

    /// <summary>A list where several items can be selected (the <c>multiselectable</c> attribute).</summary>
    public bool CanSelectMultiple { get; internal set; }

    /// <summary>
    /// A live region (the <c>live</c> attribute, an info bar): its changes are announced. Without an accessible name
    /// it is named by its text, which is what is read.
    /// </summary>
    public LiveSetting LiveSetting { get; internal set; }

    /// <summary>The control <c>Id</c>, stable across runs: the key used by UI tests.</summary>
    public string? AutomationId { get; internal set; }

    /// <summary>Gets whether the element is enabled for interaction.</summary>
    public bool IsEnabled { get; internal set; } = true;

    /// <summary>Gets whether the element can receive keyboard focus.</summary>
    public bool IsFocusable { get; internal set; }

    /// <summary>Gets whether this element currently has keyboard focus.</summary>
    public bool HasFocus { get; internal set; }

    /// <summary>The state of a check box; null for other roles.</summary>
    public bool? IsChecked { get; internal set; }

    /// <summary>The value of a text box; null for other roles.</summary>
    public string? Value { get; internal set; }

    /// <summary>Minimum, maximum and value of a slider or a progress bar; null for the other roles.</summary>
    public RangeInfo? Range { get; internal set; }

    /// <summary>Whether a combo box shows its list; null for the other roles.</summary>
    public bool? IsExpanded { get; internal set; }

    /// <summary>A text box that holds a password: its value is not exposed.</summary>
    public bool IsPassword { get; internal set; }

    /// <summary>The depth of a tree item, from 1; null for the other roles.</summary>
    public int? Level { get; internal set; }

    /// <summary>Bounds in logical client coordinates, scroll offsets included.</summary>
    public Rect Bounds { get; internal set; }

    /// <summary>True when the node is entirely outside the window or clipped by a scrolling ancestor.</summary>
    public bool IsOffscreen { get; internal set; }

    /// <summary>Scroll position and extent of this node's viewport when computed overflow enables scrolling; null otherwise. The window node uses its implicit content viewport.</summary>
    public ScrollState? Scroll { get; internal set; }
    internal ElementNode? ScrollElement { get; set; }

    internal (double X, double Y, double Width, double Height, bool Horizontal, bool Vertical) ScrollMetrics { get; set; }

    /// <summary>The element was removed: the node no longer belongs to the tree.</summary>
    public bool IsDetached { get; internal set; }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => $"{Role} \"{Name}\"";
}

/// <summary>
/// The accessible view of a window, derived from the element tree, the scene geometry and the input state.
/// Layout-only containers are skipped (their children move up to the nearest semantic ancestor), hidden subtrees (<c>display:
/// none</c>) are not exposed and the content of buttons, check boxes, text boxes and text becomes their name.
/// <see cref="Update" /> synchronises the tree and reports the differences; actions go through the input system, like
/// keyboard and pointer input. Everything runs on the UI thread.
/// </summary>
public sealed class SemanticTree : IDisposable
{
    private readonly Renderer renderer;
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly Dictionary<ElementNode, SemanticNode> nodes = [];
    private readonly List<(SemanticNode Node, ElementNode Label)> labeled = [];
    private readonly List<SemanticNode> liveChanges = [];
    private Dictionary<string, ElementNode>? ids;
    private int nextId = 1;
    private bool dirty = true;
    private bool updating;
    private bool disposed;

    /// <summary>Initializes a new instance of SemanticTree.</summary>
    /// <param name="renderer">The renderer that owns the component tree.</param>
    /// <param name="scene">The scene used for layout, hit testing, and drawing.</param>
    /// <param name="input">The input manager associated with the scene.</param>
    public SemanticTree(Renderer renderer, Scene scene, InputManager input)
    {
        this.renderer = renderer;
        this.scene = scene;
        this.input = input;
        Root = new SemanticNode(0, null, SemanticRole.Window);
        renderer.BatchCompleted += OnBatchCompleted;
        input.VisualStateChanged += Invalidate;
    }

    /// <summary>The window: always the same node, whatever the root components render.</summary>
    public SemanticNode Root { get; }

    /// <summary>The node with the keyboard focus, if any.</summary>
    public SemanticNode? Focused { get; private set; }

    /// <summary>Number of synchronisations performed (diagnostics and tests).</summary>
    public int UpdateCount { get; private set; }

    /// <summary>The focus moved to another node (or to none).</summary>
    public event Action<SemanticNode?>? FocusChanged;

    /// <summary>A property of an existing node changed: node, property, old value, new value.</summary>
    public event Action<SemanticNode, SemanticProperty, object?, object?>? PropertyChanged;

    /// <summary>The children of a node changed (added, removed or reordered).</summary>
    public event Action<SemanticNode>? ChildrenChanged;

    /// <summary>A node left the tree; it stays detached.</summary>
    public event Action<SemanticNode>? NodeRemoved;

    /// <summary>A button was invoked through <see cref="Invoke" />, after its handler ran.</summary>
    public event Action<SemanticNode>? Invoked;

    /// <summary>
    /// A live region appeared or its text changed (not on the first synchronisation: what is there when the tree is
    /// created is not news).
    /// </summary>
    public event Action<SemanticNode>? LiveRegionChanged;

    /// <summary>A message to speak (<see cref="Announce" />), from the node with the focus or the window.</summary>
    public event Action<SemanticNode, string, AnnouncementPriority>? Announced;

    /// <summary>
    /// Asks the screen reader to speak a message that is not on the screen, as "Saved" after a command: an
    /// announcement (UI Automation notification). Nothing happens without a message.
    /// </summary>
    /// <param name="message">The message (string).</param>
    /// <param name="priority">The priority (AnnouncementPriority).</param>
    public void Announce(string message, AnnouncementPriority priority = AnnouncementPriority.Polite)
    {
        renderer.Dispatcher.AssertAccess();
        if (disposed || string.IsNullOrWhiteSpace(message)) return;
        EnsureCurrent();
        Announced?.Invoke(Focused ?? Root, message.Trim(), priority);
    }

    /// <summary>Marks the tree as out of date; render batches and interaction states do it automatically.</summary>
    public void Invalidate() => dirty = true;

    /// <summary>Synchronises the tree only if something changed since the last synchronisation.</summary>
    public void EnsureCurrent()
    {
        if (dirty) Update();
    }

    /// <summary>Synchronises the tree with the elements and raises the change events.</summary>
    public void Update() => Synchronize(refreshScene: true);

    internal void UpdateAfterScene() => Synchronize(refreshScene: false);

    private void Synchronize(bool refreshScene)
    {
        if (updating || disposed) return;
        renderer.Dispatcher.AssertAccess();
        updating = true;
        var pending = new List<Action>();
        try
        {
            dirty = false;
            if (refreshScene) scene.Update();
            ids = null;
            labeled.Clear();
            liveChanges.Clear();
            var seen = new HashSet<ElementNode>();
            var rootElements = renderer.RootComponents.SelectMany(r => r.Nodes).OfType<ElementNode>().ToList();
            var window = rootElements.FirstOrDefault(e => e.Attributes.GetValueOrDefault("role") is "window");
            Root.Element = window;
            var top = new List<SemanticNode>();

            var sources = window is not null
                ? ChildElements(window).Concat(rootElements.Where(e => e != window))
                : rootElements;
            foreach (var element in sources) Collect(element, top, seen, pending);
            NumberItems(top);
            SyncChildren(Root, top, pending);
            Refresh(Root, isNew: false, pending);

            foreach (var (node, label) in labeled) node.LabeledBy = nodes.GetValueOrDefault(label);

            foreach (var node in liveChanges) pending.Add(() => LiveRegionChanged?.Invoke(node));

            foreach (var (element, node) in nodes.Where(n => !seen.Contains(n.Key)).ToList())
            {
                nodes.Remove(element);
                node.IsDetached = true;
                node.Parent = null;
                pending.Insert(0, () => NodeRemoved?.Invoke(node));
            }

            var focused = input.Focused is { } f && nodes.TryGetValue(f, out var focusedNode) ? focusedNode : null;
            if (focused != Focused)
            {
                Focused = focused;
                pending.Add(() => FocusChanged?.Invoke(focused));
            }
            UpdateCount++;
        }
        finally
        {
            updating = false;
        }

        foreach (var action in pending) action();
    }

    /// <summary>The deepest node at a point in logical client coordinates; the root if no control is there.</summary>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <returns>The resulting SemanticNode value.</returns>
    public SemanticNode HitTest(Point point)
    {
        EnsureCurrent();
        for (var element = scene.HitTest(point); element is not null; element = element.ParentElement)
            if (nodes.TryGetValue(element, out var node)) return node;
        return Root;
    }

    /// <summary>The node of an element, if the element is exposed.</summary>
    /// <param name="element">The element to inspect or update.</param>
    /// <returns>The resulting SemanticNode? value.</returns>
    public SemanticNode? Find(ElementNode element)
    {
        EnsureCurrent();
        return nodes.GetValueOrDefault(element);
    }

    /// <summary>
    /// The selected items of a list, a tree, a tab list or a combo box (its open list), among the nodes that exist: a
    /// virtualized list has only the rows it created.
    /// </summary>
    /// <param name="container">The container (SemanticNode).</param>
    /// <returns>The resulting IReadOnlyList&lt;SemanticNode&gt; value.</returns>
    public IReadOnlyList<SemanticNode> SelectedItems(SemanticNode container)
    {
        EnsureCurrent();
        var items = container.Role == SemanticRole.ComboBox
            ? container.Children.Where(c => c.Role == SemanticRole.List).SelectMany(Options)
            : Options(container);
        return [.. items.Where(item => item.Role is SemanticRole.ListItem or SemanticRole.TreeItem or SemanticRole.TabItem && item.IsChecked == true)];

        static IEnumerable<SemanticNode> Options(SemanticNode node) =>
            node.Children.SelectMany(child => child.Role == SemanticRole.Group ? Options(child) : [child]);
    }

    /// <summary>The list, tree, tab list or combo box an item is selected in; null for other nodes.</summary>
    /// <param name="item">The item (SemanticNode).</param>
    /// <returns>The resulting SemanticNode? value.</returns>
    public SemanticNode? SelectionContainer(SemanticNode item)
    {
        if (item.Role is not (SemanticRole.ListItem or SemanticRole.TreeItem or SemanticRole.TabItem)) return null;
        SemanticNode? list = null;
        for (var parent = item.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent.Role == SemanticRole.ComboBox) return parent;
            list ??= parent.Role is SemanticRole.List or SemanticRole.Tree or SemanticRole.TabList ? parent : null;
        }
        return list;
    }

    /// <summary>All nodes, depth first, root included.</summary>
    /// <returns>The resulting IEnumerable&lt;SemanticNode&gt; value.</returns>
    public IEnumerable<SemanticNode> Descendants()
    {
        EnsureCurrent();
        var stack = new Stack<SemanticNode>([Root]);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            for (var i = node.Children.Count - 1; i >= 0; i--) stack.Push(node.Children[i]);
        }
    }

    /// <summary>
    /// Presses a button. The handler runs later on the dispatcher, so the caller (an assistive technology) is never
    /// blocked; false when the node cannot be invoked.
    /// </summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool Invoke(SemanticNode node)
    {
        if (!CanAct(node, SemanticRole.Button) && !CanAct(node, SemanticRole.MenuItem) && !CanAct(node, SemanticRole.Hyperlink) && !CanAct(node, SemanticRole.SplitButton)) return false;
        var element = node.Element!;
        Post(() =>
        {
            if (input.Activate(element)) Invoked?.Invoke(node);
        });
        return true;
    }

    /// <summary>Toggles a check box now, so its new state can be read right after.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool Toggle(SemanticNode node) => (CanAct(node, SemanticRole.CheckBox) || CanAct(node, SemanticRole.Switch) || CanAct(node, SemanticRole.ToggleButton) || CanAct(node, SemanticRole.MenuCheckBox) || CanAct(node, SemanticRole.MenuRadio)) && input.Activate(node.Element!);

    /// <summary>Moves a slider to a value, snapped to its step and kept in its range, as the keyboard does.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool SetRangeValue(SemanticNode node, double value) =>
        (CanAct(node, SemanticRole.Slider) || CanAct(node, SemanticRole.Splitter) || (CanAct(node, SemanticRole.TextBox) && node.Range is not null)) && input.SetRangeValue(node.Element!, value);

    /// <summary>Checks a radio button or chooses an item of an open list, as a click does.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool Select(SemanticNode node) =>
        (CanAct(node, SemanticRole.RadioButton) || CanAct(node, SemanticRole.ListItem) || CanAct(node, SemanticRole.TreeItem) || CanAct(node, SemanticRole.TabItem)) &&
        input.Activate(node.Element!);

    /// <summary>
    /// The text of a text box whose words and lines assistive technologies read (null for a password).
    /// </summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <returns>The resulting string? value.</returns>
    public string? TextOf(SemanticNode node) => node.TextSelection is null ? null : node.Value;

    /// <summary>The lines of a text box as shown, broken at its width.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <returns>The resulting IReadOnlyList&lt;(int Start, int End)&gt; value.</returns>
    public IReadOnlyList<(int Start, int End)> TextLines(SemanticNode node) =>
        node is { IsDetached: false, Element: { } element } && node.TextSelection is not null ? input.GetTextLines(element) : [(0, node.Value?.Length ?? 0)];

    /// <summary>The rectangles of a part of the text of a text box, in logical client coordinates.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <param name="start">The start offset in the UTF-16 source text.</param>
    /// <param name="end">The end offset in the UTF-16 source text.</param>
    /// <returns>The resulting IReadOnlyList&lt;Rect&gt; value.</returns>
    public IReadOnlyList<Rect> TextBounds(SemanticNode node, int start, int end) =>
        node is { IsDetached: false, Element: { } element } && node.TextSelection is not null ? input.GetTextRangeBounds(element, start, end) : [];

    /// <summary>The position in the text of a text box closest to a point in logical client coordinates.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <returns>The resulting int value.</returns>
    public int TextIndexAt(SemanticNode node, Point point) =>
        node is { IsDetached: false, Element: { } element } && node.TextSelection is not null ? input.GetTextIndexAt(element, point) : 0;

    /// <summary>
    /// Scrolls the node into the visible area of its scrolling ancestors, without moving the focus.
    /// </summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool ScrollIntoView(SemanticNode node) => node is { IsDetached: false, Element: { } element } && input.BringIntoView(element);

    /// <summary>Selects a part of the text of a text box, as Shift+arrows do; false for other nodes.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <param name="start">The start offset in the UTF-16 source text.</param>
    /// <param name="end">The end offset in the UTF-16 source text.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool SelectText(SemanticNode node, int start, int end) =>
        CanAct(node, SemanticRole.TextBox) && node.TextSelection is not null && input.SetTextSelection(node.Element!, start, end);

    /// <summary>
    /// Adds an item to the selection of a list that selects several, or removes it, as Ctrl+click does; false in
    /// other containers or when the item already is so.
    /// </summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <param name="selected">The selected (bool).</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool SetSelected(SemanticNode node, bool selected) =>
        CanAct(node, SemanticRole.ListItem) && SelectionContainer(node) is { CanSelectMultiple: true } && node.IsChecked != selected &&
        input.Activate(node.Element!, KeyModifiers.Control);

    /// <summary>Opens or closes a combo box or a menu, as a click does; false when it already is so.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <param name="expanded">The expanded (bool).</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool SetExpanded(SemanticNode node, bool expanded) =>
        node.IsExpanded is { } current && current != expanded && (CanAct(node, SemanticRole.ComboBox) || CanAct(node, SemanticRole.Button) || CanAct(node, SemanticRole.MenuItem) ? input.Activate(ExpansionTarget(node.Element!))

            : (CanAct(node, SemanticRole.TreeItem) || CanAct(node, SemanticRole.SplitButton)) &&
              node.Element!.Children.OfType<ElementNode>().FirstOrDefault(e => e.Attributes.GetValueOrDefault("expander") is true) is { } arrow && input.Activate(arrow));

    /// <summary>Replaces the value of a text box, as if the user typed it.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool SetValue(SemanticNode node, string value) => CanAct(node, SemanticRole.TextBox) && input.SetText(node.Element!, value);

    /// <summary>Gives the keyboard focus to a focusable node and brings it into view.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool Focus(SemanticNode node)
    {
        if (node.IsDetached || node.Element is not { } element || !node.IsFocusable) return false;
        input.Focus(element, visible: true);
        input.BringIntoView(element);
        return input.Focused == element;
    }

    /// <summary>
    /// Scrolls an enabled node with an NSS scroll capability by <paramref name="deltaY" /> logical units.
    /// </summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <param name="deltaY">The vertical scroll delta in the specified unit.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool ScrollBy(SemanticNode node, float deltaY) => ScrollBy(node, 0, deltaY);

    /// <summary>Scrolls a node with a scroll capability on both axes, in logical units; false when the request is unchanged or unavailable.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <param name="deltaX">The horizontal scroll delta in the specified unit.</param>
    /// <param name="deltaY">The vertical scroll delta in the specified unit.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool ScrollBy(SemanticNode node, float deltaX, float deltaY) =>
        node is { IsDetached: false, IsEnabled: true, Scroll: not null, ScrollElement: { } element } && input.ScrollBy(element, deltaX, deltaY);

    /// <summary>Requests absolute logical offsets for an enabled scroll container; null preserves an axis. Honors NSS smooth scrolling.</summary>
    /// <param name="node">The accessible scroll container.</param>
    /// <param name="left">Horizontal offset, or null to keep the current destination on that axis.</param>
    /// <param name="top">Vertical offset, or null to keep the current destination on that axis.</param>
    /// <returns>True when a new destination is accepted; false when unchanged, detached, disabled or not scrollable.</returns>
    public bool ScrollTo(SemanticNode node, double? left, double? top) =>
        node is { IsDetached: false, IsEnabled: true, Scroll: not null, ScrollElement: { } element } && input.ScrollTo(element, left, top);

    /// <summary>Requests ordinary dialog dismissal, including AlertDialog cancellation rules.</summary>
    public bool Dismiss(SemanticNode node)
    {
        if (node.Role is not (SemanticRole.Dialog or SemanticRole.AlertDialog) || !CanAct(node,node.Role)) return false;
        if (node.Role == SemanticRole.AlertDialog && !Descendants().Any(candidate=>candidate.Role == SemanticRole.Button && candidate.IsEnabled && candidate.Element is { } element &&
            SemanticAncestors(element).FirstOrDefault(parent=>parent.Attributes.GetValueOrDefault("role") is "dialog" or "alertdialog") == node.Element &&
            SemanticAncestors(element).TakeWhile(parent=>parent != node.Element).Any(parent=>parent.Attributes.GetValueOrDefault("dialog-cancel") is true))) return false;
        Post(()=>input.Dismiss(node.Element!)); return true;
    }

    /// <summary>Gets the nearest table containing a cell or row, or the table itself.</summary>
    public SemanticNode? ContainingTable(SemanticNode node)
    {
        for (var current = node; current is not null; current = current.Parent)
            if (current.Role == SemanticRole.Table) return current;
        return null;
    }

    /// <summary>Gets cells and header cells of this table, excluding any nested table.</summary>
    public IReadOnlyList<SemanticNode> TableCells(SemanticNode table)
    {
        EnsureCurrent();
        return Descendants().Where(node => (node.Role is SemanticRole.Cell or SemanticRole.ColumnHeader or SemanticRole.RowHeader) && ContainingTable(node) == table).ToArray();
    }

    /// <summary>Gets the cell covering a zero-based logical coordinate, including spanned coordinates.</summary>
    public SemanticNode? TableCellAt(SemanticNode table, int row, int column) => row < 0 || column < 0 ? null :
        TableCells(table).FirstOrDefault(cell => cell.RowIndex is { } r && cell.ColumnIndex is { } c && row >= r && row < r + cell.RowSpan && column >= c && column < c + cell.ColumnSpan);

    /// <summary>Gets row or column headers overlapping the corresponding cell coordinates.</summary>
    public IReadOnlyList<SemanticNode> TableHeaders(SemanticNode node, bool columns)
    {
        if (ContainingTable(node) is not { } table) return [];
        return TableCells(table).Where(header => header != node && header.Role == (columns ? SemanticRole.ColumnHeader : SemanticRole.RowHeader) &&
            (node == table || (columns ? header.ColumnIndex < node.ColumnIndex + node.ColumnSpan && node.ColumnIndex < header.ColumnIndex + header.ColumnSpan
                : header.RowIndex < node.RowIndex + node.RowSpan && node.RowIndex < header.RowIndex + header.RowSpan))).ToArray();
    }

    private bool CanAct(SemanticNode node, SemanticRole role)
    {
        renderer.Dispatcher.AssertAccess();
        EnsureCurrent();
        return !node.IsDetached && node.Role == role && node.IsEnabled && node.Element is not null;
    }

    private void Post(Action action)
    {
        if (renderer.Dispatcher is UIDispatcher dispatcher) dispatcher.Post(action);
        else _ = renderer.Dispatcher.InvokeAsync(action);
    }

    private void Collect(ElementNode element, List<SemanticNode> into, HashSet<ElementNode> seen, List<Action> pending)
    {
        if (element.IsRemoved || element.Attributes.GetValueOrDefault("semantic-hidden") is true || scene.GetStyle(element).Get(StyleProperties.Display) == Display.None) return;

        if ((RoleOf(element) ?? (LiveOf(element) != LiveSetting.Off || element.Name != "WindowContent" && scene.GetScrollState(element) is not null ? SemanticRole.Group : null)) is not { } role)
        {
            foreach (var child in ChildElements(element)) Collect(child, into, seen, pending);
            return;
        }
        var isNew = !nodes.TryGetValue(element, out var node);
        if (node is null)
        {
            node = new SemanticNode(nextId++, element, role);
            nodes.Add(element, node);
        }
        node.Role = role;
        seen.Add(element);
        into.Add(node);
        var children = new List<SemanticNode>();

        if (role is SemanticRole.Heading or SemanticRole.DefinitionTerm or SemanticRole.DocumentList or SemanticRole.DocumentListItem or SemanticRole.DefinitionList or SemanticRole.DefinitionDescription or SemanticRole.Form or SemanticRole.Dialog or SemanticRole.AlertDialog or SemanticRole.Table or SemanticRole.RowGroup or SemanticRole.Row or SemanticRole.Cell or SemanticRole.ColumnHeader or SemanticRole.RowHeader or SemanticRole.Group or SemanticRole.Window or SemanticRole.Menu or SemanticRole.MenuBar or SemanticRole.ToolBar or SemanticRole.Calendar or SemanticRole.TitleBar or SemanticRole.List or SemanticRole.Tree or SemanticRole.TabList or SemanticRole.Status)
            foreach (var child in ChildElements(element)) Collect(child, children, seen, pending);

        else if (role == SemanticRole.Text)
        {
            foreach (var child in InlineActions(element)) Collect(child, children, seen, pending);
            foreach (var overlay in OverlaysIn(element)) Collect(overlay, children, seen, pending);
        }
        else
            foreach (var overlay in OverlaysIn(element)) Collect(overlay, children, seen, pending);
        NumberItems(children);
        SyncChildren(node, children, isNew ? null : pending);
        Refresh(node, isNew, pending);
    }

    private IEnumerable<ElementNode> InlineActions(ElementNode element)
    {
        foreach (var child in ChildElements(element))
        {
            if (child.Attributes.GetValueOrDefault("semantic-hidden") is true || scene.GetStyle(child).Display == Display.None) continue;
            if (RoleOf(child) is { } role && (role is not SemanticRole.Text ||
                Trimmed(child.Attributes.GetValueOrDefault("accessible-description")) is not null || Trimmed(child.Attributes.GetValueOrDefault("tooltip")) is not null)) yield return child;
            else foreach (var nested in InlineActions(child)) yield return nested;
        }
    }

    private void SyncChildren(SemanticNode node, List<SemanticNode> children, List<Action>? pending)
    {
        foreach (var child in children) child.Parent = node;
        if (node.ChildList.SequenceEqual(children)) return;
        node.ChildList.Clear();
        node.ChildList.AddRange(children);
        pending?.Add(() => ChildrenChanged?.Invoke(node));
    }

    private void Refresh(SemanticNode node, bool isNew, List<Action> pending)
    {
        double Number(string name) => node.Element?.Attributes.GetValueOrDefault(name) is double number && double.IsFinite(number) ? number : 0;

        var element = node.Element;
        var attributes = element?.Attributes;
        var accessibleName = attributes?.GetValueOrDefault("accessible-name") as string;
        var label = attributes?.GetValueOrDefault("labeled-by") is string { Length: > 0 } labelId ? ElementById(labelId) : null;
        label ??= element is null ? null : SemanticLabel(element);
        var tooltip = Trimmed(attributes?.GetValueOrDefault("tooltip"));
        var name = !string.IsNullOrWhiteSpace(accessibleName) ? accessibleName.Trim() : label is not null ? LabelText(label) : node.Role switch
        {
            SemanticRole.Heading or SemanticRole.DefinitionTerm or SemanticRole.DefinitionDescription or SemanticRole.DocumentListItem or SemanticRole.ToggleButton or SemanticRole.MenuCheckBox or SemanticRole.MenuRadio or SemanticRole.Cell or SemanticRole.ColumnHeader or SemanticRole.RowHeader or SemanticRole.Button or SemanticRole.SplitButton or SemanticRole.Hyperlink or SemanticRole.CheckBox or SemanticRole.RadioButton or SemanticRole.Switch or SemanticRole.ListItem or SemanticRole.TreeItem or SemanticRole.TabItem or SemanticRole.Text or SemanticRole.MenuItem or SemanticRole.ToolTip when element is not null => TextContent(element),
            SemanticRole.Table when element is not null => element.Children.OfType<ElementNode>().FirstOrDefault(child => child.Attributes.GetValueOrDefault("table-part") is "caption") is { } caption ? TextContent(caption) : "",
            SemanticRole.Window => attributes?.GetValueOrDefault("title") as string ?? "",
            SemanticRole.Image => (attributes?.GetValueOrDefault("alt") as string ?? "").Trim(),
            _ => "",
        };

        if (name.Length == 0 && tooltip is not null) name = tooltip;

        var live = element is null ? LiveSetting.Off : LiveOf(element);
        if (name.Length == 0 && live != LiveSetting.Off)
        {
            var message = SemanticDescendants(element!).FirstOrDefault(child => child.Attributes.GetValueOrDefault("live-message") is true);
            var title = Trimmed(attributes?.GetValueOrDefault("live-title"));
            name = message is null && title is null ? TextContent(element!) :
                string.Join(": ", new[] { title, message is null ? null : TextContent(message) }.Where(text => !string.IsNullOrWhiteSpace(text)));
        }
        if (live != LiveSetting.Off && name.Length > 0 && (isNew ? UpdateCount > 0 : name != node.Name)) liveChanges.Add(node);
        var description = Trimmed(attributes?.GetValueOrDefault("accessible-description"));
        if (element is not null && attributes?.GetValueOrDefault("described-by") is not string && DialogPart(element,"dialog-description") is { } dialogDescription)
            description=string.Join(" ",new[] { description,LabelText(dialogDescription) }.Where(value=>!string.IsNullOrWhiteSpace(value)));
        var field = element is null || !IsFieldControl(element) ? null : SemanticAncestors(element).FirstOrDefault(value => value.Attributes.GetValueOrDefault("field") is true);
        if (field is not null && element != field && attributes?.GetValueOrDefault("described-by") is not string)
        {
            var help = SemanticDescendants(field).Where(value => value.Attributes.GetValueOrDefault("field-description") is true &&
                SemanticAncestors(value).FirstOrDefault(parent => parent.Attributes.GetValueOrDefault("field") is true) == field).Select(TextContent);
            description = string.Join(" ", new[] { description }.Concat(help).Where(value => !string.IsNullOrWhiteSpace(value)));
        }
        var tooltipDescription = element is null ? null : SemanticAncestors(element).Skip(1).Select(parent=>parent.Attributes.GetValueOrDefault("tooltip-describes") as string).FirstOrDefault(value=>value is not null);
        var describedBy = string.Join(" ",new[] { attributes?.GetValueOrDefault("described-by") as string,tooltipDescription }.Where(value=>value is not null));
        if (describedBy.Length > 0)
        {
            var details = describedBy.Split(' ',StringSplitOptions.RemoveEmptyEntries).Select(ElementById).OfType<ElementNode>().Select(LabelText);
            description = string.Join(" ",new[] { description }.Concat(details).Where(text=>!string.IsNullOrWhiteSpace(text)));
        }
        if (string.IsNullOrWhiteSpace(description) && tooltip is not null && tooltip != name) description = tooltip;
        var enabled = element is null || input.CanInteract(element);
        var focusable = enabled && (attributes?.GetValueOrDefault("focusable") is true || attributes?.GetValueOrDefault("tabindex") is int);
        bool? isChecked = node.Role is SemanticRole.CheckBox or SemanticRole.RadioButton or SemanticRole.Switch or SemanticRole.ToggleButton or SemanticRole.MenuCheckBox or SemanticRole.MenuRadio ? attributes?.GetValueOrDefault("indeterminate") is true ? null : attributes?.GetValueOrDefault(node.Role == SemanticRole.ToggleButton ? "pressed" : "checked") is true
            : node.Role is SemanticRole.ListItem or SemanticRole.TreeItem or SemanticRole.TabItem ? attributes?.GetValueOrDefault("selected") is true : null;
        var password = node.Role == SemanticRole.TextBox && attributes?.GetValueOrDefault("password") is true;

        var value = node.Role == SemanticRole.TextBox ? password ? "" : attributes?.GetValueOrDefault("value") as string ?? ""
            : node.Role == SemanticRole.ComboBox && element is not null ? ComboValue(element) : null;

        var trigger = element is null ? null : SemanticAncestors(element).Skip(1).FirstOrDefault(value=>value.Attributes.GetValueOrDefault("overlay-trigger") is true);
        bool? expanded = node.Role == SemanticRole.ComboBox ? attributes?.GetValueOrDefault("expanded") is true
            : node.Role is SemanticRole.TreeItem or SemanticRole.Button or SemanticRole.SplitButton or SemanticRole.MenuItem ? attributes?.GetValueOrDefault("expanded") as bool? ?? trigger?.Attributes.GetValueOrDefault("expanded") as bool? : null;

        var ranged = node.Role is SemanticRole.Slider or SemanticRole.ProgressBar or SemanticRole.Meter or SemanticRole.Splitter || (node.Role == SemanticRole.TextBox && attributes?.GetValueOrDefault("spin") is true);
        var range = ranged && attributes?.GetValueOrDefault("range-value") is double current
            ? new RangeInfo(Number("range-min"), Number("range-max"), current, Number("range-step"), Number("range-large-step"), node.Role is SemanticRole.ProgressBar or SemanticRole.Meter || attributes?.GetValueOrDefault("readonly") is true)
            : null;
        var bounds = element is null ? new Rect(0, 0, scene.Viewport.Width, scene.Viewport.Height) : scene.GetBounds(element);
        var offscreen = element is not null && IsOffscreen(element, bounds);

        Set(node, isNew, pending, SemanticProperty.Name, node.Name, name, v => node.Name = v);
        Set(node, isNew, pending, SemanticProperty.IsEnabled, node.IsEnabled, enabled, v => node.IsEnabled = v);
        Set(node, isNew, pending, SemanticProperty.IsFocusable, node.IsFocusable, focusable, v => node.IsFocusable = v);
        Set(node, isNew, pending, SemanticProperty.IsChecked, node.IsChecked, isChecked, v => node.IsChecked = v);
        Set(node, isNew, pending, SemanticProperty.Value, node.Value, value, v => node.Value = v);
        Set(node, isNew, pending, SemanticProperty.IsOffscreen, node.IsOffscreen, offscreen, v => node.IsOffscreen = v);
        Set(node, isNew, pending, SemanticProperty.RangeValue, node.Range?.Value, range?.Value, _ => { });
        Set(node, isNew, pending, SemanticProperty.IsExpanded, node.IsExpanded, expanded, v => node.IsExpanded = v);

        TextSelection? selection = node.Role == SemanticRole.TextBox && !password && element is not null ? input.GetTextSelection(element) : null;
        Set(node, isNew, pending, SemanticProperty.TextSelection, node.TextSelection, selection, v => node.TextSelection = v);
        node.Range = range;
        Set(node, isNew, pending, SemanticProperty.Description, node.Description, description, v => node.Description = v);
        node.HelpText = tooltip is not null && tooltip != name ? tooltip : description;
        node.RoleDescription = Trimmed(attributes?.GetValueOrDefault("role-description"));
        node.LiveSetting = live;
        node.CanSelectMultiple = node.Role == SemanticRole.List && attributes?.GetValueOrDefault("multiselectable") is true;

        node.PositionInSet = attributes?.GetValueOrDefault("position-in-set") as int?;
        node.SizeOfSet = attributes?.GetValueOrDefault("size-of-set") as int?;
        node.LabeledBy = null;
        if (label is not null) labeled.Add((node, label));
        node.IsPassword = password;
        node.Level = attributes?.GetValueOrDefault("level") as int?;
        node.HeadingLevel = node.Role == SemanticRole.Heading ? node.Level : null;
        Set(node, isNew, pending, SemanticProperty.IsRequired, node.IsRequired, attributes?.GetValueOrDefault("required") is true || field?.Attributes.GetValueOrDefault("required") is true, v => node.IsRequired = v);
        Set(node, isNew, pending, SemanticProperty.IsInvalid, node.IsInvalid, attributes?.GetValueOrDefault("invalid") is true || field?.Attributes.GetValueOrDefault("invalid") is true, v => node.IsInvalid = v);
        var tableLayout = element is null ? null : scene.GetLayoutNode(element);
        node.RowIndex = tableLayout?.TableRow ?? attributes?.GetValueOrDefault("row-index") as int?;
        node.ColumnIndex = tableLayout?.TableColumn ?? attributes?.GetValueOrDefault("column-index") as int?;
        node.RowSpan = tableLayout?.Style.TablePart == "cell" ? tableLayout.TableRowSpan : attributes?.GetValueOrDefault("row-span") as int? ?? 1;
        node.ColumnSpan = attributes?.GetValueOrDefault("column-span") as int? ?? 1;
        node.RowCount = tableLayout?.TableRows ?? attributes?.GetValueOrDefault("row-count") as int?;
        node.ColumnCount = tableLayout?.TableColumns ?? attributes?.GetValueOrDefault("column-count") as int?;
        node.AutomationId = attributes?.GetValueOrDefault("id") as string;
        node.HasFocus = element is not null && input.Focused == element;
        node.Bounds = bounds;
        var scrollElement = element?.Name == "Window" ? ChildElements(element).FirstOrDefault(child => child.Name == "WindowContent") ?? element : element;
        var scroll = scrollElement is not null ? scene.GetScrollState(scrollElement) : null;
        node.ScrollElement = scrollElement;
        var horizontal = scroll?.CanScrollX == true;
        var vertical = scroll?.CanScrollY == true;
        var metrics = (X: horizontal ? (double)scroll!.OffsetX / scroll.MaxOffsetX * 100 : -1,
            Y: vertical ? (double)scroll!.OffsetY / scroll.MaxOffsetY * 100 : -1,
            Width: horizontal ? (double)scroll!.ViewportWidth / scroll.ContentWidth * 100 : 100,
            Height: vertical ? (double)scroll!.ViewportHeight / scroll.ContentHeight * 100 : 100);
        Set(node, isNew, pending, SemanticProperty.ScrollAvailable, node.Scroll is not null, scroll is not null, _ => { });
        Set(node, isNew, pending, SemanticProperty.HorizontalScrollPercent, node.ScrollMetrics.X, metrics.X, _ => { });
        Set(node, isNew, pending, SemanticProperty.VerticalScrollPercent, node.ScrollMetrics.Y, metrics.Y, _ => { });
        Set(node, isNew, pending, SemanticProperty.HorizontalViewSize, node.ScrollMetrics.Width, metrics.Width, _ => { });
        Set(node, isNew, pending, SemanticProperty.VerticalViewSize, node.ScrollMetrics.Height, metrics.Height, _ => { });
        Set(node, isNew, pending, SemanticProperty.HorizontallyScrollable, node.ScrollMetrics.Horizontal, horizontal, _ => { });
        Set(node, isNew, pending, SemanticProperty.VerticallyScrollable, node.ScrollMetrics.Vertical, vertical, _ => { });
        node.ScrollMetrics = (metrics.X, metrics.Y, metrics.Width, metrics.Height, horizontal, vertical);
        node.Scroll = scroll;
    }

    private static LiveSetting LiveOf(ElementNode element) => element.Attributes.GetValueOrDefault("live") switch
    {
        "polite" => LiveSetting.Polite,
        "assertive" => LiveSetting.Assertive,
        _ => LiveSetting.Off,
    };

    private static string? Trimmed(object? value) => value is string text && !string.IsNullOrWhiteSpace(text) ? text.Trim() : null;

    private static IEnumerable<ElementNode> SemanticAncestors(ElementNode element)
    {
        for (var node = element; node is not null; node = node.ParentElement) yield return node;
    }
    private static IEnumerable<ElementNode> SemanticDescendants(ElementNode element)
    {
        foreach (var child in ChildElements(element))
        {
            yield return child;
            foreach (var nested in SemanticDescendants(child)) yield return nested;
        }
    }
    private static bool IsFieldControl(ElementNode element) => element.Attributes.GetValueOrDefault("role") is "textbox" or "spinbutton" or "searchbox" or "checkbox" or "switch" or "radiogroup" or "combobox" or "slider" or "listbox" or "tree" or "togglebutton";

    private static ElementNode? SemanticLabel(ElementNode element)
    {
        if (DialogPart(element,"dialog-title") is { } title) return title;
        if (element.Attributes.GetValueOrDefault("id") is string id && SemanticDescendants(SemanticAncestors(element).Last())
            .FirstOrDefault(value => Equals(value.Attributes.GetValueOrDefault("label-for"), id)) is { } direct) return direct;
        if (!IsFieldControl(element) && element.Attributes.GetValueOrDefault("field") is not true && element.Attributes.GetValueOrDefault("fieldset") is not true) return null;
        var owner = element.Attributes.GetValueOrDefault("fieldset") is true || element.Attributes.GetValueOrDefault("field") is true ? element
            : SemanticAncestors(element).Skip(1).FirstOrDefault(value => value.Attributes.GetValueOrDefault("field") is true);
        return owner is null ? null : SemanticDescendants(owner).FirstOrDefault(value =>
            (value.Attributes.GetValueOrDefault("label") is true || value.Attributes.GetValueOrDefault("legend") is true) &&
            SemanticAncestors(value).Skip(1).FirstOrDefault(parent => parent.Attributes.GetValueOrDefault("field") is true || parent.Attributes.GetValueOrDefault("fieldset") is true) == owner);
    }
    private static ElementNode? DialogPart(ElementNode element,string marker) => element.Attributes.GetValueOrDefault("role") is "dialog" or "alertdialog"
        ? SemanticDescendants(element).FirstOrDefault(value=>value.Attributes.GetValueOrDefault(marker) is true &&
            SemanticAncestors(value).Skip(1).FirstOrDefault(parent=>parent.Attributes.GetValueOrDefault("role") is "dialog" or "alertdialog")==element)
        : null;
    private static string LabelText(ElementNode label) =>
        Trimmed(label.Attributes.GetValueOrDefault("accessible-name")) ?? TextContent(label);

    private ElementNode? ElementById(string id)
    {
        if (ids is null)
        {
            ids = new Dictionary<string, ElementNode>(StringComparer.Ordinal);
            foreach (var root in renderer.RootComponents.SelectMany(r => r.Nodes).OfType<ElementNode>()) Index(root, ids);
        }
        return ids.GetValueOrDefault(id);

        static void Index(ElementNode element, Dictionary<string, ElementNode> ids)
        {
            if (element.Attributes.GetValueOrDefault("id") is string { Length: > 0 } key) ids.TryAdd(key, element);
            foreach (var child in ChildElements(element)) Index(child, ids);
        }
    }

    private static void NumberItems(List<SemanticNode> siblings)
    {
        Dictionary<SemanticRole, int>? sizes = null;
        foreach (var node in siblings)
            if (node.Role is SemanticRole.DocumentListItem or SemanticRole.ListItem or SemanticRole.TabItem or SemanticRole.RadioButton or SemanticRole.MenuItem or SemanticRole.MenuCheckBox or SemanticRole.MenuRadio)
            {
                sizes ??= [];
                var role = ItemSetRole(node.Role);
                var position = sizes[role] = sizes.GetValueOrDefault(role) + 1;
                node.PositionInSet ??= position;
            }
        if (sizes is not null)
            foreach (var node in siblings)
                if (sizes.TryGetValue(ItemSetRole(node.Role), out var size)) node.SizeOfSet ??= size;

        for (var i = 0; i < siblings.Count; i++)
        {
            if (siblings[i] is not { Role: SemanticRole.TreeItem, PositionInSet: null, Level: { } level } item) continue;
            var (start, end) = (i, i);
            while (start > 0 && siblings[start - 1] is { Role: SemanticRole.TreeItem, Level: { } before } && before >= level) start--;
            while (end < siblings.Count - 1 && siblings[end + 1] is { Role: SemanticRole.TreeItem, Level: { } after } && after >= level) end++;
            var (position, count) = (0, 0);
            for (var j = start; j <= end; j++)
            {
                if (siblings[j].Level != level) continue;
                count++;
                if (j <= i) position++;
            }
            (item.PositionInSet, item.SizeOfSet) = (position, count);
        }
    }

    private static SemanticRole ItemSetRole(SemanticRole role) => role is SemanticRole.MenuCheckBox or SemanticRole.MenuRadio ? SemanticRole.MenuItem : role;

    private void Set<T>(SemanticNode node, bool isNew, List<Action> pending, SemanticProperty property, T current, T value, Action<T> assign)
    {
        if (EqualityComparer<T>.Default.Equals(current, value)) return;
        assign(value);

        if (!isNew) pending.Add(() => PropertyChanged?.Invoke(node, property, current, value));
    }

    private bool IsOffscreen(ElementNode element, Rect bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return true;
        var visible = Intersect(bounds, scene.GetElementViewport(element));
        for (var ancestor = element.ParentElement; ancestor is not null && visible is not null && !Scene.IsOverlay(element); ancestor = ancestor.ParentElement)
        {
            if (scene.GetScrollState(ancestor) is not null) visible = Intersect(visible.Value, scene.GetBounds(ancestor));
            if (Scene.IsOverlay(ancestor)) break;
        }
        return visible is null;
    }

    private static Rect? Intersect(Rect a, Rect b)
    {
        var left = Math.Max(a.X, b.X);
        var top = Math.Max(a.Y, b.Y);
        var right = Math.Min(a.Right, b.Right);
        var bottom = Math.Min(a.Bottom, b.Bottom);
        return right > left && bottom > top ? new Rect(left, top, right - left, bottom - top) : null;
    }

    private static SemanticRole? RoleOf(ElementNode element) => element.Attributes.GetValueOrDefault("role") switch
    {
        "list" => SemanticRole.DocumentList,
        "listitem" => SemanticRole.DocumentListItem,
        "definitionlist" => SemanticRole.DefinitionList,
        "term" => SemanticRole.DefinitionTerm,
        "definition" => SemanticRole.DefinitionDescription,
        "heading" => SemanticRole.Heading,
        "form" => SemanticRole.Form,
        "dialog" => SemanticRole.Dialog,
        "alertdialog" => SemanticRole.AlertDialog,
        "togglebutton" => SemanticRole.ToggleButton,
        "meter" => SemanticRole.Meter,
        "table" => SemanticRole.Table,
        "rowgroup" => SemanticRole.RowGroup,
        "row" => SemanticRole.Row,
        "cell" => SemanticRole.Cell,
        "columnheader" => SemanticRole.ColumnHeader,
        "rowheader" => SemanticRole.RowHeader,
        "menuitemcheckbox" => SemanticRole.MenuCheckBox,
        "menuitemradio" => SemanticRole.MenuRadio,
        "separator" => SemanticRole.Separator,
        "button" => SemanticRole.Button,
        "checkbox" => SemanticRole.CheckBox,
        "radio" => SemanticRole.RadioButton,
        "switch" => SemanticRole.Switch,
        "slider" => SemanticRole.Slider,
        "progressbar" => SemanticRole.ProgressBar,
        "combobox" => SemanticRole.ComboBox,
        "listbox" => SemanticRole.List,
        "option" => SemanticRole.ListItem,
        "tree" => SemanticRole.Tree,
        "treeitem" => SemanticRole.TreeItem,
        "tablist" => SemanticRole.TabList,
        "tab" => SemanticRole.TabItem,
        "tabpanel" => SemanticRole.Group,
        "splitter" => SemanticRole.Splitter,
        "link" => SemanticRole.Hyperlink,
        "status" => SemanticRole.Status,
        "splitbutton" => SemanticRole.SplitButton,
        "menubar" => SemanticRole.MenuBar,
        "toolbar" => SemanticRole.ToolBar,
        "calendar" => SemanticRole.Calendar,
        "titlebar" => SemanticRole.TitleBar,
        "radiogroup" => SemanticRole.Group,
        "textbox" or "spinbutton" or "searchbox" => SemanticRole.TextBox,
        "text" => SemanticRole.Text,
        "image" => SemanticRole.Image,
        "menu" => SemanticRole.Menu,
        "menuitem" => SemanticRole.MenuItem,
        "tooltip" => SemanticRole.ToolTip,
        "group" => SemanticRole.Group,

        "window" => SemanticRole.Group,
        _ => null,
    };

    private static IEnumerable<ElementNode> ChildElements(ElementNode element) => element.Children.OfType<ElementNode>();

    private static IEnumerable<ElementNode> OverlaysIn(ElementNode element)
    {
        foreach (var child in ChildElements(element))
        {
            if (IsOverlay(child)) yield return child;
            else foreach (var nested in OverlaysIn(child)) yield return nested;
        }
    }

    private static bool IsOverlay(ElementNode element) => element.Attributes.GetValueOrDefault("overlay") is true;

    private static ElementNode ExpansionTarget(ElementNode element) =>
        element.Attributes.GetValueOrDefault("role") is "combobox" && element.ParentElement is { } parent
            ? ChildElements(parent).FirstOrDefault(child => child.Attributes.GetValueOrDefault("combo-expander") is true) ?? element
            : element;

    private static string ComboValue(ElementNode combo) =>
        combo.Attributes.GetValueOrDefault("value") as string ??
        (ChildElements(combo).FirstOrDefault(c => c.Attributes.GetValueOrDefault("combo-value") is true) is { } part && part.Attributes.GetValueOrDefault("placeholder") is not true
            ? TextContent(part) : "");

    private static string TextContent(ElementNode element)
    {
        var text = new StringBuilder();
        Append(element);

        var words = string.Concat(text.ToString().EnumerateRunes()
            .Where(rune => Rune.GetUnicodeCategory(rune) != System.Globalization.UnicodeCategory.PrivateUse).Select(rune => rune.ToString()));
        return string.Join(' ', words.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        void Append(ElementNode parent)
        {
            foreach (var child in parent.Children)
            {
                if (child is TextNode textNode) text.Append(textNode.Text);

                else if (child is ElementNode nested && !IsOverlay(nested) && nested.Attributes.GetValueOrDefault("semantic-hidden") is not true && nested.Attributes.GetValueOrDefault("tooltip-surface") is not true) Append(nested);
            }
        }
    }

    private void OnBatchCompleted(object? sender, RenderBatch batch)
    {
        if (!batch.IsEmpty) dirty = true;
    }

    /// <summary>Releases the resources and event subscriptions owned by this instance.</summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        renderer.BatchCompleted -= OnBatchCompleted;
        input.VisualStateChanged -= Invalidate;
        foreach (var node in nodes.Values) node.IsDetached = true;
        nodes.Clear();
    }
}
