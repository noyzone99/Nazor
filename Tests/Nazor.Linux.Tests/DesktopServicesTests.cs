using Nazor.Accessibility;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Platform;
using Nazor.Platform.Linux;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Tmds.DBus.Protocol;

namespace Nazor.Linux.Tests;

[Collection("Linux desktop")]
public sealed class DesktopServicesTests
{
    public static bool HasSession => WaylandTests.HasCompositor && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS"));

    [Fact]
    public void Desktop_bindings_reference_only_the_AOT_compatible_protocol_library()
    {
        var references = typeof(DesktopConnection).Assembly.GetReferencedAssemblies();
        Assert.Contains(references, reference => reference.Name == "Tmds.DBus.Protocol");
        Assert.DoesNotContain(references, reference => reference.Name == "Tmds.DBus");
    }

    [Fact(SkipUnless=nameof(HasSession),Skip="Requires isolated Wayland and D-Bus test services.")]
    public async Task Tray_menu_notifications_and_preferences_use_native_desktop_protocols()
    {
        using var services=await Task.Run(MockDesktop.CreateAsync);
        using var platform=new WaylandPlatform();
        var desktop=platform.Desktop;
        Assert.True(desktop.SupportsTray); Assert.True(desktop.SupportsNotifications);
        Assert.True(desktop.Dark); Assert.True(desktop.ReducedMotion);
        using var tray = (LinuxTrayItem)desktop.CreateTrayItem();
        using var second = (LinuxTrayItem)desktop.CreateTrayItem();
        var activations=0; var secondary=0; var commands=new List<int>();
        tray.Activated+=()=>activations++;
        tray.SecondaryActivated+=()=>secondary++;
        tray.MenuItemInvoked+=commands.Add;
        Assert.True(tray.Show(new() { ToolTip="Nazor test", Menu=
        [
            new(1,NativeTrayMenuItemKind.Command,"Open"),
            new(2,NativeTrayMenuItemKind.Separator),
            new(3,NativeTrayMenuItemKind.Command,"Disabled") { Enabled=false },
            new(4,NativeTrayMenuItemKind.Submenu,"Settings") { Children=
            [
                new(5,NativeTrayMenuItemKind.CheckBox,"Pause") { IsChecked=true, Icon=new RenderedImage(1,1,[0,0,255,255]) },
                new(6,NativeTrayMenuItemKind.Radio,"Work") { IsChecked=true },
                new(7,NativeTrayMenuItemKind.Radio,"Home"),
                new(8,NativeTrayMenuItemKind.Command,"Hidden") { Visible=false }
            ] }
        ] }));
        Assert.True(tray.IsVisible);
        Assert.Equal(tray.ObjectPath.ToString(),services.Watcher.RegisteredPath);
        Assert.True(second.Show(new() { ToolTip="Second" }));
        Assert.NotEqual(tray.BusName,second.BusName);
        var item=services.Bus.CreateProxy<IStatusNotifierItem>(tray.BusName,tray.ObjectPath);
        var menu=services.Bus.CreateProxy<ITrayMenu>(tray.BusName,tray.MenuPath);
        Assert.Equal("Nazor test",platform.Wait(item.GetAsync("Title")));
        var layout=platform.Wait(menu.GetLayoutAsync(0,-1,[]));
        Assert.Equal(4,layout.Layout.Children.Length);
        var nested=platform.Wait(menu.GetLayoutAsync(4,1,[]));
        Assert.Equal(4,nested.Layout.Children.Length);
        Assert.Equal("checkmark",platform.Wait(menu.GetPropertyAsync(5,"toggle-type")));
        var iconData=(byte[])platform.Wait(menu.GetPropertyAsync(5,"icon-data"));
        Assert.Equal(new byte[] { 137,80,78,71,13,10,26,10 },iconData.Take(8));
        Assert.Equal(1,platform.Wait(menu.GetPropertyAsync(6,"toggle-state")));
        Assert.Equal("radio",platform.Wait(menu.GetPropertyAsync(6,"toggle-type")));
        Assert.False((bool)platform.Wait(menu.GetPropertyAsync(8,"visible")));
        var openings=0;
        tray.MenuOpening+=args=> { openings++; Assert.False(args.CanCancel); Assert.False(args.TryCancel()); };
        Assert.False(platform.Wait(menu.AboutToShowAsync(0)));
        Assert.Equal(1,openings);
        platform.Wait(Complete(item.ActivateAsync(0,0)));
        platform.Wait(Complete(item.SecondaryActivateAsync(0,0)));
        platform.Wait(Complete(menu.EventAsync(1,"clicked",0,0)));
        platform.Wait(Complete(menu.EventAsync(3,"clicked",0,0)));
        platform.Wait(Complete(menu.EventAsync(8,"clicked",0,0)));
        Assert.Equal(1,activations); Assert.Equal(1,secondary); Assert.Equal(new[] { 1 },commands);
        Assert.True(desktop.ShowNotification(new("Test","<message>")));
        Assert.Equal("&lt;message&gt;",services.Notifications.Body);
        Assert.True(platform.Wait(services.Bus.Raw.ReleaseNameAsync("org.kde.StatusNotifierWatcher")));
        Assert.True(platform.Wait(UntilAsync(() => !tray.IsVisible && !second.IsVisible)));
        platform.Wait(Complete(services.Bus.Raw.RequestNameAsync("org.kde.StatusNotifierWatcher")));
        Assert.True(platform.Wait(UntilAsync(() => tray.IsVisible && second.IsVisible)));
        var removedName = tray.BusName;
        tray.Hide(); Assert.False(tray.IsVisible); Assert.True(second.IsVisible);
        Assert.Null(platform.Wait(services.Bus.ResolveServiceOwnerAsync(removedName)));
        Assert.NotNull(platform.Wait(services.Bus.ResolveServiceOwnerAsync(second.BusName)));
        Assert.True(platform.Wait(services.Bus.Raw.ReleaseNameAsync("org.kde.StatusNotifierWatcher")));
        Assert.True(platform.Wait(UntilAsync(() => !second.IsVisible)));
        platform.Wait(Complete(services.Bus.Raw.RequestNameAsync("org.kde.StatusNotifierWatcher")));
        Assert.True(platform.Wait(UntilAsync(() => second.IsVisible)));
        Assert.False(tray.IsVisible);
    }

