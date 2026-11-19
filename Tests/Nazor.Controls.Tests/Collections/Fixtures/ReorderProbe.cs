using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Tests.Collections.Fixtures;

public sealed class ReorderEntry(int id)
{
    public int Id { get; } = id;
    public List<ReorderEntry> Children { get; } = [];
    public override string ToString() => "Item " + Id;
}

public sealed class ReorderProbe : ComponentBase
{
    public string Kind { get; set; } = "list";
    public List<ReorderEntry> Items { get; } = Enumerable.Range(1, 9).Select(id => new ReorderEntry(id)).ToList();
    public ReorderEntry? Selected { get; set; }
    public int SelectionChanges { get; private set; }
    public int Activations { get; private set; }
    public int ButtonClicks { get; private set; }
    public bool Enabled { get; set; } = true;
    public bool Disabled { get; set; }
    public bool Apply { get; set; } = true;
    public bool Buttons { get; set; }
    public bool Grouped { get; set; }
    public int? DisabledItem { get; set; }
    public List<ItemReorderRequest<ReorderEntry>> Requests { get; } = [];
    public Control View { get; private set; } = null!;
    public void Refresh() => StateHasChanged();

    protected override void OnInitialized() => Selected = Items[1];

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var type = Kind switch { "grid" => typeof(GridView<ReorderEntry>), "tree" => typeof(TreeView<ReorderEntry>), _ => typeof(ListView<ReorderEntry>) };
        builder.OpenComponent(0, type);
        builder.SetKey(Kind);
        builder.AddComponentParameter(1, Kind == "tree" ? "Roots" : "Items", Items.ToArray());
        builder.AddComponentParameter(2, "ItemText", (Func<ReorderEntry, string>)(item => item.ToString()));
        builder.AddComponentParameter(3, "SelectedItem", Selected);
        builder.AddComponentParameter(4, "SelectedItemChanged", EventCallback.Factory.Create<ReorderEntry?>(this, item => { Selected = item; SelectionChanges++; }));
        builder.AddComponentParameter(5, "CanReorderItems", Enabled);
        builder.AddComponentParameter(6, "OnItemReorder", EventCallback.Factory.Create<ItemReorderRequest<ReorderEntry>>(this, Move));
        builder.AddComponentParameter(7, "Disabled", Disabled);
        builder.AddComponentParameter(8, "ItemDisabled", (Func<ReorderEntry, bool>)(item => item.Id == DisabledItem));
        builder.AddComponentParameter(9, "OnItemActivated", EventCallback.Factory.Create<ReorderEntry>(this, _ => Activations++));
        builder.AddComponentParameter(10, "ItemHeight", Kind == "grid" ? 60d : 32d);
        builder.AddComponentParameter(11, "Style", "width: 360px; height: 240px");
        if (Kind == "grid") builder.AddComponentParameter(12, "ItemWidth", 120d);
        if (Kind == "tree") builder.AddComponentParameter(13, "Children", (Func<ReorderEntry, IReadOnlyList<ReorderEntry>?>)(item => item.Children));
        else builder.AddComponentParameter(14, "ItemKey", (Func<ReorderEntry, object>)(item => item.Id));
        if (Kind == "list" && Grouped) builder.AddComponentParameter(17, "GroupBy", (Func<ReorderEntry, string>)(item => item.Id <= 3 ? "First" : "Second"));
        builder.AddComponentParameter(15, "ItemTemplate", (RenderFragment<ReorderEntry>)(item => content =>
        {
            content.AddContent(0, item.ToString());
            if (!Buttons) return;
            content.OpenComponent<Button>(10);
            content.AddComponentParameter(11, nameof(Control.Id), "child-" + item.Id);
            content.AddComponentParameter(12, nameof(Button.ChildContent), (RenderFragment)(text => text.AddContent(0, "Open")));
            content.AddComponentParameter(13, nameof(Button.OnClick), EventCallback.Factory.Create<PointerEventArgs>(this, () => ButtonClicks++));
            content.CloseComponent();
        }));
        builder.AddComponentReferenceCapture(16, component => View = (Control)component);
        builder.CloseComponent();
    }

    private void Move(ItemReorderRequest<ReorderEntry> request)
    {
        Requests.Add(request);
        if (!Apply) return;
        var siblings = Kind == "tree" ? FindSiblings(Items, request.Item) : Items;
        siblings.RemoveAt(request.OldIndex);
        siblings.Insert(request.NewIndex, request.Item);
    }

    private static List<ReorderEntry> FindSiblings(List<ReorderEntry> candidates, ReorderEntry item)
    {
        if (candidates.Contains(item)) return candidates;
        foreach (var candidate in candidates)
        {
            var result = FindSiblings(candidate.Children, item);
            if (result.Count > 0) return result;
        }
        return [];
    }
}
