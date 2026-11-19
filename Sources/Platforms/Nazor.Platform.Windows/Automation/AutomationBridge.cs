using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Nazor.Accessibility;
using Nazor.Graphics;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows.Automation;

internal sealed class AutomationBridge : IDisposable
{
    private const uint CallTimeoutMilliseconds = 5000;
    private static readonly ConcurrentDictionary<int, UiCall> Calls = new();
    private static int lastCallId;

    private readonly Win32Window window;
    private readonly int uiThread = Environment.CurrentManagedThreadId;
    private readonly Dictionary<SemanticNode, AutomationNode> providers = [];
    private readonly Queue<Action> pending = new();
    private bool flushPosted;
    private bool disposed;

    public AutomationBridge(Win32Window window, SemanticTree tree)
    {
        this.window = window;
        Tree = tree;
        Root = new RootAutomationNode(this, tree.Root);
        providers.Add(tree.Root, Root);
        tree.FocusChanged += OnFocusChanged;
        tree.PropertyChanged += OnPropertyChanged;
        tree.ChildrenChanged += OnChildrenChanged;
        tree.NodeRemoved += OnNodeRemoved;
        tree.Invoked += OnInvoked;
        tree.LiveRegionChanged += OnLiveRegionChanged;
        tree.Announced += OnAnnounced;
    }

    public SemanticTree Tree { get; }

    public RootAutomationNode Root { get; }

    public nint Handle => window.Handle;

    public T OnUiThread<T>(Func<T> call)
    {
        if (Environment.CurrentManagedThreadId == uiThread)
        {
            ThrowIfDisposed();
            return call();
        }
        var work = new UiCall<T>(() =>
        {
            ThrowIfDisposed();
            return call();
        });
        var id = Interlocked.Increment(ref lastCallId);
        Calls[id] = work;
        var hwnd = window.Handle;
        var sent = hwnd != 0 && SendMessageTimeoutW(hwnd, Win32Window.WM_NAZOR_AUTOMATION_CALL, 0, id, SMTO_NORMAL | SMTO_ERRORONEXIT, CallTimeoutMilliseconds, out _) != 0;
        if (!sent)
        {

            Calls.TryRemove(id, out _);
            if (hwnd == 0 || window.IsClosed) throw ElementNotAvailable();
        }
        if (!work.IsCompleted) throw sent ? ElementNotAvailable() : new TimeoutException("The UI thread did not answer the UI Automation call.");
        return work.GetResult();
    }

    public void OnUiThread(Action call) => OnUiThread(() =>
    {
        call();
        return true;
    });

    public static void RunCall(int id)
    {
        if (Calls.TryRemove(id, out var work)) work.Run();
    }

    public AutomationNode Wrap(SemanticNode node)
    {
        if (!providers.TryGetValue(node, out var provider)) providers.Add(node, provider = node.Role is SemanticRole.Dialog or SemanticRole.AlertDialog ? new DialogAutomationNode(this,node) : new AutomationNode(this, node));
        return provider;
    }

    public UiaRect ToScreen(Rect bounds)
    {
        var scale = window.Scale;
        var origin = new POINT { X = (int)MathF.Round(bounds.X * scale), Y = (int)MathF.Round(bounds.Y * scale) };
        ClientToScreen(window.Handle, ref origin);
        return new UiaRect { Left = origin.X, Top = origin.Y, Width = MathF.Round(bounds.Width * scale), Height = MathF.Round(bounds.Height * scale) };
    }

    public Point ToClient(double x, double y)
    {
        var point = new POINT { X = (int)Math.Floor(x), Y = (int)Math.Floor(y) };
        ScreenToClient(window.Handle, ref point);
        return new Point(point.X / window.Scale, point.Y / window.Scale);
    }

    public void FlushEvents()
    {
        flushPosted = false;
        while (pending.TryDequeue(out var raise)) raise();
    }

    public void FocusWindow()
    {
        var hwnd = window.Handle;
        if (hwnd != 0 && GetForegroundWindow() == hwnd && GetFocus() != hwnd) SetFocus(hwnd);
    }