    [Fact(SkipUnless=nameof(HasSession),Skip="Requires isolated Wayland and D-Bus test services.")]
    public async Task File_picker_subscribes_before_request_and_returns_local_paths()
    {
        using var services=await Task.Run(MockDesktop.CreateAsync);
        using var platform=new WaylandPlatform();
        services.Chooser.Requester=platform.Desktop.BusName;
        var paths=platform.ShowFileDialog(null,new(FileDialogKind.Open) { Multiple=true,Filters=[new("Text",["*.txt"])] });
        Assert.Equal(new[] { "/tmp/nazor-file.txt" },paths);
        Assert.True((bool)services.Chooser.Options!["multiple"]);
        Assert.True(services.Chooser.Options.ContainsKey("filters"));
    }

    [Fact(SkipUnless=nameof(HasSession),Skip="Requires isolated Wayland and D-Bus test services.")]
    public async Task Accessibility_exports_semantics_actions_unicode_and_password_protection()
    {
        using var services=await Task.Run(MockDesktop.CreateAsync);
        using var platform=new WaylandPlatform();
        var dispatcher=new UIDispatcher();
        using var renderer=new Renderer(dispatcher);
        renderer.AddRootComponent(typeof(AccessibleFixture));
        using var graphics=new SkiaRenderer();
        using var scene=new Scene(renderer,graphics.Fonts,graphics.Shaper) { Viewport=new(320,200),Styles={ Theme=StyleSheets.Default } };
        using var input=new InputManager(renderer,scene);
        using var semantics=new SemanticTree(renderer,scene,input);
        semantics.Update();
        using var native=platform.CreateWindow(new SemanticHost(semantics),new("Accessibility",320,200));
        platform.Desktop.SyncAccessibility((LinuxDesktopWindow)native,semantics);
        Assert.NotNull(services.Socket.Application);
        var reference=services.Socket.Application!.Value;
        var cache=services.Bus.CreateProxy<IAccessibleCache>(reference.Item1,"/org/a11y/atspi/cache");
        var items=platform.Wait(cache.GetItemsAsync());
        Assert.Contains(items,item=>item.Name=="Press me" && item.Role==43);
        var editor=items.Single(item=>item.Name=="Editor");
        var text=services.Bus.CreateProxy<IAccessibleText>(reference.Item1,editor.Reference.Item2);
        Assert.Equal("A😀B",platform.Wait(text.GetTextAsync(0,-1)));
        Assert.Equal(3,platform.Wait(text.GetAsync("CharacterCount")));
        Assert.Equal(0x1f600,platform.Wait(text.GetCharacterAtOffsetAsync(1)));
        var secret=items.Single(item=>item.Role==40);
        var password=services.Bus.CreateProxy<IAccessibleText>(reference.Item1,secret.Reference.Item2);
        Assert.Equal("",platform.Wait(password.GetTextAsync(0,-1)));
        var button=items.Single(item=>item.Name=="Press me");
        var action=services.Bus.CreateProxy<IAccessibleAction>(reference.Item1,button.Reference.Item2);
        Assert.True(platform.Wait(action.DoActionAsync(0)));
        var scroller=items.Single(item=>item.Role==49);
        var scrollAction=services.Bus.CreateProxy<IAccessibleAction>(reference.Item1,scroller.Reference.Item2);
        Assert.Equal(8,platform.Wait(scrollAction.GetAsync("NActions")));
        var changed=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var scrollEvents=services.Bus.CreateProxy<IAccessibleEvents>(reference.Item1,scroller.Reference.Item2);
        using var subscription=platform.Wait(scrollEvents.WatchVisibleDataChangedAsync(_=>changed.TrySetResult(true)));
        Assert.True(platform.Wait(scrollAction.DoActionAsync(1)));
        Assert.True(semantics.Descendants().Single(node=>node.Scroll is not null).Scroll!.OffsetY>0);
        semantics.Update();
        Assert.True(platform.Wait(changed.Task.WaitAsync(TimeSpan.FromSeconds(5),TestContext.Current.CancellationToken)));
        var tableItem=items.Single(item=>item.Name=="Inventory");
        var table=services.Bus.CreateProxy<IAccessibleTable>(reference.Item1,tableItem.Reference.Item2);
        Assert.Equal(2,platform.Wait(table.GetAsync("NRows")));
        Assert.Equal(2,platform.Wait(table.GetAsync("NColumns")));
        var cellReference=platform.Wait(table.GetAccessibleAtAsync(1,1));
        Assert.Equal(cellReference,platform.Wait(table.GetAccessibleAtAsync(1,0)));
        var cell=services.Bus.CreateProxy<IAccessibleTableCell>(reference.Item1,cellReference.Item2);
        Assert.Equal((true,1,0,1,2),platform.Wait(cell.GetRowColumnSpanAsync()));
        Assert.Equal(new object[] { 1,0 },Assert.IsType<object[]>(platform.Wait(cell.GetAsync("Position"))));
        Assert.Equal(2,platform.Wait(cell.GetColumnHeaderCellsAsync()).Length);
        Assert.Empty(platform.Wait(cell.GetRowHeaderCellsAsync()));
        Assert.False(platform.Wait(table.AddRowSelectionAsync(1)));
        var tableAccessible=services.Bus.CreateProxy<IAccessible>(reference.Item1,tableItem.Reference.Item2);
        var cellIndex=platform.Wait(table.GetIndexAtAsync(1,1));
        Assert.Equal(cellReference,platform.Wait(tableAccessible.GetChildAtIndexAsync(cellIndex)));
        Assert.Contains("org.a11y.atspi.Table",platform.Wait(tableAccessible.GetInterfacesAsync()));
        dispatcher.RunPending();
    }

