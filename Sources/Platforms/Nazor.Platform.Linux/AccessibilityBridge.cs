using System.Globalization;
using System.Reflection;
using System.Text;
using Nazor.Accessibility;
using Nazor.Graphics;
using Tmds.DBus.Protocol;

namespace Nazor.Platform.Linux;

internal sealed partial class AccessibilityBridge : IDisposable
{
    private static readonly string toolkitVersion = typeof(AccessibilityBridge).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
    private readonly LinuxDesktopPlatform platform;
    private readonly DesktopConnection bus;
    private readonly string address, name;
    private readonly ApplicationObject application;
    private readonly Cache cache;
    private readonly Dictionary<LinuxDesktopWindow, WindowTree> trees=[];
    private readonly List<AccessibleObject> objects=[];
    private readonly (string,ObjectPath) parent=NullReference;
    private int nextWindow;
    private bool disposed;
    private static readonly (string,ObjectPath) NullReference=("",new("/org/a11y/atspi/null"));

    internal AccessibilityBridge(LinuxDesktopPlatform platform, DesktopConnection session)
    {
        this.platform=platform;
        address=platform.Wait(session.CreateProxy<IAccessibilityBus>("org.a11y.Bus","/org/a11y/bus").GetAddressAsync());
        bus=new DesktopConnection(address);
        try
        {
            name=platform.Wait(bus.ConnectAsync()).LocalName;
            application=new(this); cache=new(this); objects.Add(application);
            platform.Wait(RegisterAsync([application,cache]));
            parent=platform.Wait(bus.CreateProxy<IAccessibilitySocket>("org.a11y.atspi.Registry","/org/a11y/atspi/accessible/root").EmbedAsync(Reference(application)));
        }
        catch { bus.Dispose(); throw; }
    }
    private async Task<bool> RegisterAsync(IEnumerable<IDBusObject> values) { await bus.RegisterObjectsAsync(values).ConfigureAwait(false); return true; }
    private (string,ObjectPath) Reference(AccessibleObject? value) => value is null ? NullReference : (name,value.ObjectPath);
    private AccessibleObject? Find(WindowTree tree, SemanticNode? node) => node is null ? null : tree.Nodes.GetValueOrDefault(node.Id);
    internal void Sync(LinuxDesktopWindow window,SemanticTree? tree)
    {
        if (disposed || tree is null) return;
        if (!trees.TryGetValue(window,out var entry))
        {
            entry=new(this,window,tree,++nextWindow); trees.Add(window,entry);
            entry.Subscribe();
        }
        Refresh(entry);
    }
    private void Refresh(WindowTree tree)
    {
        if (tree.Syncing || disposed) return;
        tree.Syncing=true;
        try
        {
            var added=new List<AccessibleObject>();
            foreach (var node in tree.Tree.Descendants())
                if (!tree.Nodes.ContainsKey(node.Id))
                {
                    var value=new AccessibleObject(this,tree,node,new($"/org/a11y/atspi/accessible/w{tree.Id}_n{node.Id}"));
                    tree.Nodes.Add(node.Id,value); objects.Add(value); added.Add(value);
                }
            if (added.Count!=0)
            {
                platform.Wait(RegisterAsync(added));
                foreach (var value in added) cache.Add(value.Item());
                application.Signal("ChildrenChanged","add",trees.Keys.ToList().IndexOf(tree.Window),0,Reference(tree.Nodes[0]));
            }
        }
        finally { tree.Syncing=false; }
    }
    internal void Remove(LinuxDesktopWindow window)
    {
        if (!trees.Remove(window,out var tree)) return;
        tree.Unsubscribe();
        foreach (var value in tree.Nodes.Values) { value.Defunct=true; cache.Remove(Reference(value)); bus.UnregisterObject(value); objects.Remove(value); }
        application.Signal("ChildrenChanged","remove",0,0,Reference(tree.Nodes.GetValueOrDefault(0)));
    }
    public void Dispose()
    {
        if (disposed) return; disposed=true;
        foreach (var tree in trees.Values) tree.Unsubscribe();
        trees.Clear(); bus.Dispose();
    }