    public static COMException ElementNotAvailable() => new("The element is no longer available.", Uia.E_ELEMENTNOTAVAILABLE);

    private void ThrowIfDisposed()
    {
        if (disposed) throw ElementNotAvailable();
    }

    private void Enqueue(Action raise)
    {
        pending.Enqueue(raise);
        if (flushPosted) return;
        flushPosted = true;
        PostMessageW(window.Handle, Win32Window.WM_NAZOR_AUTOMATION, 0, 0);
    }

    private void Raise(SemanticNode node, Action<AutomationNode> raise)
    {
        if (!Uia.UiaClientsAreListening()) return;
        Enqueue(() =>
        {
            if (!node.IsDetached && Uia.UiaClientsAreListening()) raise(Wrap(node));
        });
    }

    private void OnFocusChanged(SemanticNode? node)
    {
        if (node is not null) Raise(node, provider => Uia.UiaRaiseAutomationEvent(provider, Uia.AutomationFocusChangedEvent));
    }

    internal static int PropertyOf(SemanticRole role, SemanticProperty property) => property switch
    {
        SemanticProperty.Name => Uia.NameProperty,
        SemanticProperty.Description => 30159,
        SemanticProperty.IsRequired => 30025,
        SemanticProperty.IsInvalid => 30026,
        SemanticProperty.IsEnabled => Uia.IsEnabledProperty,
        SemanticProperty.IsFocusable => Uia.IsKeyboardFocusableProperty,
        SemanticProperty.IsChecked when IsSelectionItem(role) => Uia.SelectionItemIsSelectedProperty,
        SemanticProperty.IsChecked => Uia.ToggleStateProperty,
        SemanticProperty.Value => Uia.ValueValueProperty,
        SemanticProperty.IsOffscreen => Uia.IsOffscreenProperty,
        SemanticProperty.RangeValue => Uia.RangeValueValueProperty,
        SemanticProperty.IsExpanded => Uia.ExpandCollapseStateProperty,
        SemanticProperty.ScrollAvailable => Uia.IsScrollPatternAvailableProperty,
        SemanticProperty.HorizontalScrollPercent => Uia.HorizontalScrollPercentProperty,
        SemanticProperty.VerticalScrollPercent => Uia.VerticalScrollPercentProperty,
        SemanticProperty.HorizontalViewSize => Uia.HorizontalViewSizeProperty,
        SemanticProperty.VerticalViewSize => Uia.VerticalViewSizeProperty,
        SemanticProperty.HorizontallyScrollable => Uia.HorizontallyScrollableProperty,
        SemanticProperty.VerticallyScrollable => Uia.VerticallyScrollableProperty,
        _ => 0,
    };

    private void OnTextChanged(SemanticNode node, SemanticProperty property)
    {
        if (node.TextSelection is null) return;
        if (property == SemanticProperty.TextSelection) Raise(node, provider => Uia.UiaRaiseAutomationEvent(provider, Uia.TextSelectionChangedEvent));
        else if (property == SemanticProperty.Value) Raise(node, provider => Uia.UiaRaiseAutomationEvent(provider, Uia.TextChangedEvent));
    }

    private static bool IsSelectionItem(SemanticRole role) =>
        role is SemanticRole.RadioButton or SemanticRole.ListItem or SemanticRole.TreeItem or SemanticRole.TabItem;