    [Fact]
    public void Unicode_offsets_follow_AT_SPI_codepoints_instead_of_UTF16_units()
    {
        Assert.Equal(3,AccessibilityBridge.Count("A😀B"));
        Assert.Equal(3,AccessibilityBridge.ToUtf16("A😀B",2));
        Assert.Equal(2,AccessibilityBridge.ToOffset("A😀B",3));
    }

    [Fact(SkipUnless=nameof(HasSession),Skip="Requires isolated Wayland and D-Bus test services.")]
    public async Task Accessibility_exports_component_splitters_and_menu_check_actions()
    {
        using var services=await Task.Run(MockDesktop.CreateAsync); using var platform=new WaylandPlatform();
        var dispatcher=new UIDispatcher(); using var renderer=new Renderer(dispatcher);
        renderer.AddRootComponent(typeof(ComposedPrimitivesFixture)); using var graphics=new SkiaRenderer();
        using var scene=new Scene(renderer,graphics.Fonts,graphics.Shaper) { Viewport=new(320,200),Styles={ Theme=StyleSheets.Default } };
        using var input=new InputManager(renderer,scene); using var semantics=new SemanticTree(renderer,scene,input); semantics.Update();
        using var native=platform.CreateWindow(new SemanticHost(semantics),new("Primitives",320,200));
        platform.Desktop.SyncAccessibility((LinuxDesktopWindow)native,semantics);
        var reference=services.Socket.Application!.Value;
        var cache=services.Bus.CreateProxy<IAccessibleCache>(reference.Item1,"/org/a11y/atspi/cache");
        var items=platform.Wait(cache.GetItemsAsync());
        var splitter=items.Single(item=>item.Name=="Pane width"); Assert.Equal(50u,splitter.Role);
        var range=services.Bus.CreateProxy<IAccessibleValue>(reference.Item1,splitter.Reference.Item2);
        Assert.Equal(200.0,platform.Wait(range.GetAsync("CurrentValue")));
        async Task<bool> WriteRange() { await range.SetAsync("CurrentValue",250.0).ConfigureAwait(false); return true; }
        Assert.True(platform.Wait(WriteRange())); dispatcher.RunPending(); semantics.Update();
        Assert.Equal(250.0,platform.Wait(range.GetAsync("CurrentValue")));
        var choice=items.Single(item=>item.Name=="Choice"); Assert.Equal(8u,choice.Role);
        var accessible=services.Bus.CreateProxy<IAccessible>(reference.Item1,choice.Reference.Item2);
        Assert.Equal(0u,platform.Wait(accessible.GetStateAsync())[0]&(1u<<4));
        var action=services.Bus.CreateProxy<IAccessibleAction>(reference.Item1,choice.Reference.Item2);
        Assert.True(platform.Wait(action.DoActionAsync(0))); dispatcher.RunPending(); semantics.Update();
        var states=platform.Wait(accessible.GetStateAsync()); Assert.Equal(0u,states[1]&1u); Assert.NotEqual(0u,states[0]&(1u<<4));
    }

