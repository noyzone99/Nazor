using System.Globalization;
using Nazor.Style;
using Nazor.DevTools.Protocol;
using Nazor.Controls;

namespace Nazor.DevTools;

public partial class InspectorShell
{
    [Parameter] public InspectionStore Store { get; set; } = null!;
    [Parameter] public InspectorClient? Client { get; set; }
    [Parameter] public ColorScheme Scheme { get; set; } = ColorScheme.System;
    [Parameter] public EventCallback<ColorScheme> SchemeChanged { get; set; }
    /// <summary>Whether choosing a node in the inspector also highlights it in the app.</summary>
    [Parameter] public bool FollowSelection { get; set; } = true;
    [Parameter] public EventCallback<bool> FollowSelectionChanged { get; set; }
    private enum DetailView { None, Styles, Computed, Frames }
    private const int ComponentsPanel = 0, ElementsPanel = 1, PerformancePanel = 2;
    private const int StylesTab = 0, ComputedTab = 1;
    private const int MaxCrumbs = 6;
    private InspectionStore? observed;
    private InspectionState data = new(null, InspectorConnectionStatus.Disconnected, true, null, null,
        new Dictionary<string, StyleDetails>(), new Dictionary<string, GeometryDetails>(), null, null);
    private IReadOnlyList<LocalDevToolsDescriptor> targets = [];
    private Dictionary<string, ComponentInfo> components = [];
    private Dictionary<string, ElementInfo> elements = [];
    private readonly Dictionary<string, object?> statusAttributes = new() { ["live"] = "polite" };
    private string? selectedTarget;
    private bool initialized;
    private int panel;
    private int elementTab;
    private double sidebarWidth = 380;
    private bool detailBusy;
    private bool detailsPending;
    private bool follow = true;
    private bool? lastFollow;
    private bool connectionOpen;
    private bool settingsOpen;
    private Button? connectionAnchor;
    private Button? settingsAnchor;
    private Button? pickButton;
    private SearchField<string>? filterBox;
    private int highlightVersion;
    private DetailView ActiveDetail => panel switch
    {
        PerformancePanel => DetailView.Frames,
        ElementsPanel => elementTab switch { StylesTab => DetailView.Styles, ComputedTab => DetailView.Computed, _ => DetailView.None },
        _ => DetailView.None
    };
    private ComponentInfo? SelectedComponent => data.Selection is { Kind: "component" } selected ? components.GetValueOrDefault(selected.NodeId) : null;
    private ElementInfo? SelectedElement => data.Selection is { Kind: "element" } selected ? elements.GetValueOrDefault(selected.NodeId) : null;
    private string? SelectedElementId => SelectedElement is { Kind: "element" } element ? element.Id : null;
    private StyleDetails? SelectedStyles => SelectedElementId is { } id ? data.Styles.GetValueOrDefault(id) : null;
    private GeometryDetails? SelectedGeometry => SelectedElementId is { } id ? data.Geometry.GetValueOrDefault(id) : null;
    private bool CanRefreshPanel => Client is not null && data.Target?.State == "running" &&
        data.Connection is InspectorConnectionStatus.Connected or InspectorConnectionStatus.Unresponsive &&
        (ActiveDetail == DetailView.Frames ? Client.Supports(DevToolsCapabilities.Frames) : ActiveDetail != DetailView.None && SelectedElementId is not null && !data.IsRetained &&
            (ActiveDetail == DetailView.Styles ? Client.Supports(DevToolsCapabilities.Styles) : Client.Supports(DevToolsCapabilities.Styles) || Client.Supports(DevToolsCapabilities.Geometry)));
    private TreeNavigation navigation = new(null);
    private TreeView<string>? componentTree;
    private TreeView<string>? elementTree;
    private string search = "";
    private bool showFramework;
    private bool revealPending;
    private bool expandPending;
    private TreeView<string>? ActiveTree => panel == ComponentsPanel ? componentTree : panel == ElementsPanel ? elementTree : null;
    private bool CanUseSelection => Client?.SupportsSelection == true && !data.IsRetained && data.Connection == InspectorConnectionStatus.Connected && data.Target?.State == "running";
    private bool CanUseInspection => Client?.SupportsInspection == true && CanUseSelection;
    private bool CanReconnect => Client is not null && data.Connection == InspectorConnectionStatus.Disconnected && data.Target is not null && data.Target.State != "exited";
    private bool SelectionHidden => data.Selection is { } selection && !navigation.Contains(selection.Kind, selection.NodeId);
    private string? notice;
    private bool busy;
    private bool disposed;
    private readonly CancellationTokenSource lifetime = new();
    private Timer? clock;