    private void OnPropertyChanged(SemanticNode node, SemanticProperty property, object? oldValue, object? newValue)
    {
        OnTextChanged(node, property);
        var propertyId = PropertyOf(node.Role, property);
        if (propertyId == 0) return;

        if (property == SemanticProperty.ScrollAvailable && node.Role == SemanticRole.Group)
            Raise(node, provider => Uia.UiaRaiseAutomationPropertyChangedEvent(provider, Uia.ControlTypeProperty,
                Variant.FromObject(oldValue is true ? Uia.PaneControlType : Uia.GroupControlType),
                Variant.FromObject(newValue is true ? Uia.PaneControlType : Uia.GroupControlType)));

        if (propertyId == Uia.SelectionItemIsSelectedProperty)
        {
            var several = Tree.SelectionContainer(node) is { CanSelectMultiple: true } container && Tree.SelectedItems(container).Count > (newValue is true ? 1 : 0);
            var eventId = newValue is true
                ? several ? Uia.SelectionItemElementAddedToSelectionEvent : Uia.SelectionItemElementSelectedEvent
                : several ? Uia.SelectionItemElementRemovedFromSelectionEvent : 0;
            if (eventId != 0) Raise(node, provider => Uia.UiaRaiseAutomationEvent(provider, eventId));
        }

        else if (property == SemanticProperty.IsInvalid)
        {
            oldValue = oldValue is not true;
            newValue = newValue is not true;
        }

        else if (property == SemanticProperty.IsExpanded)
        {
            oldValue = oldValue is true ? (int)ExpandCollapseState.Expanded : (int)ExpandCollapseState.Collapsed;
            newValue = newValue is true ? (int)ExpandCollapseState.Expanded : (int)ExpandCollapseState.Collapsed;
        }

        else if (property == SemanticProperty.IsChecked)
        {
            oldValue = oldValue is null ? (int)ToggleState.Indeterminate : oldValue is true ? (int)ToggleState.On : (int)ToggleState.Off;
            newValue = newValue is null ? (int)ToggleState.Indeterminate : newValue is true ? (int)ToggleState.On : (int)ToggleState.Off;
        }
        Raise(node, provider =>
        {
            var before = Variant.FromObject(oldValue);
            var after = Variant.FromObject(newValue);
            try
            {
                Uia.UiaRaiseAutomationPropertyChangedEvent(provider, propertyId, before, after);
            }
            finally
            {
                before.Clear();
                after.Clear();
            }
        });
    }

    private unsafe void OnChildrenChanged(SemanticNode node) =>
        Raise(node, provider => Uia.UiaRaiseStructureChangedEvent(provider, StructureChangeType.ChildrenInvalidated, null, 0));

    private void OnNodeRemoved(SemanticNode node)
    {

        if (providers.Remove(node, out var provider)) Enqueue(() => Uia.UiaDisconnectProvider(provider));
    }

    private void OnInvoked(SemanticNode node) => Raise(node, provider => Uia.UiaRaiseAutomationEvent(provider, Uia.InvokeInvokedEvent));

    private void OnLiveRegionChanged(SemanticNode node) => Raise(node, provider => Uia.UiaRaiseAutomationEvent(provider, Uia.LiveRegionChangedEvent));

    private void OnAnnounced(SemanticNode source, string message, AnnouncementPriority priority) => Raise(source, provider =>
    {
        try
        {
            Uia.UiaRaiseNotificationEvent(provider, Uia.NotificationKindOther,
                priority == AnnouncementPriority.Assertive ? Uia.NotificationProcessingImportantAll : Uia.NotificationProcessingAll, message, "Nazor.Announcement");
        }
        catch (EntryPointNotFoundException)
        {

        }
    });

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Tree.FocusChanged -= OnFocusChanged;
        Tree.PropertyChanged -= OnPropertyChanged;
        Tree.ChildrenChanged -= OnChildrenChanged;
        Tree.NodeRemoved -= OnNodeRemoved;
        Tree.Invoked -= OnInvoked;
        Tree.LiveRegionChanged -= OnLiveRegionChanged;
        Tree.Announced -= OnAnnounced;
        pending.Clear();
        foreach (var provider in providers.Values) Uia.UiaDisconnectProvider(provider);
        providers.Clear();
    }

    private abstract class UiCall
    {
        public abstract void Run();
    }

    private sealed class UiCall<T>(Func<T> call) : UiCall
    {
        private T? result;
        private ExceptionDispatchInfo? error;
        private volatile bool completed;

        public bool IsCompleted => completed;

        public override void Run()
        {
            try
            {
                result = call();
            }
            catch (Exception exception)
            {
                error = ExceptionDispatchInfo.Capture(exception);
            }
            completed = true;
        }

        public T GetResult()
        {
            error?.Throw();
            return result!;
        }
    }
}