    public sealed class ComposedPrimitivesFixture : ComponentBase
    {
        private double width = 200;

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<ResizeHandle>(0);
            b.AddComponentParameter(1, nameof(ResizeHandle.AccessibleName), "Pane width");
            b.AddComponentParameter(2, nameof(ResizeHandle.Width), width);
            b.AddComponentParameter(3, nameof(ResizeHandle.MaxSize), 400d);
            b.AddComponentParameter(4, nameof(ResizeHandle.OnResize), EventCallback.Factory.Create<ResizeValue>(this, size => width = size.Width));
            b.CloseComponent();
            b.OpenComponent<Menu>(10); b.AddComponentParameter(11,nameof(Menu.IsOpen),true);
            b.AddComponentParameter(12,"ChildContent",(RenderFragment)(menu=>
            {
                menu.OpenComponent<MenuCheckItem>(0);
                menu.AddComponentParameter(2,"CloseOnSelect",false); menu.AddComponentParameter(3,"ChildContent",(RenderFragment)(text=>text.AddContent(0,"Choice"))); menu.CloseComponent();
            })); b.CloseComponent();
        }
    }

    [Fact(SkipUnless=nameof(HasSession),Skip="Requires isolated Wayland and D-Bus test services.")]
    public async Task Accessibility_exports_dialog_modality_and_blocks_background_actions()
    {
        using var services=await Task.Run(MockDesktop.CreateAsync);
        using var platform=new WaylandPlatform();
        var dispatcher=new UIDispatcher();
        using var renderer=new Renderer(dispatcher);
        renderer.AddRootComponent(typeof(DialogFixture));
        using var graphics=new SkiaRenderer();
        using var scene=new Scene(renderer,graphics.Fonts,graphics.Shaper) { Viewport=new(320,200),Styles={ Theme=StyleSheets.Default } };
        using var input=new InputManager(renderer,scene);
        using var semantics=new SemanticTree(renderer,scene,input);
        semantics.Update();
        using var native=platform.CreateWindow(new SemanticHost(semantics),new("Dialogs",320,200));
        platform.Desktop.SyncAccessibility((LinuxDesktopWindow)native,semantics);
        var reference=services.Socket.Application!.Value;
        var cache=services.Bus.CreateProxy<IAccessibleCache>(reference.Item1,"/org/a11y/atspi/cache");
        var items=platform.Wait(cache.GetItemsAsync());
        var dialog=items.Single(item=>item.Name=="Review");
        Assert.Equal(16u,dialog.Role);
        var accessible=services.Bus.CreateProxy<IAccessible>(reference.Item1,dialog.Reference.Item2);
        Assert.NotEqual(0u,platform.Wait(accessible.GetStateAsync())[0] & (1u<<16));
        var background=items.Single(item=>item.Name=="Background");
        var blocked=services.Bus.CreateProxy<IAccessibleAction>(reference.Item1,background.Reference.Item2);
        Assert.False(platform.Wait(blocked.DoActionAsync(0)));
        var close=items.Single(item=>item.Name=="Close");
        var action=services.Bus.CreateProxy<IAccessibleAction>(reference.Item1,close.Reference.Item2);
        Assert.True(platform.Wait(action.DoActionAsync(0)));
        dispatcher.RunPending(); semantics.Update();
        Assert.DoesNotContain(semantics.Descendants(),node=>node.Role==SemanticRole.Dialog);
        Assert.True(semantics.Descendants().Single(node=>node.Name=="Background").IsEnabled);
    }

    public sealed class DialogFixture : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Button>(0); builder.AddComponentParameter(1,"ChildContent",(RenderFragment)(body=>body.AddContent(0,"Background"))); builder.CloseComponent();
            builder.OpenComponent<Dialog>(2); builder.AddComponentParameter(3,"IsOpen",true); builder.AddComponentParameter(4,"AccessibleName","Review");
            builder.CloseComponent();
        }
    }
    [Fact]
    public void Save_picker_options_apply_default_extension_and_nul_terminated_directory()
    {
        var options=LinuxDesktopIntegration.FileOptions(new(FileDialogKind.Save) { FileName="report",DefaultExtension=".csv",InitialDirectory=Path.GetTempPath() },"token");
        Assert.Equal("report.csv",options["current_name"]);
        Assert.Equal(0,((byte[])options["current_folder"])[^1]);
        Assert.False((bool)options["multiple"]);
    }
    private static async Task<bool> UntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline) return false;
            await Task.Delay(10, TestContext.Current.CancellationToken).ConfigureAwait(false);
        }
        return true;
    }

    private static async Task<bool> Complete(Task task) { await task.ConfigureAwait(false); return true; }

    private sealed class MockDesktop : IDisposable
    {
        internal DesktopConnection Bus=null!;
        internal readonly MockWatcher Watcher=new();
        internal readonly MockNotifications Notifications=new();
        internal readonly MockSocket Socket=new();
        internal MockChooser Chooser=null!;
        internal static async Task<MockDesktop> CreateAsync()
        {
            var value=new MockDesktop { Bus=new DesktopConnection(DBusAddress.Session!) };
            await value.Bus.ConnectAsync();
            value.Chooser=new(value.Bus);
            await value.Bus.RegisterObjectsAsync([value.Watcher,value.Notifications,value.Chooser,new MockAccessibilityBus(),value.Socket]);
            foreach (var name in new[] { "org.kde.StatusNotifierWatcher","org.freedesktop.Notifications","org.freedesktop.portal.Desktop","org.a11y.Bus","org.a11y.atspi.Registry" }) await value.Bus.Raw.RequestNameAsync(name);
            // The test pumps Wayland synchronously; resume it outside the D-Bus reader's continuation.
            await Task.Yield();
            return value;
        }
        public void Dispose()=>Bus.Dispose();
    }
    private sealed class MockWatcher : IStatusNotifierWatcher
    {
        internal string? RegisteredPath;
        public ObjectPath ObjectPath=>new("/StatusNotifierWatcher");
        public Task RegisterStatusNotifierItemAsync(string service) { RegisteredPath=service; return Task.CompletedTask; }
        public Task<object> GetAsync(string property)=>Task.FromResult<object>(true);
    }
    private sealed class MockNotifications : INotifications
    {
        internal string? Body;
        public ObjectPath ObjectPath=>new("/org/freedesktop/Notifications");
        public Task<uint> NotifyAsync(string appName,uint replacesId,string appIcon,string summary,string body,string[] actions,IDictionary<string,object> hints,int expireTimeout) { Body=body; return Task.FromResult(42u); }
        public Task CloseNotificationAsync(uint id)=>Task.CompletedTask;
        public Task<IDisposable> WatchActionInvokedAsync(Action<(uint Id,string Action)> handler)=>Task.FromResult<IDisposable>(new BusSubscription(()=>{}));
        public Task<IDisposable> WatchNotificationClosedAsync(Action<(uint Id,uint Reason)> handler)=>Task.FromResult<IDisposable>(new BusSubscription(()=>{}));
    }
    private sealed class MockSettings : IDesktopSettings
    {
        public ObjectPath ObjectPath=>new("/org/freedesktop/portal/desktop");
        public Task<object> ReadAsync(string group,string key)=>Task.FromResult<object>(1u);
        public Task<IDictionary<string,IDictionary<string,object>>> ReadAllAsync(string[] namespaces)=>Task.FromResult<IDictionary<string,IDictionary<string,object>>>(new Dictionary<string,IDictionary<string,object>> { ["org.freedesktop.appearance"]=new Dictionary<string,object> { ["color-scheme"]=1u,["reduced-motion"]=1u } });
        public Task<IDisposable> WatchSettingChangedAsync(Action<(string Namespace,string Key,object Value)> handler)=>Task.FromResult<IDisposable>(new BusSubscription(()=>{}));
    }
    private sealed class MockChooser(DesktopConnection bus) : IFileChooser, IDesktopSettings
    {
        internal IDictionary<string,object>? Options;
        internal string Requester="";
        private readonly MockSettings settings=new();
        public ObjectPath ObjectPath=>new("/org/freedesktop/portal/desktop");
        public Task<ObjectPath> OpenFileAsync(string parentWindow,string title,IDictionary<string,object> options)=>Open(options);
        public Task<ObjectPath> SaveFileAsync(string parentWindow,string title,IDictionary<string,object> options)=>Open(options);
        private async Task<ObjectPath> Open(IDictionary<string,object> options)
        {
            Options=options;
            var path=new ObjectPath("/org/freedesktop/portal/desktop/request/"+Requester.TrimStart(':').Replace('.','_')+"/"+options["handle_token"]);
            var request=new MockRequest(path); await bus.RegisterObjectsAsync([request]);
            _=Task.Run(async ()=> { await Task.Delay(30); request.Complete(); });
            return path;
        }
        public Task<object> ReadAsync(string group,string key)=>settings.ReadAsync(group,key);
        public Task<IDictionary<string,IDictionary<string,object>>> ReadAllAsync(string[] namespaces)=>settings.ReadAllAsync(namespaces);
        public Task<IDisposable> WatchSettingChangedAsync(Action<(string Namespace,string Key,object Value)> handler)=>settings.WatchSettingChangedAsync(handler);
    }
    private sealed class MockRequest(ObjectPath path) : IPortalRequest
    {
        private Action<(uint Response,IDictionary<string,object> Results)>? response;
        public ObjectPath ObjectPath=>path;
        public Task CloseAsync()=>Task.CompletedTask;
        internal void Complete()=>response?.Invoke((0,new Dictionary<string,object> { ["uris"]=new[] { "file:///tmp/nazor-file.txt" } }));
        public Task<IDisposable> WatchResponseAsync(Action<(uint Response,IDictionary<string,object> Results)> handler) { response+=handler; return Task.FromResult<IDisposable>(new BusSubscription(()=>response-=handler)); }
    }
    private sealed class MockAccessibilityBus : IAccessibilityBus
    {
        public ObjectPath ObjectPath=>new("/org/a11y/bus");
        public Task<string> GetAddressAsync()=>Task.FromResult(DBusAddress.Session!);
    }
    private sealed class MockSocket : IAccessibilitySocket
    {
        internal (string,ObjectPath)? Application;
        public ObjectPath ObjectPath=>new("/org/a11y/atspi/accessible/root");
        public Task<(string,ObjectPath)> EmbedAsync((string,ObjectPath) application) { Application=application; return Task.FromResult(("org.a11y.atspi.Registry",ObjectPath)); }
        public Task UnembedAsync((string,ObjectPath) application)=>Task.CompletedTask;
    }
    public sealed class AccessibleFixture : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0,"Button"); builder.AddAttribute(1,"role","button"); builder.AddAttribute(2,"accessible-name","Press me"); builder.AddContent(3,"Press me"); builder.CloseElement();
            builder.OpenElement(4,"TextBox"); builder.AddAttribute(5,"role","textbox"); builder.AddAttribute(6,"accessible-name","Editor"); builder.AddAttribute(7,"value","A😀B"); builder.CloseElement();
            builder.OpenElement(8,"PasswordBox"); builder.AddAttribute(9,"role","textbox"); builder.AddAttribute(10,"password",true); builder.AddAttribute(11,"value","secret"); builder.CloseElement();
            builder.OpenElement(12,"Container");  builder.AddAttribute(14,"style","height: 40px; overflow-y: auto");
            builder.OpenElement(15,"Container"); builder.AddAttribute(16,"style","height: 400px"); builder.AddContent(17,"Scrollable content"); builder.CloseElement(); builder.CloseElement();
            builder.OpenElement(20,"Table"); builder.AddAttribute(21,"role","table"); builder.AddAttribute(22,"table-part","table"); builder.AddAttribute(23,"accessible-name","Inventory");
            builder.OpenElement(24,"TableHeader"); builder.AddAttribute(25,"role","rowgroup"); builder.AddAttribute(26,"table-part","group");
            builder.OpenElement(27,"TableRow"); builder.AddAttribute(28,"role","row"); builder.AddAttribute(29,"table-part","row");
            Cell(builder,30,"columnheader",1,"Name"); Cell(builder,40,"columnheader",1,"Count");
            builder.CloseElement(); builder.CloseElement();
            builder.OpenElement(50,"TableBody"); builder.AddAttribute(51,"role","rowgroup"); builder.AddAttribute(52,"table-part","group");
            builder.OpenElement(53,"TableRow"); builder.AddAttribute(54,"role","row"); builder.AddAttribute(55,"table-part","row");
            Cell(builder,60,"cell",2,"Combined"); builder.CloseElement(); builder.CloseElement(); builder.CloseElement();

        }
    }
    private static void Cell(RenderTreeBuilder builder,int sequence,string role,int span,string text)
    {
        builder.OpenElement(sequence,"TableCell"); builder.AddAttribute(sequence+1,"role",role); builder.AddAttribute(sequence+2,"table-part","cell");
        builder.AddAttribute(sequence+3,"column-span",span); builder.AddContent(sequence+4,text); builder.CloseElement();
    }
    private sealed class SemanticHost(SemanticTree tree) : IWindowHost
    {
        public void OnResized(Size size,float scale) { }
        public IReadOnlyList<PixelRect>? OnPaint(PaintTarget target)=>null;
        public void OnPointerMoved(Point point,KeyModifiers modifiers) { }
        public void OnPointerPressed(Point point,PointerButton button,KeyModifiers modifiers) { }
        public void OnPointerReleased(Point point,PointerButton button,KeyModifiers modifiers) { }
        public void OnPointerLeft() { }
        public void OnPointerWheel(Point point,float notches,KeyModifiers modifiers) { }
        public void OnPointerHorizontalWheel(Point point,float notches,KeyModifiers modifiers) { }
        public void OnPointerCanceled() { }
        public bool OnKeyDown(string key,KeyModifiers modifiers,bool repeat)=>false;
        public bool OnKeyUp(string key,KeyModifiers modifiers)=>false;
        public void OnTextInput(string text) { }
        public void OnComposition(string text,int cursor) { }
        public void OnCompositionCommit(string text) { }
        public void OnFocusChanged(bool focused) { }
        public void OnVisibilityChanged(bool visible) { }
        public void OnSystemSettingsChanged() { }
        public bool OnClosing()=>true;
        public void OnClosed() { }
        public PlatformCursor GetCursor(Point point)=>PlatformCursor.Arrow;
        public SemanticTree? GetSemanticTree()=>tree;
    }
}
