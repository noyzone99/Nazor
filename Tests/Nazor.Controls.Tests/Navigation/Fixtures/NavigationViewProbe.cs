namespace Nazor.Controls.Tests.Navigation.Fixtures;

public sealed class NavigationViewProbe : ComponentBase
{
    public sealed class Item(string key, string text, params Item[] children)
    {
        public string Key { get; } = key;
        public string Text { get; } = text;
        public List<Item> Children { get; } = [.. children];
        public bool Disabled { get; set; }
    }
    public List<Item> Items { get; } = [new("home", "Home"), new("projects", "Projects", new("active", "Active projects"), new("archive", "Archive")), new("help", "Help"), new("settings", "Settings")];
    public Item? Selected { get; set; }
    public NavigationViewPaneDisplayMode Mode { get; set; }
    public double Width { get; set; } = 1100;
    public bool PaneOpen { get; set; } = true;
    public bool Disabled { get; set; }
    public bool FocusRouteContent { get; set; }
    public bool IconEditors { get; set; }
    public bool OuterScroll { get; set; }
    public List<string> Invoked { get; } = [];
    public List<string?> Selections { get; } = [];
    public List<string> Events { get; } = [];
    public List<bool> PaneChanges { get; } = [];
    public NavigationView<Item> View { get; private set; } = null!;
    private TextField? editor;
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (OuterScroll)
        {
            builder.OpenElement(1000, "NavigationProbeScroller");
            builder.AddAttribute(1001, "style", "height: 420px; width: 1200px; overflow-y: auto; overflow-x: hidden");
            builder.OpenElement(1002, "NavigationProbeSpacer"); builder.AddAttribute(1003, "style", "height: 80px; flex-shrink: 0"); builder.CloseElement();
        }
        builder.OpenComponent<NavigationView<Item>>(0);
        builder.AddComponentParameter(1, nameof(NavigationView<Item>.Items), Items);
        builder.AddComponentParameter(2, nameof(NavigationView<Item>.ItemKey), (Func<Item, object>)(item => item.Key));
        builder.AddComponentParameter(3, nameof(NavigationView<Item>.ItemText), (Func<Item, string>)(item => item.Text));
        builder.AddComponentParameter(4, nameof(NavigationView<Item>.Children), (Func<Item, IReadOnlyList<Item>?>)(item => item.Children));
        builder.AddComponentParameter(5, nameof(NavigationView<Item>.ItemDisabled), (Func<Item, bool>)(item => item.Disabled));
        builder.AddComponentParameter(6, nameof(NavigationView<Item>.SelectedItem), Selected);
        builder.AddComponentParameter(7, nameof(NavigationView<Item>.SelectedItemChanged), EventCallback.Factory.Create<Item?>(this, item => { Selected = item; Selections.Add(item?.Key); Events.Add("selection:" + item?.Key); }));
        builder.AddComponentParameter(8, nameof(NavigationView<Item>.OnItemInvoked), EventCallback.Factory.Create<Item>(this, async item =>
        {
            Invoked.Add(item.Key);
            Events.Add("invoke:" + item.Key);
            if (FocusRouteContent && editor is not null) await editor.InputElement.FocusAsync(true);
        }));
        builder.AddComponentParameter(9, nameof(NavigationView<Item>.PaneDisplayMode), Mode);
        builder.AddComponentParameter(10, nameof(NavigationView<Item>.IsPaneOpen), PaneOpen);
        builder.AddComponentParameter(11, nameof(NavigationView<Item>.IsPaneOpenChanged), EventCallback.Factory.Create<bool>(this, value => { PaneOpen = value; PaneChanges.Add(value); }));
        builder.AddComponentParameter(12, nameof(NavigationView<Item>.Disabled), Disabled);
        builder.AddComponentParameter(13, nameof(Control.Style), FormattableString.Invariant($"width: {Width}px; height: 400px"));
        builder.AddComponentParameter(14, nameof(NavigationView<Item>.Header), (RenderFragment)(content =>
        {
            content.OpenComponent<TextField>(0); content.AddComponentParameter(1, nameof(Control.Id), "header-editor"); content.CloseComponent();
        }));
        builder.AddComponentParameter(15, nameof(NavigationView<Item>.PaneHeader), (RenderFragment)(content =>
        {
            content.OpenComponent<TextField>(0); content.AddComponentParameter(1, nameof(Control.Id), "pane-editor"); content.CloseComponent();
        }));
        builder.AddComponentParameter(16, nameof(NavigationView<Item>.PaneFooter), (RenderFragment)(content => content.AddContent(0, "Account")));
        builder.AddComponentParameter(17, nameof(NavigationView<Item>.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<TextField>(0); content.AddComponentParameter(1, nameof(Control.Id), "main-editor");
            content.AddComponentParameter(2, nameof(TextField.Value), "Local document");
            content.AddComponentReferenceCapture(3, component => editor = (TextField)component); content.CloseComponent();
        }));
        if (IconEditors) builder.AddComponentParameter(18, nameof(NavigationView<Item>.ItemIcon), (RenderFragment<Item>)(item => content =>
        {
            content.OpenComponent<TextField>(0); content.AddComponentParameter(1, nameof(Control.Id), "icon-editor-" + item.Key); content.CloseComponent();
        }));
        builder.AddComponentReferenceCapture(19, component => View = (NavigationView<Item>)component);
        builder.CloseComponent();
        if (OuterScroll)
        {
            builder.OpenElement(1010, "NavigationProbeSpacer"); builder.AddAttribute(1011, "style", "height: 80px; flex-shrink: 0"); builder.CloseElement();
            builder.CloseElement();
        }
    }
}