    protected override void OnParametersSet()
    {
        if (observed != Store)
        {
            if (observed is not null) { observed.Changed -= StoreChanged; observed.FramesChanged -= FramesChanged; }
            observed = Store;
            observed.Changed += StoreChanged;
            observed.FramesChanged += FramesChanged;
        }
        if (FollowSelection != lastFollow) follow = FollowSelection;
        lastFollow = FollowSelection;
        ReadData();
        if (!initialized && data.Selection?.Kind == "element") panel = ElementsPanel;
        initialized = true;
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (expandPending && ActiveTree is { } expandTree) { expandPending = false; expandTree.ExpandAll(); }
        if (revealPending && ActiveTree is { } tree && data.Selection is { } selected)
        { revealPending = false; tree.Reveal(selected.NodeId); }
        if (detailsPending && !detailBusy) { detailsPending = false; await ReadPanelAsync(); }
        if (!firstRender) return;
        clock = new Timer(_ => _ = InvokeAsync(() => { if (!disposed) StateHasChanged(); }), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        if (Client is not null) await DiscoverAsync();
    }
    private void StoreChanged() => _ = InvokeAsync(() =>
    {
        if (disposed) return;
        var before = data.Selection;
        var beforeContext = data.Tree?.Capture;
        ReadData();
        if (ActiveDetail != DetailView.None && (ActiveDetail != DetailView.Frames && (before?.Kind != data.Selection?.Kind || before?.NodeId != data.Selection?.NodeId) ||
            beforeContext?.SessionId != data.Tree?.Capture.SessionId || beforeContext?.Generation != data.Tree?.Capture.Generation || beforeContext?.StopId != data.Tree?.Capture.StopId))
        { Client?.CancelDetailRequests(); detailsPending = true; Client?.SetFrameUpdates(ActiveDetail == DetailView.Frames); }
        if (data.Selection is { } selected && (before?.Kind != selected.Kind || before.NodeId != selected.NodeId))
        {
            if (panel != PerformancePanel) panel = selected.Kind == "component" ? ComponentsPanel : ElementsPanel;
            if (!navigation.Contains(selected.Kind, selected.NodeId)) { search = ""; if (selected.Kind == "component") showFramework = true; RebuildNavigation(); }
            revealPending = true;
        }
        StateHasChanged();
    });
    private void FramesChanged() => _ = InvokeAsync(() =>
    {
        if (disposed || ActiveDetail != DetailView.Frames) return;
        data = data with { Frames = Store.ReadFrames() };
        StateHasChanged();
    });
    private void ChangePanel(int index)
    {
        panel = index;
        if (data.Selection is { } selected && index != PerformancePanel)
        {
            if (index == ComponentsPanel && selected.Kind == "element" && elements.GetValueOrDefault(selected.NodeId)?.ProducingComponentId is { } owner) Navigate("component", owner);
            else if (index == ElementsPanel && selected.Kind == "component" && components.GetValueOrDefault(selected.NodeId)?.OutputElements is { Length: 1 } outputs) Navigate("element", outputs[0]);
            else revealPending = true;
        }
        DetailViewChanged();
    }
    private void ChangeElementTab(int index) { elementTab = index; DetailViewChanged(); }
    private void DetailViewChanged()
    {
        Client?.CancelDetailRequests();
        Client?.SetFrameUpdates(ActiveDetail == DetailView.Frames);
        detailsPending = ActiveDetail != DetailView.None;
    }
    private void ResizeSidebar(double width) => sidebarWidth = width;
    private async Task ReadPanelAsync()
    {
        if (Client is null || !CanRefreshPanel || detailBusy) return;
        detailBusy = true;
        try
        {
            if (ActiveDetail == DetailView.Frames) await Client.RefreshFramesAsync(lifetime.Token);
            else if (SelectedElementId is { } id) await Client.CaptureDetailsAsync(id, Client.Supports(DevToolsCapabilities.Styles), ActiveDetail == DetailView.Computed && Client.Supports(DevToolsCapabilities.Geometry), lifetime.Token);
        }
        finally { detailBusy = false; if (!disposed) { ReadData(); StateHasChanged(); } }
    }
    private void ReadData()
    {
        data = Store.Read();
        components = data.Tree?.Components.ToDictionary(c => c.Id) ?? [];
        elements = data.Tree?.Elements.ToDictionary(e => e.Id) ?? [];
        RebuildNavigation();
    }
    private void RebuildNavigation() => navigation = new(data.Tree, search, showFramework);
    private IReadOnlyList<string> ComponentChildren(string id) => navigation.ComponentChildren(id);
    private IReadOnlyList<string> ElementChildren(string id) => navigation.ElementChildren(id);
    private string ComponentText(string id) => navigation.ComponentLabel(id);
    private string ElementText(string id) => navigation.ElementLabel(id);
    private string Crumb(string kind, string id) => kind == "component"
        ? components.TryGetValue(id, out var component) ? NodeSyntax.ShortName(component.TypeName) : id
        : elements.TryGetValue(id, out var element) ? NodeSyntax.Selector(element) : id;
    private void SelectComponent(string? id) { Store.Select("component", id); FollowInApp(); }
    private void SelectElement(string? id) { Store.Select("element", id); FollowInApp(); }
    private void SearchChanged(string? value) { search = value ?? ""; RebuildNavigation(); expandPending = !string.IsNullOrWhiteSpace(search); }
    private void FrameworkChanged(bool value) { showFramework = value; RebuildNavigation(); }
    private void NavigateTo(NodeLink link) => Navigate(link.Kind, link.Id);
    private void Navigate(string kind, string id)
    {
        search = "";
        if (kind == "component" && components.GetValueOrDefault(id)?.IsFramework == true) showFramework = true;
        panel = kind == "component" ? ComponentsPanel : ElementsPanel;
        RebuildNavigation();
        Store.Select(kind, id);
        revealPending = true;
        FollowInApp();
    }
    private void RevealSelection()
    {
        if (data.Selection is { } selected) Navigate(selected.Kind, selected.NodeId);
    }

    // Keyboard navigation can move through many rows quickly: only the row that stays selected briefly reaches the app.
    private void FollowInApp()
    {
        if (!follow || !CanUseSelection) return;
        var version = ++highlightVersion;
        _ = InvokeAsync(async () =>
        {
            await Task.Delay(120);
            if (disposed || version != highlightVersion || !follow || !CanUseSelection) return;
            try { await Client!.HighlightSelectionAsync(lifetime.Token); }
            catch (Exception error) when (error is OperationCanceledException or ObjectDisposedException) { }
        });
    }
    private async Task FollowChanged(bool value)
    {
        follow = value;
        await FollowSelectionChanged.InvokeAsync(value);
        if (value) FollowInApp();
        else if (CanUseSelection) await HideHighlightAsync();
    }
    public Task TogglePickAsync() => CanUseInspection && !busy ? PickAsync() : Task.CompletedTask;
    public Task RefreshCaptureAsync() => Client is null || data.Connection is InspectorConnectionStatus.Disconnected or InspectorConnectionStatus.Connecting ? Task.CompletedTask : RefreshAsync();
    public Task FocusFilterAsync() => filterBox?.FocusAsync() ?? Task.CompletedTask;
    private Task PickAsync() => RunAsync(() => Client!.SetInspectModeAsync(!data.IsInspecting, lifetime.Token));
    private Task HighlightAsync() => RunAsync(() => Client!.HighlightSelectionAsync(lifetime.Token));
    private Task HideHighlightAsync() => RunAsync(() => Client!.ClearHighlightAsync(lifetime.Token));
    private void ToggleConnection() { connectionOpen = !connectionOpen; settingsOpen = false; }
    private void ToggleSettings() { settingsOpen = !settingsOpen; connectionOpen = false; }
    private string TargetText(string session) => targets.FirstOrDefault(t => t.Identity.SessionId == session) is { } target
        ? (target.ApplicationName ?? "Process " + target.Identity.ProcessId) + " · window " + Short(target.Identity.WindowId, 8) : session;
    private string TargetName => data.Target is null ? "Not connected" : data.ApplicationName ?? "Process " + data.Target.Identity.ProcessId;
    private string TargetLabel => data.Target is { } target ? (data.ApplicationName is { } name ? name + " · " : "") + "Process " + target.Identity.ProcessId + " · app " + Short(target.Identity.ApplicationId, 8) + " · window " + Short(target.Identity.WindowId, 8) : "No target selected";
    private static string Short(string value, int length) => value.Length <= length ? value : value[..length] + "…";
    private string Status => data.Debugger?.State == "paused" ? "Paused" : data.Connection switch
    {
        InspectorConnectionStatus.Connecting => "Connecting…",
        InspectorConnectionStatus.Disconnected => data.Target?.State == "exited" ? "Target exited" : "Disconnected",
        InspectorConnectionStatus.Unresponsive => "UI unavailable",
        _ => data.Target?.State switch { "paused" => "Paused", "exited" => "Target exited", _ => data.NeedsTree ? "Connected · awaiting capture" : "Connected" }
    };
    private string StatusTone => data.Debugger?.State == "paused" || data.Target?.State == "paused" ? "paused" : data.Connection switch
    {
        InspectorConnectionStatus.Connected => "live",
        InspectorConnectionStatus.Connecting => "busy",
        InspectorConnectionStatus.Unresponsive => "warning",
        _ => "off"
    };
    private string CaptureLabel
    {
        get
        {
            if (data.Tree is null) return "No capture available";
            var age = DateTimeOffset.UtcNow - DateTimeOffset.Parse(data.Tree.Capture.CapturedAtUtc, CultureInfo.InvariantCulture);
            return (data.IsRetained ? "Retained capture" : data.Tree.Capture.Source == "debugger" ? "Debugger capture" : "Live capture") + " · " + DetailFormatting.Age(age);
        }
    }
    private string? CaptureDetails => data.Tree is not { } tree ? null : DetailFormatting.CapturedAt(tree.Capture) + " · " + tree.Capture.Source +
        " · generation " + tree.Capture.Generation + " · revision " + tree.Revision + (tree.Capture.StopId is { } stop ? " · debugger stop " + stop : "");
    private (string Tone, string Text)? BannerText
    {
        get
        {
            if ((notice ?? data.Message ?? data.DetailMessage) is { } message) return ("warning", message);
            if (data.IsInspecting) return ("info", "Click a control in your app to inspect it. Press Esc in the app to cancel.");
            if (data.Debugger?.State == "paused" || data.Target?.State == "paused")
                return ("paused", "Paused in the debugger. You are browsing the retained capture; overlays and picking resume after you continue.");
            if (data.Tree is not null && data.Connection == InspectorConnectionStatus.Disconnected) return ("info", "Disconnected. You are browsing the last capture.");
            return null;
        }
    }
    private Task DiscoverAsync() => RunAsync(async () =>
    {
        targets = await Client!.DiscoverAsync(lifetime.Token);
        if (selectedTarget is null || !targets.Any(t => t.Identity.SessionId == selectedTarget)) selectedTarget = targets.FirstOrDefault()?.Identity.SessionId;
    });
    private Task ConnectAsync() => RunAsync(async () =>
    {
        var descriptor = targets.FirstOrDefault(t => t.Identity.SessionId == selectedTarget);
        connectionOpen = false;
        if (descriptor is not null) await Client!.ConnectAsync(descriptor, lifetime.Token);
    });
    private Task ReconnectAsync() => RunAsync(async () =>
    {
        var session = data.Target?.Identity.SessionId;
        targets = await Client!.DiscoverAsync(lifetime.Token);
        if (targets.FirstOrDefault(t => t.Identity.SessionId == session) is { } descriptor)
        {
            selectedTarget = session;
            await Client.ConnectAsync(descriptor, lifetime.Token);
        }
        else notice = "That app window is no longer available. Choose another window from the connection menu.";
    });
    private Task RefreshAsync() => RunAsync(() => Client!.RefreshAsync(lifetime.Token));
    private async Task RunAsync(Func<Task> action)
    {
        if (Client is null || busy) return;
        busy = true;
        notice = null;
        try { await action(); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OperationCanceledException or DevToolsException)
        { if (!disposed) notice = "The requested connection data is unavailable. Discover or reconnect to try again."; }
        finally { busy = false; if (!disposed) { ReadData(); StateHasChanged(); } }
    }
    private void Disconnect() { Client?.Disconnect(); busy = false; connectionOpen = false; }
    private void Clear() { if (Client is null) Store.Clear(); else Client.Clear(); notice = null; connectionOpen = false; }
    public void Dispose()
    {
        disposed = true;
        if (observed is not null) { observed.Changed -= StoreChanged; observed.FramesChanged -= FramesChanged; }
        clock?.Dispose();
        Client?.SetFrameUpdates(false);
        Client?.CancelDetailRequests();
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
