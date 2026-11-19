using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Nazor.Accessibility;
using Nazor.Presentation;

namespace Nazor.Platform.Windows.Automation;

[GeneratedComClass]
internal partial class AutomationNode(AutomationBridge bridge, SemanticNode node)
    : IRawElementProviderSimple, IRawElementProviderFragment, IInvokeProvider, IToggleProvider, IValueProvider, IScrollProvider,
      ISelectionItemProvider, IRangeValueProvider, IExpandCollapseProvider, ISelectionProvider, ITextProvider2,
      IGridProvider, IGridItemProvider, ITableProvider, ITableItemProvider
{
    private const float SmallScrollStep = 48;

    protected AutomationBridge Bridge => bridge;

    public SemanticNode Node => node;

    public ProviderOptions GetProviderOptions() =>
        ProviderOptions.ServerSideProvider | ProviderOptions.ProviderOwnsSetFocus | ProviderOptions.UseComThreading;

    public virtual object? GetPatternProvider(int patternId) => Ui(() => (patternId, node.Role) switch
    {
        (10006 or 10012, SemanticRole.Table) => this,
        (10007 or 10013, SemanticRole.Cell or SemanticRole.ColumnHeader or SemanticRole.RowHeader) => this,
        (Uia.InvokePattern, SemanticRole.Button or SemanticRole.MenuItem or SemanticRole.Hyperlink or SemanticRole.SplitButton) => this,
        (Uia.TogglePattern, SemanticRole.CheckBox or SemanticRole.Switch or SemanticRole.ToggleButton or SemanticRole.MenuCheckBox or SemanticRole.MenuRadio) => this,
        (Uia.SelectionItemPattern, SemanticRole.RadioButton or SemanticRole.ListItem or SemanticRole.TreeItem or SemanticRole.TabItem) => this,
        (Uia.RangeValuePattern, SemanticRole.Slider or SemanticRole.ProgressBar or SemanticRole.Meter or SemanticRole.TextBox or SemanticRole.Splitter) when node.Range is not null => this,
        (Uia.ValuePattern, SemanticRole.TextBox or SemanticRole.ComboBox) => this,
        (Uia.ExpandCollapsePattern, SemanticRole.ComboBox or SemanticRole.TreeItem or SemanticRole.SplitButton) => this,

        (Uia.ExpandCollapsePattern, SemanticRole.Button or SemanticRole.MenuItem) when node.IsExpanded is not null => this,
        (Uia.ScrollPattern, _) when node.Scroll is not null => this,
        (Uia.SelectionPattern, SemanticRole.List or SemanticRole.Tree or SemanticRole.TabList or SemanticRole.ComboBox) => this,

        (Uia.TextPattern or Uia.TextPattern2, SemanticRole.TextBox) when node.TextSelection is not null => this,
        _ => null,
    });

    public IRawElementProviderSimple? GetItem(int row, int column) => Ui(() =>
    {
        if (row < 0 || column < 0 || row >= node.RowCount || column >= node.ColumnCount) throw new ArgumentOutOfRangeException(nameof(row));
        return bridge.Tree.TableCellAt(node, row, column) is { } cell ? (IRawElementProviderSimple)bridge.Wrap(cell) : null;
    });
    public int GetRowCount() => Ui(() => node.RowCount ?? 0);
    public int GetColumnCount() => Ui(() => node.ColumnCount ?? 0);
    public int GetRow() => Ui(() => node.RowIndex ?? 0);
    public int GetColumn() => Ui(() => node.ColumnIndex ?? 0);
    public int GetRowSpan() => Ui(() => node.RowSpan);
    public int GetColumnSpan() => Ui(() => node.ColumnSpan);
    public IRawElementProviderSimple? GetContainingGrid() => Ui(() => bridge.Tree.ContainingTable(node) is { } table ? (IRawElementProviderSimple)bridge.Wrap(table) : null);
    public nint GetRowHeaders() => Headers(false);
    public nint GetColumnHeaders() => Headers(true);
    public nint GetRowHeaderItems() => Headers(false);
    public nint GetColumnHeaderItems() => Headers(true);
    public int GetRowOrColumnMajor() => 0;
    private nint Headers(bool columns) => Ui(() => Uia.CreateProviderArray(bridge.Tree.TableHeaders(node, columns).Select(header => (IRawElementProviderSimple)bridge.Wrap(header)).ToArray()));

    public Variant GetPropertyValue(int propertyId) => Variant.FromObject(Ui<object?>(() => propertyId switch
    {
        Uia.ControlTypeProperty => node is { Role: SemanticRole.Group, Scroll: not null } ? Uia.PaneControlType : ControlType(node.Role),
        Uia.LocalizedControlTypeProperty when node.RoleDescription is { } description => description,
        Uia.LocalizedControlTypeProperty when node.Role == SemanticRole.Meter => "meter",
        Uia.NameProperty when node.Name.Length > 0 => node.Name,
        Uia.HelpTextProperty => node.HelpText,
        Uia.FullDescriptionProperty => node.Description,
        30174 => node.Role is SemanticRole.Dialog or SemanticRole.AlertDialog,
        Uia.LabeledByProperty when node.LabeledBy is { IsDetached: false } label => (IRawElementProviderSimple)bridge.Wrap(label),
        Uia.PositionInSetProperty when node.PositionInSet is { } position => position,
        Uia.SizeOfSetProperty when node.SizeOfSet is { } size => size,
        Uia.LiveSettingProperty when node.LiveSetting != LiveSetting.Off => (int)node.LiveSetting,
        Uia.AutomationIdProperty => node.AutomationId,
        Uia.ClassNameProperty => node.Element?.Name,
        Uia.IsScrollPatternAvailableProperty => node.Scroll is not null,
        Uia.HorizontalScrollPercentProperty => node.Scroll is { CanScrollX: true } sx ? (double)sx.OffsetX / sx.MaxOffsetX * 100 : Uia.NoScroll,
        Uia.VerticalScrollPercentProperty => node.Scroll is { CanScrollY: true } sy ? (double)sy.OffsetY / sy.MaxOffsetY * 100 : Uia.NoScroll,
        Uia.HorizontalViewSizeProperty => node.Scroll is { CanScrollX: true } vx ? (double)vx.ViewportWidth / vx.ContentWidth * 100 : 100d,
        Uia.VerticalViewSizeProperty => node.Scroll is { CanScrollY: true } vy ? (double)vy.ViewportHeight / vy.ContentHeight * 100 : 100d,
        Uia.HorizontallyScrollableProperty => node.Scroll is { CanScrollX: true },
        Uia.VerticallyScrollableProperty => node.Scroll is { CanScrollY: true },
        Uia.IsEnabledProperty => node.IsEnabled,
        Uia.IsKeyboardFocusableProperty => node.IsFocusable,
        Uia.HasKeyboardFocusProperty => node.HasFocus,
        Uia.IsOffscreenProperty => node.IsOffscreen,
        Uia.IsPasswordProperty when node.Role == SemanticRole.TextBox => node.IsPassword,
        30173 when node.HeadingLevel is { } level => 80050 + level,
        30025 => node.IsRequired,
        30026 => !node.IsInvalid,
        Uia.FrameworkIdProperty => "Nazor",
        Uia.LevelProperty when node.Level is { } level => level,
        _ => null,
    }));

    public virtual nint GetHostRawElementProvider() => 0;

    public IRawElementProviderFragment? Navigate(NavigateDirection direction) => Ui(() =>
    {
        var target = direction switch
        {
            NavigateDirection.Parent => node.Parent,
            NavigateDirection.FirstChild => node.Children.Count > 0 ? node.Children[0] : null,
            NavigateDirection.LastChild => node.Children.Count > 0 ? node.Children[^1] : null,
            NavigateDirection.NextSibling => Sibling(+1),
            NavigateDirection.PreviousSibling => Sibling(-1),
            _ => null,
        };
        return target is null ? null : (IRawElementProviderFragment)bridge.Wrap(target);
    });

    public virtual nint GetRuntimeId() => Uia.CreateRuntimeId(node.Id);

    public virtual UiaRect GetBoundingRectangle() => Ui(() => bridge.ToScreen(node.Bounds));

    public nint GetEmbeddedFragmentRoots() => 0;

    public virtual void SetFocus() => Ui(() =>
    {
        if (!bridge.Tree.Focus(node)) throw new COMException("The element cannot take the focus.", Uia.E_INVALIDOPERATION);
        bridge.FocusWindow();
    });

    public virtual IRawElementProviderFragmentRoot? GetFragmentRoot() => bridge.Root;

    void IInvokeProvider.Invoke() => Ui(() =>
    {
        EnsureEnabled();
        if (!bridge.Tree.Invoke(node)) throw new COMException("The button cannot be invoked.", Uia.E_INVALIDOPERATION);
    });

    void IToggleProvider.Toggle() => Ui(() =>
    {
        EnsureEnabled();
        if (!bridge.Tree.Toggle(node)) throw new COMException("The check box cannot be toggled.", Uia.E_INVALIDOPERATION);
    });

    ToggleState IToggleProvider.GetToggleState() => Ui(() => node.IsChecked == true ? ToggleState.On : node.IsChecked is null ? ToggleState.Indeterminate : ToggleState.Off);

    void ISelectionItemProvider.Select() => Ui(() =>
    {
        EnsureEnabled();
        if (!bridge.Tree.Select(node)) throw new COMException("The radio button cannot be selected.", Uia.E_INVALIDOPERATION);
    });

    void ISelectionItemProvider.AddToSelection() => Ui(() =>
    {
        if (node.IsChecked == true) return;
        EnsureEnabled();
        if (!bridge.Tree.SetSelected(node, true)) throw new COMException("The item cannot be added to the selection.", Uia.E_INVALIDOPERATION);
    });

    void ISelectionItemProvider.RemoveFromSelection() => Ui(() =>
    {
        if (node.IsChecked != true) return;
        EnsureEnabled();
        if (!bridge.Tree.SetSelected(node, false)) throw new COMException("The item cannot be removed from the selection.", Uia.E_INVALIDOPERATION);
    });

    bool ISelectionItemProvider.GetIsSelected() => Ui(() => node.IsChecked == true);

    IRawElementProviderSimple? ISelectionItemProvider.GetSelectionContainer() => Ui(() =>
        bridge.Tree.SelectionContainer(node) is { } container ? (IRawElementProviderSimple)bridge.Wrap(container) : null);

    nint ISelectionProvider.GetSelection() => Ui(() =>
        Uia.CreateProviderArray([.. bridge.Tree.SelectedItems(node).Select(item => (IRawElementProviderSimple)bridge.Wrap(item))]));

    bool ISelectionProvider.GetCanSelectMultiple() => Ui(() => node.CanSelectMultiple);

    bool ISelectionProvider.GetIsSelectionRequired() => Ui(() => node.Role == SemanticRole.TabList);

    nint ITextProvider.GetSelection() => Ui(() =>
        Uia.CreateUnknownArray<ITextRangeProvider>([Range(node.TextSelection is { } selection ? (selection.Start, selection.End) : (0, 0))]));

    nint ITextProvider.GetVisibleRanges() => Ui(() => Uia.CreateUnknownArray<ITextRangeProvider>([Range((0, int.MaxValue))]));

    ITextRangeProvider? ITextProvider.RangeFromChild(IRawElementProviderSimple childElement) =>
        throw new COMException("A text box has no children in its text.", Uia.E_INVALIDOPERATION);

    ITextRangeProvider? ITextProvider.RangeFromPoint(UiaPoint point) => Ui(() =>
    {
        var index = bridge.Tree.TextIndexAt(node, bridge.ToClient(point.X, point.Y));
        return Range((index, index));
    });

    ITextRangeProvider ITextProvider.GetDocumentRange() => Ui(() => Range((0, int.MaxValue)));

    SupportedTextSelection ITextProvider.GetSupportedTextSelection() => SupportedTextSelection.Single;

    ITextRangeProvider? ITextProvider2.RangeFromAnnotation(IRawElementProviderSimple annotationElement) =>
        throw new COMException("A text box has no annotations.", Uia.E_INVALIDOPERATION);

    ITextRangeProvider ITextProvider2.GetCaretRange(out bool isActive)
    {
        var (range, active) = Ui(() => (Range(node.TextSelection is { } selection ? (selection.Caret, selection.Caret) : (0, 0)), node.HasFocus));
        isActive = active;
        return range;
    }

    private TextRangeProvider Range((int Start, int End) range) => new(bridge, node, range.Start, range.End);

    void IExpandCollapseProvider.Expand() => Ui(() =>
    {
        EnsureEnabled();
        bridge.Tree.SetExpanded(node, true);
    });

    void IExpandCollapseProvider.Collapse() => Ui(() =>
    {
        EnsureEnabled();
        bridge.Tree.SetExpanded(node, false);
    });

    ExpandCollapseState IExpandCollapseProvider.GetExpandCollapseState() => Ui(() => node.IsExpanded switch
    {
        true => ExpandCollapseState.Expanded,
        false => ExpandCollapseState.Collapsed,

        null => ExpandCollapseState.LeafNode,
    });

    void IRangeValueProvider.SetValue(double value) => Ui(() =>
    {
        EnsureEnabled();
        var range = Range();
        if (range.IsReadOnly) throw new COMException("The value is read-only.", Uia.E_INVALIDOPERATION);
        if (!double.IsFinite(value) || value < range.Minimum || value > range.Maximum) throw new ArgumentOutOfRangeException(nameof(value));
        bridge.Tree.SetRangeValue(node, value);
    });

    double IRangeValueProvider.GetValue() => Ui(() => Range().Value);

    bool IRangeValueProvider.GetIsReadOnly() => Ui(() => Range().IsReadOnly);

    double IRangeValueProvider.GetMaximum() => Ui(() => Range().Maximum);

    double IRangeValueProvider.GetMinimum() => Ui(() => Range().Minimum);

    double IRangeValueProvider.GetLargeChange() => Ui(() => Range().LargeChange);

    double IRangeValueProvider.GetSmallChange() => Ui(() => Range().SmallChange);

    private RangeInfo Range() => node.Range ?? throw new COMException("The element has no range.", Uia.E_ELEMENTNOTAVAILABLE);

    void IValueProvider.SetValue(string? value) => Ui(() =>
    {
        EnsureEnabled();
        if (!bridge.Tree.SetValue(node, value ?? "")) throw new COMException("The value cannot be set.", Uia.E_INVALIDOPERATION);
    });

    string? IValueProvider.GetValue() => Ui(() => node.Value ?? "");

    bool IValueProvider.GetIsReadOnly() => Ui(() => node.Role == SemanticRole.ComboBox || node.Element?.Attributes.GetValueOrDefault("readonly") is true);

    void IScrollProvider.Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount) => Ui(() =>
    {
        var scroll = Scrollable();
        if (horizontalAmount != ScrollAmount.NoAmount && !scroll.CanScrollX) throw new COMException("The element cannot scroll horizontally.", Uia.E_INVALIDOPERATION);
        if (verticalAmount != ScrollAmount.NoAmount && !scroll.CanScrollY) throw new COMException("The element cannot scroll vertically.", Uia.E_INVALIDOPERATION);
        var deltaX = Step(horizontalAmount, scroll.ViewportWidth);
        var deltaY = Step(verticalAmount, scroll.ViewportHeight);
        if (deltaX != 0 || deltaY != 0) bridge.Tree.ScrollBy(node, deltaX, deltaY);
    });

    private static float Step(ScrollAmount amount, float viewport) => amount switch
    {
        ScrollAmount.SmallIncrement => SmallScrollStep,
        ScrollAmount.SmallDecrement => -SmallScrollStep,
        ScrollAmount.LargeIncrement => viewport * 0.9f,
        ScrollAmount.LargeDecrement => -viewport * 0.9f,
        _ => 0,
    };

    void IScrollProvider.SetScrollPercent(double horizontalPercent, double verticalPercent) => Ui(() =>
    {
        var scroll = Scrollable();
        if (horizontalPercent != Uia.NoScroll && !scroll.CanScrollX) throw new COMException("The element cannot scroll horizontally.", Uia.E_INVALIDOPERATION);
        if (verticalPercent != Uia.NoScroll && !scroll.CanScrollY) throw new COMException("The element cannot scroll vertically.", Uia.E_INVALIDOPERATION);
        if (horizontalPercent != Uia.NoScroll && (!double.IsFinite(horizontalPercent) || horizontalPercent is < 0 or > 100)) throw new ArgumentOutOfRangeException(nameof(horizontalPercent));
        if (verticalPercent != Uia.NoScroll && (!double.IsFinite(verticalPercent) || verticalPercent is < 0 or > 100)) throw new ArgumentOutOfRangeException(nameof(verticalPercent));
        bridge.Tree.ScrollTo(node, horizontalPercent == Uia.NoScroll ? null : horizontalPercent / 100 * scroll.MaxOffsetX,
            verticalPercent == Uia.NoScroll ? null : verticalPercent / 100 * scroll.MaxOffsetY);
    });

    double IScrollProvider.GetHorizontalScrollPercent() =>
        Ui(() => node.Scroll is { CanScrollX: true } scroll ? scroll.OffsetX / scroll.MaxOffsetX * 100 : Uia.NoScroll);

    double IScrollProvider.GetVerticalScrollPercent() =>
        Ui(() => node.Scroll is { CanScrollY: true } scroll ? scroll.OffsetY / scroll.MaxOffsetY * 100 : Uia.NoScroll);

    double IScrollProvider.GetHorizontalViewSize() =>
        Ui(() => node.Scroll is { CanScrollX: true } scroll ? scroll.ViewportWidth / scroll.ContentWidth * 100 : 100);

    double IScrollProvider.GetVerticalViewSize() =>
        Ui(() => node.Scroll is { CanScrollY: true } scroll ? scroll.ViewportHeight / scroll.ContentHeight * 100 : 100);

    bool IScrollProvider.GetHorizontallyScrollable() => Ui(() => node.Scroll is { CanScrollX: true });

    bool IScrollProvider.GetVerticallyScrollable() => Ui(() => node.Scroll is { CanScrollY: true });

    protected T Ui<T>(Func<T> call) => bridge.OnUiThread(() =>
    {
        if (!node.IsDetached) bridge.Tree.EnsureCurrent();
        if (node.IsDetached) throw AutomationBridge.ElementNotAvailable();
        return call();
    });

    protected void Ui(Action call) => Ui(() =>
    {
        call();
        return true;
    });

    private void EnsureEnabled()
    {
        if (!node.IsEnabled) throw new COMException("The element is not enabled.", Uia.E_ELEMENTNOTENABLED);
    }

    private ScrollState Scrollable()
    {
        EnsureEnabled();
        return node.Scroll is { CanScroll: true } scroll ? scroll : throw new COMException("The element cannot scroll.", Uia.E_INVALIDOPERATION);
    }

    private SemanticNode? Sibling(int offset)
    {
        if (node.Parent is not { } parent) return null;
        var index = IndexOf(parent.Children, node) + offset;
        return index >= 0 && index < parent.Children.Count ? parent.Children[index] : null;
    }

    private static int IndexOf(IReadOnlyList<SemanticNode> list, SemanticNode item)
    {
        for (var i = 0; i < list.Count; i++)
            if (list[i] == item) return i;
        return -1;
    }

    private static int ControlType(SemanticRole role) => role switch
    {
        SemanticRole.DocumentList => Uia.ListControlType,
        SemanticRole.DocumentListItem => Uia.ListItemControlType,
        SemanticRole.DefinitionList => Uia.GroupControlType,
        SemanticRole.DefinitionTerm => Uia.TextControlType,
        SemanticRole.DefinitionDescription => Uia.GroupControlType,
        SemanticRole.Heading => Uia.TextControlType,
        SemanticRole.Form => Uia.GroupControlType,
        SemanticRole.Dialog => Uia.WindowControlType,
        SemanticRole.AlertDialog => Uia.WindowControlType,
        SemanticRole.ToggleButton => Uia.ButtonControlType,
        SemanticRole.Meter => Uia.ProgressBarControlType,
        SemanticRole.Table => Uia.TableControlType,
        SemanticRole.RowGroup => Uia.GroupControlType,
        SemanticRole.Row => Uia.GroupControlType,
        SemanticRole.Cell => Uia.DataItemControlType,
        SemanticRole.ColumnHeader => Uia.HeaderItemControlType,
        SemanticRole.RowHeader => Uia.HeaderItemControlType,
        SemanticRole.MenuCheckBox => Uia.MenuItemControlType,
        SemanticRole.MenuRadio => Uia.MenuItemControlType,
        SemanticRole.Separator => Uia.SeparatorControlType,
        SemanticRole.Button => Uia.ButtonControlType,
        SemanticRole.CheckBox => Uia.CheckBoxControlType,
        SemanticRole.TextBox => Uia.EditControlType,
        SemanticRole.Text => Uia.TextControlType,
        SemanticRole.Group => Uia.GroupControlType,
        SemanticRole.Image => Uia.ImageControlType,
        SemanticRole.Menu => Uia.MenuControlType,
        SemanticRole.RadioButton => Uia.RadioButtonControlType,
        SemanticRole.Slider => Uia.SliderControlType,
        SemanticRole.ComboBox => Uia.ComboBoxControlType,
        SemanticRole.List => Uia.ListControlType,
        SemanticRole.ListItem => Uia.ListItemControlType,
        SemanticRole.Tree => Uia.TreeControlType,
        SemanticRole.TreeItem => Uia.TreeItemControlType,
        SemanticRole.TabList => Uia.TabControlType,
        SemanticRole.TabItem => Uia.TabItemControlType,

        SemanticRole.Splitter => Uia.ThumbControlType,
        SemanticRole.Hyperlink => Uia.HyperlinkControlType,
        SemanticRole.SplitButton => Uia.SplitButtonControlType,
        SemanticRole.MenuBar => Uia.MenuBarControlType,
        SemanticRole.ToolBar => Uia.ToolBarControlType,
        SemanticRole.Calendar => Uia.CalendarControlType,
        SemanticRole.TitleBar => Uia.TitleBarControlType,

        SemanticRole.Status => Uia.StatusBarControlType,
        SemanticRole.ProgressBar => Uia.ProgressBarControlType,

        SemanticRole.Switch => Uia.ButtonControlType,
        SemanticRole.MenuItem => Uia.MenuItemControlType,
        SemanticRole.ToolTip => Uia.ToolTipControlType,
        _ => Uia.WindowControlType,
    };
}

[GeneratedComClass]
internal sealed partial class RootAutomationNode(AutomationBridge bridge, SemanticNode node) : AutomationNode(bridge, node), IRawElementProviderFragmentRoot
{
    public override nint GetHostRawElementProvider() =>
        Uia.UiaHostProviderFromHwnd(Bridge.Handle, out var provider) >= 0 ? provider : 0;

    public override nint GetRuntimeId() => 0;

    public override UiaRect GetBoundingRectangle() => default;

    public override void SetFocus() => Ui(Bridge.FocusWindow);

    public override IRawElementProviderFragmentRoot? GetFragmentRoot() => this;

    public IRawElementProviderFragment? ElementProviderFromPoint(double x, double y) =>
        Ui(() => (IRawElementProviderFragment)Bridge.Wrap(Bridge.Tree.HitTest(Bridge.ToClient(x, y))));

    public IRawElementProviderFragment? GetFocus() =>
        Ui(() => Bridge.Tree.Focused is { } focused ? (IRawElementProviderFragment)Bridge.Wrap(focused) : null);
}