    private sealed class WindowTree(AccessibilityBridge owner,LinuxDesktopWindow window,SemanticTree tree,int id)
    {
        internal readonly LinuxDesktopWindow Window=window;
        internal readonly SemanticTree Tree=tree;
        internal readonly int Id=id;
        internal readonly Dictionary<int,AccessibleObject> Nodes=[];
        internal bool Syncing;
        private AccessibleObject? lastFocus;
        internal void Subscribe() { Tree.FocusChanged+=Focus; Tree.PropertyChanged+=Property; Tree.ChildrenChanged+=Children; Tree.NodeRemoved+=Removed; Tree.Announced+=Announce; Tree.LiveRegionChanged+=Live; }
        internal void Unsubscribe() { Tree.FocusChanged-=Focus; Tree.PropertyChanged-=Property; Tree.ChildrenChanged-=Children; Tree.NodeRemoved-=Removed; Tree.Announced-=Announce; Tree.LiveRegionChanged-=Live; }
        private void Focus(SemanticNode? node)
        {
            lastFocus?.Signal("StateChanged","focused",0,0,0);
            lastFocus = node is null ? null : Nodes.GetValueOrDefault(node.Id);
            lastFocus?.Signal("StateChanged","focused",1,0,0);
        }
        private void Property(SemanticNode node,SemanticProperty property,object? oldValue,object? value)
        {
            if (!Nodes.TryGetValue(node.Id,out var accessible)) return;
            if (property==SemanticProperty.Name) accessible.Signal("PropertyChange","accessible-name",0,0,node.Name);
            else if (property==SemanticProperty.Description) accessible.Signal("PropertyChange","accessible-description",0,0,node.Description ?? "");
            else if (property==SemanticProperty.IsRequired) accessible.Signal("StateChanged","required",node.IsRequired ? 1:0,0,0);
            else if (property==SemanticProperty.IsInvalid) accessible.Signal("StateChanged","invalid-entry",node.IsInvalid ? 1:0,0,0);
            else if (property==SemanticProperty.Value && !node.IsPassword)
            { accessible.Signal("TextChanged","delete",0,Count(oldValue as string ?? ""),oldValue as string ?? ""); accessible.Signal("TextChanged","insert",0,Count(node.Value ?? ""),node.Value ?? ""); }
            else if (property==SemanticProperty.TextSelection)
            { accessible.Signal("TextCaretMoved","",ToOffset(node.Value ?? "",node.TextSelection?.Caret ?? 0),0,0); accessible.Signal("TextSelectionChanged","",0,0,0); }
            else if (property==SemanticProperty.IsChecked)
            {
                accessible.Signal("StateChanged",node.Role==SemanticRole.ToggleButton ? "pressed" : node.Role is SemanticRole.ListItem or SemanticRole.TreeItem or SemanticRole.TabItem ? "selected":"checked",node.IsChecked==true ? 1:0,0,0);
                if (node.Role is SemanticRole.CheckBox or SemanticRole.MenuCheckBox) accessible.Signal("StateChanged","indeterminate",node.IsChecked is null ? 1:0,0,0);
            }
            else if (property==SemanticProperty.IsExpanded) accessible.Signal("StateChanged","expanded",node.IsExpanded==true ? 1:0,0,0);
            else if (property==SemanticProperty.IsEnabled) accessible.Signal("StateChanged","enabled",node.IsEnabled ? 1:0,0,0);
            else if (property==SemanticProperty.RangeValue) accessible.Signal("PropertyChange","accessible-value",0,0,node.Range?.Value ?? 0);
            else if (property==SemanticProperty.IsOffscreen) accessible.Signal("StateChanged","showing",node.IsOffscreen ? 0:1,0,0);
            else if (property==SemanticProperty.ScrollAvailable)
            {
                owner.cache.Add(accessible.Item());
                if (node.Role==SemanticRole.Group) accessible.Signal("PropertyChange","accessible-role",0,0,Role(node));
            }
            else if (property is SemanticProperty.HorizontalScrollPercent or SemanticProperty.VerticalScrollPercent or SemanticProperty.HorizontalViewSize or SemanticProperty.VerticalViewSize)
                accessible.Signal("VisibleDataChanged","",0,0,0);
        }
        private void Children(SemanticNode node) { owner.Refresh(this); if (Nodes.TryGetValue(node.Id,out var value)) value.Signal("ChildrenChanged","add",-1,0,NullReference); }
        private void Removed(SemanticNode node) { if (Nodes.Remove(node.Id,out var value)) { value.Defunct=true; value.Signal("StateChanged","defunct",1,0,0); owner.cache.Remove(owner.Reference(value)); owner.bus.UnregisterObject(value); owner.objects.Remove(value); } }
        private void Announce(SemanticNode node,string text,AnnouncementPriority priority) { if (Nodes.TryGetValue(node.Id,out var value)) value.Signal("Announcement","",priority==AnnouncementPriority.Assertive ? 2:1,0,text); }
        private void Live(SemanticNode node) => Announce(node,node.Name,node.LiveSetting==LiveSetting.Assertive ? AnnouncementPriority.Assertive:AnnouncementPriority.Polite);
    }

    internal static int Count(string text) => text.EnumerateRunes().Count();
    internal static int ToUtf16(string text,int offset)
    {
        if (offset<0) return text.Length;
        var index=0; foreach (var rune in text.EnumerateRunes()) { if (offset--==0) break; index+=rune.Utf16SequenceLength; } return index;
    }
    internal static int ToOffset(string text,int utf16) => Count(text[..Math.Clamp(utf16,0,text.Length)]);
    internal static uint Role(SemanticNode? node) => node?.Role switch
    {
        SemanticRole.DocumentList=>31,
        SemanticRole.DocumentListItem=>32,
        SemanticRole.DefinitionList=>121,
        SemanticRole.DefinitionTerm=>122,
        SemanticRole.DefinitionDescription=>123,
        SemanticRole.Heading=>83,
        SemanticRole.Form=>87,
        SemanticRole.Dialog=>16,
        SemanticRole.AlertDialog=>2,
        SemanticRole.ToggleButton=>62,
        SemanticRole.Meter=>103,
        SemanticRole.Table=>55,
        SemanticRole.RowGroup=>39,
        SemanticRole.Row=>90,
        SemanticRole.Cell=>56,
        SemanticRole.ColumnHeader=>57,
        SemanticRole.RowHeader=>59,
        SemanticRole.MenuCheckBox=>8,
        SemanticRole.MenuRadio=>45,
        SemanticRole.Separator=>50,
        null=>75, SemanticRole.Window=>23, SemanticRole.Group=>node.Scroll is not null ? 49u:39u, SemanticRole.Text=>116, SemanticRole.Button=>43,
        SemanticRole.CheckBox=>7,SemanticRole.TextBox=>node.IsPassword ? 40u:61u,SemanticRole.Image=>27,
        SemanticRole.Menu=>33,SemanticRole.MenuItem=>35,SemanticRole.ToolTip=>64,SemanticRole.RadioButton=>44,SemanticRole.Switch=>62,
        SemanticRole.Slider=>51,SemanticRole.ProgressBar=>42,SemanticRole.ComboBox=>11,SemanticRole.List=>31,SemanticRole.ListItem=>32,
        SemanticRole.Tree=>65,SemanticRole.TreeItem=>91,SemanticRole.TabList=>38,SemanticRole.TabItem=>37,SemanticRole.Splitter=>50,
        SemanticRole.Hyperlink=>88,SemanticRole.Status=>54,SemanticRole.SplitButton=>129,SemanticRole.MenuBar=>34,SemanticRole.ToolBar=>63,
        SemanticRole.Calendar=>5,SemanticRole.TitleBar=>104,_=>0
    };

    private sealed class ApplicationObject(AccessibilityBridge owner) : AccessibleObject(owner,null,null,new("/org/a11y/atspi/accessible/root")), IAccessibleApplication
    {
        private int id;
        private Dictionary<string,object> Properties() => new() { ["ToolkitName"]="Nazor",["Version"]=toolkitVersion,["ToolkitVersion"]=toolkitVersion,["AtspiVersion"]="2.1",["InterfaceVersion"]=1u,["Id"]=id };
        Task<object> IAccessibleApplication.GetAsync(string property) => Task.FromResult(Properties()[property]);
        Task<IDictionary<string,object>> IAccessibleApplication.GetAllAsync() => Task.FromResult<IDictionary<string,object>>(Properties());
        Task IAccessibleApplication.SetAsync(string property,object value) { if (property=="Id" && value is int number) id=number; return Task.CompletedTask; }
        public Task<string> GetLocaleAsync(uint kind) => Task.FromResult(CultureInfo.CurrentCulture.Name);
        public Task<string> GetApplicationBusAddressAsync() => Task.FromResult(Bridge.address);
    }

    private sealed class Cache(AccessibilityBridge owner) : IAccessibleCache
    {
        private Action<AccessibleCacheItem>? add;
        private Action<(string,ObjectPath)>? remove;
        public ObjectPath ObjectPath => new("/org/a11y/atspi/cache");
        public Task<AccessibleCacheItem[]> GetItemsAsync() => owner.platform.InvokeAsync(() => owner.objects.Where(value=>!value.Defunct).Select(value=>value.Item()).ToArray());
        internal void Add(AccessibleCacheItem item) => add?.Invoke(item);
        internal void Remove((string,ObjectPath) reference) => remove?.Invoke(reference);
        public Task<IDisposable> WatchAddAccessibleAsync(Action<AccessibleCacheItem> handler) { add+=handler; return Task.FromResult<IDisposable>(new BusSubscription(()=>add-=handler)); }
        public Task<IDisposable> WatchRemoveAccessibleAsync(Action<(string,ObjectPath)> handler) { remove+=handler; return Task.FromResult<IDisposable>(new BusSubscription(()=>remove-=handler)); }
    }

    private partial class AccessibleObject(AccessibilityBridge owner,WindowTree? tree,SemanticNode? node,ObjectPath path) :
        IAccessible,IAccessibleAction,IAccessibleComponent,IAccessibleSelection,IAccessibleValue,IAccessibleEvents,IAccessibleText,IAccessibleEditableText,IAccessibleTable,IAccessibleTableCell
    {
        internal readonly SemanticNode? Node=node;
        protected AccessibilityBridge Bridge => owner;
        internal bool Defunct;
        public ObjectPath ObjectPath => path;
        private readonly Dictionary<string,Action<(string,int,int,object,IDictionary<string,object>)>> signals=[];
        private Task<T> OnUi<T>(Func<T> action) => owner.platform.InvokeAsync(() => { tree?.Tree.EnsureCurrent(); return action(); });
        private List<AccessibleObject> Children => tree is null ? owner.trees.Values.Select(value=>value.Nodes.GetValueOrDefault(0)).OfType<AccessibleObject>().ToList() :
            (Node!.Role==SemanticRole.Table ? tree.Tree.TableCells(Node) : Node.Children).Select(child=>owner.Find(tree,child)).OfType<AccessibleObject>().ToList();
        private SemanticNode? NativeParent => tree is not null && Node?.Role is SemanticRole.Cell or SemanticRole.ColumnHeader or SemanticRole.RowHeader ? tree!.Tree.ContainingTable(Node!) : Node?.Parent;
        private int NativeIndex => tree is null || Node is null ? -1 : NativeParent?.Role==SemanticRole.Table ? tree.Tree.TableCells(NativeParent).ToList().IndexOf(Node) : NativeParent?.Children.ToList().IndexOf(Node) ?? -1;
        private (string,ObjectPath) Parent => tree is null ? owner.parent : owner.Reference(NativeParent is null ? owner.application : owner.Find(tree,NativeParent));
        private Dictionary<string,object> Properties() => new()
        {
            ["version"]=1u,["Name"]=Node?.Name ?? LinuxDesktopWindow.AppId,["Description"]=Node?.Description ?? "",["Parent"]=Parent,
            ["ChildCount"]=Children.Count,["Locale"]=CultureInfo.CurrentCulture.Name,["AccessibleId"]=Node?.AutomationId ?? "",["HelpText"]=Node?.HelpText ?? ""
        };
        public Task<object> GetAsync(string property) => OnUi(() => Properties()[property]);
        public Task<IDictionary<string,object>> GetAllAsync() => OnUi<IDictionary<string,object>>(Properties);
        public Task<(string,ObjectPath)> GetChildAtIndexAsync(int index) => OnUi(()=>owner.Reference(Children.ElementAtOrDefault(index)));
        public Task<(string,ObjectPath)[]> GetChildrenAsync() => OnUi(()=>Children.Select(owner.Reference).ToArray());
        public Task<int> GetIndexInParentAsync() => OnUi(()=>tree is null ? -1 : Node?.Parent is null ? owner.trees.Keys.ToList().IndexOf(tree.Window) : NativeIndex);
        public Task<(uint,(string,ObjectPath)[])[]> GetRelationSetAsync() => OnUi(()=>Node?.LabeledBy is { } label && tree is not null ? new[] { (2u,new[] { owner.Reference(owner.Find(tree,label)) }) }:[]);
        public Task<uint> GetRoleAsync() => OnUi(()=>Role(Node));
        public Task<string> GetRoleNameAsync() => OnUi(()=>Node?.Role.ToString() ?? "Application");
        public Task<string> GetLocalizedRoleNameAsync() => OnUi(()=>Node?.RoleDescription ?? Node?.Role.ToString() ?? "Application");
        public Task<uint[]> GetStateAsync() => OnUi(States);
        private uint[] States()
        {
            ulong states=0;
            void Add(int state,bool enabled) { if (enabled) states|=1UL<<state; }
            Add(6,Defunct || Node?.IsDetached==true); Add(8,Node?.IsEnabled!=false); Add(24,Node?.IsEnabled!=false);
            Add(11,Node?.IsFocusable==true); Add(12,Node?.HasFocus==true); Add(30,!Defunct); Add(25,Node?.IsOffscreen!=true && !Defunct);
            Add(32,Node?.IsChecked is null && Node?.Role is SemanticRole.CheckBox or SemanticRole.MenuCheckBox);
            Add(20,Node?.Role==SemanticRole.ToggleButton && Node.IsChecked==true);
            Add(43,Node?.Element?.Attributes.GetValueOrDefault("readonly") is true); Add(16,Node?.Element?.Attributes.GetValueOrDefault("modal") is true); Add(33,Node?.IsRequired==true); Add(36,Node?.IsInvalid==true);
            Add(4,Node?.IsChecked==true); Add(9,Node?.IsExpanded.HasValue==true); Add(10,Node?.IsExpanded==true);
            Add(18,Node?.CanSelectMultiple==true); Add(22,Node?.Role is SemanticRole.ListItem or SemanticRole.TreeItem or SemanticRole.TabItem);
            Add(23,Node?.IsChecked==true && Node.Role is SemanticRole.ListItem or SemanticRole.TreeItem or SemanticRole.TabItem);
            Add(7,Node?.Role==SemanticRole.TextBox && Node.IsEnabled && Node.Element?.Attributes.GetValueOrDefault("readonly") is not true); Add(41,Node?.Role is SemanticRole.CheckBox or SemanticRole.Switch or SemanticRole.RadioButton or SemanticRole.ToggleButton or SemanticRole.MenuCheckBox or SemanticRole.MenuRadio);
            return [(uint)states,(uint)(states>>32)];
        }
        public Task<IDictionary<string,string>> GetAttributesAsync() => OnUi<IDictionary<string,string>>(()=>new Dictionary<string,string> { ["toolkit"]="Nazor",["id"]=Node?.AutomationId ?? "",["level"]=Node?.Level?.ToString(CultureInfo.InvariantCulture) ?? "",["posinset"]=Node?.PositionInSet?.ToString(CultureInfo.InvariantCulture) ?? "",["setsize"]=Node?.SizeOfSet?.ToString(CultureInfo.InvariantCulture) ?? "",["roledescription"]=Node?.RoleDescription ?? "" });
        public Task<(string,ObjectPath)> GetApplicationAsync() => Task.FromResult(owner.Reference(owner.application));
        private string[] Interfaces()
        {
            var result=new List<string> { "org.a11y.atspi.Accessible","org.a11y.atspi.Component" };
            if (Node is null) result.Add("org.a11y.atspi.Application");
            if (Actions.Length>0) result.Add("org.a11y.atspi.Action");
            if (Node?.Role==SemanticRole.Table) result.Add("org.a11y.atspi.Table");
            if (Node?.Role is SemanticRole.Cell or SemanticRole.ColumnHeader or SemanticRole.RowHeader) result.Add("org.a11y.atspi.TableCell");
            if (Node?.Range is not null) result.Add("org.a11y.atspi.Value");
            if (Node?.Role is SemanticRole.List or SemanticRole.Tree or SemanticRole.TabList or SemanticRole.ComboBox) result.Add("org.a11y.atspi.Selection");
            if (Node?.Role is SemanticRole.TextBox or SemanticRole.Text) result.Add("org.a11y.atspi.Text");
            if (Node?.Role==SemanticRole.TextBox) result.Add("org.a11y.atspi.EditableText");
            return result.ToArray();
        }
        public Task<string[]> GetInterfacesAsync() => OnUi(Interfaces);
        internal AccessibleCacheItem Item() => new() { Reference=owner.Reference(this),Application=owner.Reference(owner.application),Parent=Parent,Index=NativeIndex,Children=Children.Count,Interfaces=Interfaces(),Name=Node?.Name ?? LinuxDesktopWindow.AppId,Role=Role(Node),Description=Node?.Description ?? "",States=States() };
        private string[] Actions
        {
            get
            {
                string[] actions=Node?.IsExpanded.HasValue==true ? ["activate","expand","collapse"] :
                    Node?.Role is SemanticRole.ToggleButton or SemanticRole.MenuCheckBox or SemanticRole.MenuRadio or SemanticRole.Button or SemanticRole.CheckBox or SemanticRole.Switch or SemanticRole.RadioButton or SemanticRole.MenuItem or SemanticRole.Hyperlink or SemanticRole.TreeItem or SemanticRole.SplitButton ? ["activate"] : [];
                return Node?.Scroll is not null ? [..actions,"scroll-up","scroll-down","scroll-left","scroll-right","page-up","page-down","page-left","page-right"] : actions;
            }
        }
        Task<object> IAccessibleAction.GetAsync(string property) => OnUi<object>(()=>property=="version" ? (object)1u:Actions.Length);
        Task<IDictionary<string,object>> IAccessibleAction.GetAllAsync() => OnUi<IDictionary<string,object>>(()=>new Dictionary<string,object> { ["version"]=1u,["NActions"]=Actions.Length });
        public Task<string> GetDescriptionAsync(int index) => OnUi(()=>Actions.ElementAtOrDefault(index) ?? "");
        public Task<string> GetNameAsync(int index) => GetDescriptionAsync(index);
        public Task<string> GetLocalizedNameAsync(int index) => GetDescriptionAsync(index);
        public Task<string> GetKeyBindingAsync(int index) => Task.FromResult("");
        public Task<(string,string,string)[]> GetActionsAsync() => OnUi(()=>Actions.Select(action=>(action,action,"")).ToArray());
        public Task<bool> DoActionAsync(int index) => OnUi(()=>
        {
            if (tree is null || Node is null || Defunct || !Node.IsEnabled) return false;
            var action=Actions.ElementAtOrDefault(index);
            if (Node.Scroll is { } scroll)
            {
                var scrolled=action switch { "scroll-up"=>tree.Tree.ScrollBy(Node,0,-48),"scroll-down"=>tree.Tree.ScrollBy(Node,0,48),"scroll-left"=>tree.Tree.ScrollBy(Node,-48,0),"scroll-right"=>tree.Tree.ScrollBy(Node,48,0),
                    "page-up"=>tree.Tree.ScrollBy(Node,0,-scroll.ViewportHeight),"page-down"=>tree.Tree.ScrollBy(Node,0,scroll.ViewportHeight),"page-left"=>tree.Tree.ScrollBy(Node,-scroll.ViewportWidth,0),"page-right"=>tree.Tree.ScrollBy(Node,scroll.ViewportWidth,0),_=> (bool?)null };
                if (scrolled.HasValue) return scrolled.Value;
            }
            return action switch { "expand"=>tree.Tree.SetExpanded(Node,true),"collapse"=>tree.Tree.SetExpanded(Node,false),"activate"=>Node.Role switch { SemanticRole.CheckBox or SemanticRole.Switch or SemanticRole.ToggleButton or SemanticRole.MenuCheckBox or SemanticRole.MenuRadio=>tree.Tree.Toggle(Node),SemanticRole.ComboBox=>tree.Tree.SetExpanded(Node,Node.IsExpanded!=true),SemanticRole.RadioButton or SemanticRole.TreeItem=>tree.Tree.Select(Node),_=>tree.Tree.Invoke(Node) },_=>false };
        });
        private (int,int,int,int) Bounds(uint coordinates)
        {
            var rect=Node?.Bounds ?? default;
            return coordinates==0 ? (int.MinValue,int.MinValue,(int)rect.Width,(int)rect.Height) : ((int)rect.X,(int)rect.Y,(int)rect.Width,(int)rect.Height);
        }
        public Task<bool> ContainsAsync(int x,int y,uint coordinates) => OnUi(()=>coordinates!=0 && Node?.Bounds.Contains(new(x,y))==true);
        public Task<(string,ObjectPath)> GetAccessibleAtPointAsync(int x,int y,uint coordinates) => OnUi(()=>tree is not null && coordinates!=0 ? owner.Reference(owner.Find(tree,tree.Tree.HitTest(new(x,y)))):NullReference);
        public Task<(int,int,int,int)> GetExtentsAsync(uint coordinates) => OnUi(()=>Bounds(coordinates));
        public Task<(int,int)> GetPositionAsync(uint coordinates) => OnUi(()=> { var value=Bounds(coordinates); return (value.Item1,value.Item2); });
        public Task<(int,int)> GetSizeAsync() => OnUi(()=> { var value=Bounds(1); return (value.Item3,value.Item4); });
        public Task<uint> GetLayerAsync() => Task.FromResult(3u);
        public Task<short> GetMDIZOrderAsync() => Task.FromResult((short)0);
        public Task<bool> GrabFocusAsync() => OnUi(()=>tree is not null && Node is not null && tree.Tree.Focus(Node));
        public Task<double> GetAlphaAsync() => Task.FromResult(1.0);
        public Task<bool> SetExtentsAsync(int x,int y,int width,int height,uint coordinates) => Task.FromResult(false);
        public Task<bool> SetPositionAsync(int x,int y,uint coordinates) => Task.FromResult(false);
        public Task<bool> SetSizeAsync(int width,int height) => OnUi(()=> { if (tree is null || Node!=tree.Tree.Root) return false; tree.Window.Resize(new(width,height)); return true; });
        public Task<bool> ScrollToAsync(uint type) => OnUi(()=>tree is not null && Node is not null && tree.Tree.ScrollIntoView(Node));
        public Task<bool> ScrollToPointAsync(uint coordinates,int x,int y) => ScrollToAsync(0);
        private IReadOnlyList<SemanticNode> Selected => tree is not null && Node is not null ? tree.Tree.SelectedItems(Node):[];
        Task<object> IAccessibleSelection.GetAsync(string property) => OnUi<object>(()=>property=="version" ? (object)1u:Selected.Count);
        Task<IDictionary<string,object>> IAccessibleSelection.GetAllAsync() => OnUi<IDictionary<string,object>>(()=>new Dictionary<string,object> { ["version"]=1u,["NSelectedChildren"]=Selected.Count });
        public Task<(string,ObjectPath)> GetSelectedChildAsync(int index) => OnUi(()=>tree is null ? NullReference:owner.Reference(owner.Find(tree,Selected.ElementAtOrDefault(index))));
        public Task<bool> SelectChildAsync(int index) => OnUi(()=>tree is not null && Node?.Children.ElementAtOrDefault(index) is { } child && (Node.CanSelectMultiple ? tree.Tree.SetSelected(child,true):tree.Tree.Select(child)));
        public Task<bool> DeselectSelectedChildAsync(int index) => OnUi(()=>tree is not null && Selected.ElementAtOrDefault(index) is { } selected && tree.Tree.SetSelected(selected,false));
        public Task<bool> IsChildSelectedAsync(int index) => OnUi(()=>Node?.Children.ElementAtOrDefault(index)?.IsChecked==true);
        public Task<bool> SelectAllAsync() => OnUi(()=> { if (tree is null || Node?.CanSelectMultiple!=true) return false; foreach (var child in Node.Children) if (child.IsChecked!=true) tree.Tree.SetSelected(child,true); return true; });
        public Task<bool> ClearSelectionAsync() => OnUi(()=> { if (tree is null || Node?.CanSelectMultiple!=true) return false; foreach (var child in Selected.ToArray()) tree.Tree.SetSelected(child,false); return true; });
        public Task<bool> DeselectChildAsync(int index) => OnUi(()=>tree is not null && Node?.Children.ElementAtOrDefault(index) is { } child && tree.Tree.SetSelected(child,false));
        private Dictionary<string,object> ValueProperties() => new() { ["version"]=1u,["MinimumValue"]=Node?.Range?.Minimum ?? 0,["MaximumValue"]=Node?.Range?.Maximum ?? 0,["MinimumIncrement"]=Node?.Range?.SmallChange ?? 0,["CurrentValue"]=Node?.Range?.Value ?? 0,["Text"]=Node?.IsPassword==true ? "":Node?.Value ?? "" };
        Task<object> IAccessibleValue.GetAsync(string property) => OnUi(()=>ValueProperties()[property]);
        Task<IDictionary<string,object>> IAccessibleValue.GetAllAsync() => OnUi<IDictionary<string,object>>(ValueProperties);
        Task IAccessibleValue.SetAsync(string property,object value) => OnUi(()=> { if (property!="CurrentValue" || value is not double number || !double.IsFinite(number) || tree is null || Node is null || !tree.Tree.SetRangeValue(Node,number)) throw new DBusErrorReplyException("org.freedesktop.DBus.Error.InvalidArgs","The range value cannot be set."); return true; });
        private string Text => Node?.IsPassword==true ? "" : Node?.Value ?? (Node?.Role==SemanticRole.Text ? Node.Name:"");
        private Dictionary<string,object> TextProperties() => new() { ["version"]=1u,["CharacterCount"]=Count(Text),["CaretOffset"]=ToOffset(Text,Node?.TextSelection?.Caret ?? 0) };
        Task<object> IAccessibleText.GetAsync(string property) => OnUi(()=>TextProperties()[property]);
        Task<IDictionary<string,object>> IAccessibleText.GetAllAsync() => OnUi<IDictionary<string,object>>(TextProperties);
        private (int Start,int End) Range(int start,int end)
        {
            var count=Count(Text); if (end==-1) end=count;
            if (start<0 || start>count || end<start || end>count) throw new DBusErrorReplyException("org.freedesktop.DBus.Error.InvalidArgs","Invalid text range.");
            return (ToUtf16(Text,start),ToUtf16(Text,end));
        }
        public Task<string> GetTextAsync(int start,int end) => OnUi(()=> { var range=Range(start,end); return Text[range.Start..range.End]; });
        private (string,int,int) StringAt(int offset,uint granularity,int direction=0)
        {
            var text=Text;
            if (offset<0 || offset>Count(text)) return ("",-1,-1);
            var unit=granularity switch { 0=>TextUnit.Character,1=>TextUnit.Word,3=>TextUnit.Line,4=>TextUnit.Paragraph,_=>TextUnit.Paragraph };
            var lines=tree is not null && Node is not null ? tree.Tree.TextLines(Node):new[] { (0,text.Length) };
            var boundaries=TextUnits.Boundaries(text,unit,lines);
            if (granularity==2) boundaries=[0,.. Enumerable.Range(0,text.Length).Where(index=>text[index] is '.' or '!' or '?' or '\n').Select(index=>index+1).Append(text.Length).Distinct().Order()];
            var enclosing=TextUnits.Enclosing(boundaries,ToUtf16(text,offset));
            if (direction<0) { if (enclosing.Start==0) return ("",0,0); enclosing=TextUnits.Enclosing(boundaries,enclosing.Start-1); }
            if (direction>0) { if (enclosing.End==text.Length) return ("",Count(text),Count(text)); enclosing=TextUnits.Enclosing(boundaries,enclosing.End); }
            return (text[enclosing.Start..enclosing.End],ToOffset(text,enclosing.Start),ToOffset(text,enclosing.End));
        }
        public Task<(string,int,int)> GetStringAtOffsetAsync(int offset,uint granularity) => OnUi(()=>StringAt(offset,granularity));
        private static uint Granularity(uint boundary) => boundary switch { 0=>0,1 or 2=>1,3 or 4=>2,5 or 6=>3,_=>4 };
        public Task<(string,int,int)> GetTextBeforeOffsetAsync(int offset,uint boundary) => OnUi(()=>StringAt(offset,Granularity(boundary),-1));
        public Task<(string,int,int)> GetTextAtOffsetAsync(int offset,uint boundary) => OnUi(()=>StringAt(offset,Granularity(boundary)));
        public Task<(string,int,int)> GetTextAfterOffsetAsync(int offset,uint boundary) => OnUi(()=>StringAt(offset,Granularity(boundary),1));
        public Task<int> GetCharacterAtOffsetAsync(int offset) => OnUi(()=>offset<0 || offset>=Count(Text) ? -1:Rune.GetRuneAt(Text,ToUtf16(Text,offset)).Value);
        public Task<bool> SetCaretOffsetAsync(int offset) => SetSelectionAsync(0,offset,offset);
        public Task<int> GetNSelectionsAsync() => OnUi(()=>Node?.TextSelection is { } selection && selection.Start!=selection.End ? 1:0);
        public Task<(int,int)> GetSelectionAsync(int index) => OnUi(()=>index==0 && Node?.TextSelection is { } selection ? (ToOffset(Text,selection.Start),ToOffset(Text,selection.End)):(-1,-1));
        public Task<bool> AddSelectionAsync(int start,int end) => SetSelectionAsync(0,start,end);
        public Task<bool> RemoveSelectionAsync(int index) => OnUi(()=>index==0 && tree is not null && Node is not null && tree.Tree.SelectText(Node,Node.TextSelection?.Caret ?? 0,Node.TextSelection?.Caret ?? 0));
        public Task<bool> SetSelectionAsync(int index,int start,int end) => OnUi(()=> { var range=Range(start,end); return index==0 && tree is not null && Node is not null && tree.Tree.SelectText(Node,range.Start,range.End); });
        private (int,int,int,int) TextExtents(int start,int end,uint coordinates)
        {
            var range=Range(start,end);
            var rectangles=tree is not null && Node is not null ? tree.Tree.TextBounds(Node,range.Start,range.End):[];
            if (rectangles.Count==0) return Bounds(coordinates);
            var x=rectangles.Min(rect=>rect.X); var y=rectangles.Min(rect=>rect.Y);
            var width=rectangles.Max(rect=>rect.X+rect.Width)-x; var height=rectangles.Max(rect=>rect.Y+rect.Height)-y;
            return coordinates==0 ? (int.MinValue,int.MinValue,(int)width,(int)height):((int)x,(int)y,(int)width,(int)height);
        }
        public Task<(int,int,int,int)> GetCharacterExtentsAsync(int offset,uint coordinates) => OnUi(()=>TextExtents(offset,Math.Min(Count(Text),offset+1),coordinates));
        public Task<(int,int,int,int)> GetRangeExtentsAsync(int start,int end,uint coordinates) => OnUi(()=>TextExtents(start,end,coordinates));
        public Task<int> GetOffsetAtPointAsync(int x,int y,uint coordinates) => OnUi(()=>coordinates==0 || tree is null || Node is null ? -1:ToOffset(Text,tree.Tree.TextIndexAt(Node,new(x,y))));
        public Task<string> GetAttributeValueAsync(int offset,string attribute) => Task.FromResult("");
        Task<(IDictionary<string,string>,int,int)> IAccessibleText.GetAttributesAsync(int offset) => OnUi(()=>((IDictionary<string,string>)new Dictionary<string,string>(),0,Count(Text)));
        public Task<IDictionary<string,string>> GetDefaultAttributesAsync() => Task.FromResult<IDictionary<string,string>>(new Dictionary<string,string>());
        public Task<IDictionary<string,string>> GetDefaultAttributeSetAsync() => GetDefaultAttributesAsync();
        public Task<(IDictionary<string,string>,int,int)> GetAttributeRunAsync(int offset,bool defaults) => ((IAccessibleText)this).GetAttributesAsync(offset);
        public Task<(int,int,string,object)[]> GetBoundedRangesAsync(int x,int y,int width,int height,uint coordinates,uint xClip,uint yClip) => OnUi(()=>
        {
            if (coordinates==0 || tree is null || Node is null || Node.IsPassword) return [];
            var start=ToOffset(Text,tree.Tree.TextIndexAt(Node,new(x,y))); var end=ToOffset(Text,tree.Tree.TextIndexAt(Node,new(x+width,y+height)));
            if (end<start) (start,end)=(end,start);
            var range=Range(start,end); return new[] { (start,end,Text[range.Start..range.End],(object)0) };
        });
        public Task<bool> ScrollSubstringToAsync(int start,int end,uint type) => ScrollToAsync(type);
        public Task<bool> ScrollSubstringToPointAsync(int start,int end,uint coordinates,int x,int y) => ScrollToPointAsync(coordinates,x,y);
        public Task<bool> SetTextContentsAsync(string text) => OnUi(()=>tree is not null && Node is not null && tree.Tree.SetValue(Node,text));
        public Task<bool> InsertTextAsync(int position,string text,int length) => OnUi(()=>
        { if (tree is null || Node is null || Node.IsPassword || position<0 || position>Count(Text)) return false; var index=ToUtf16(Text,position); return tree.Tree.SetValue(Node,Text.Insert(index,text[..ToUtf16(text,length)])); });
        public Task CopyTextAsync(int start,int end) => OnUi(()=> { var range=Range(start,end); if (tree is not null && Node?.IsPassword!=true) tree.Window.Clipboard.SetText(Text[range.Start..range.End]); return true; });
        public Task<bool> CutTextAsync(int start,int end) => OnUi(()=> { var range=Range(start,end); return tree is not null && Node is not null && !Node.IsPassword && tree.Window.Clipboard.SetText(Text[range.Start..range.End]) && tree.Tree.SetValue(Node,Text.Remove(range.Start,range.End-range.Start)); });
        public Task<bool> DeleteTextAsync(int start,int end) => OnUi(()=> { var range=Range(start,end); return tree is not null && Node is not null && !Node.IsPassword && tree.Tree.SetValue(Node,Text.Remove(range.Start,range.End-range.Start)); });
        public Task<bool> PasteTextAsync(int position) => OnUi(()=>tree is not null && Node is not null && !Node.IsPassword && position>=0 && position<=Count(Text) && tree.Window.Clipboard.GetText() is { } pasted && tree.Tree.SetValue(Node,Text.Insert(ToUtf16(Text,position),pasted)));
        internal void Signal(string signal,string detail,int first,int second,object value)
        {
            Action<(string,int,int,object,IDictionary<string,object>)>? handler;
            lock (signals) signals.TryGetValue(signal,out handler);
            handler?.Invoke((detail,first,second,value,new Dictionary<string,object>()));
        }
        private Task<IDisposable> Watch(string signal,Action<(string,int,int,object,IDictionary<string,object>)> handler)
        {
            lock (signals) { signals.TryGetValue(signal,out var previous); signals[signal]=previous+handler; }
            return Task.FromResult<IDisposable>(new BusSubscription(()=> { lock(signals) { if (signals.TryGetValue(signal,out var current)) { current-=handler; if (current is null) signals.Remove(signal); else signals[signal]=current; } } }));
        }
        public Task<IDisposable> WatchPropertyChangeAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler)=>Watch("PropertyChange",handler);
        public Task<IDisposable> WatchStateChangedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler)=>Watch("StateChanged",handler);
        public Task<IDisposable> WatchChildrenChangedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler)=>Watch("ChildrenChanged",handler);
        public Task<IDisposable> WatchTextChangedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler)=>Watch("TextChanged",handler);
        public Task<IDisposable> WatchTextCaretMovedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler)=>Watch("TextCaretMoved",handler);
        public Task<IDisposable> WatchTextSelectionChangedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler)=>Watch("TextSelectionChanged",handler);
        public Task<IDisposable> WatchSelectionChangedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler)=>Watch("SelectionChanged",handler);
        public Task<IDisposable> WatchVisibleDataChangedAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler)=>Watch("VisibleDataChanged",handler);
        public Task<IDisposable> WatchAnnouncementAsync(Action<(string,int,int,object,IDictionary<string,object>)> handler)=>Watch("Announcement",handler);
    }
}
