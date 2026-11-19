using System.Globalization;
using Nazor.Input;

namespace Nazor.Controls.Tests.Navigation.Fixtures;

public sealed class TabsDocumentProbe : ComponentBase
{
    public sealed class Document(string key, string label)
    {
        public string Key { get; set; } = key;
        public string Label { get; } = label;
        public bool Disabled { get; set; }
    }

    public List<Document> Documents { get; } = [new("a", "Alpha"), new("b", "Beta longer"), new("c", "Charlie")];
    public List<ItemReorderRequest<Tab>> Reorders { get; } = [];
    public List<TabDragOutRequest> DragOuts { get; } = [];
    public List<string> Closed { get; } = [];
    public Tabs? Strip { get; private set; }
    public string? SelectedKey { get; set; } = "a";
    public int SelectedIndex { get; set; }
    public bool ShowAdd { get; set; } = true;
    public bool AddEnabled { get; set; } = true;
    public bool CloseEnabled { get; set; } = true;
    public bool ReorderEnabled { get; set; } = true;
    public bool ReorderHandler { get; set; } = true;
    public bool DragOutEnabled { get; set; }
    public bool ApplyReorder { get; set; } = true;
    public bool ApplyDragOut { get; set; }
    public bool Nested { get; set; }
    public bool SupplyKeys { get; set; } = true;
    public double Width { get; set; } = 680;
    public int Adds { get; private set; }
    public TabsPresentation Presentation { get; set; } = TabsPresentation.Document;
    public TabWidthMode WidthMode { get; set; }
    public TabsOverflow Overflow { get; set; }
    private int next = 1;

    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Page");
        builder.AddAttribute(1, "style", "width: " + Width.ToString(CultureInfo.InvariantCulture) + "px");
        builder.OpenComponent<Tabs>(10);
        builder.AddComponentParameter(11, nameof(Tabs.Presentation), Presentation);
        builder.AddComponentParameter(12, nameof(Tabs.WidthMode), WidthMode);
        builder.AddComponentParameter(13, nameof(Tabs.ShowAddButton), ShowAdd);
        builder.AddComponentParameter(14, nameof(Tabs.OnAdd), AddEnabled ? EventCallback.Factory.Create(this, Add) : default);
        builder.AddComponentParameter(15, nameof(Tabs.CanReorderTabs), ReorderEnabled);
        builder.AddComponentParameter(16, nameof(Tabs.OnTabReorder), ReorderHandler ? EventCallback.Factory.Create<ItemReorderRequest<Tab>>(this, Reorder) : default);
        builder.AddComponentParameter(17, nameof(Tabs.OnTabDragOut), DragOutEnabled ? EventCallback.Factory.Create<TabDragOutRequest>(this, DragOut) : default);
        builder.AddComponentParameter(18, nameof(Tabs.SelectedKey), SelectedKey);
        builder.AddComponentParameter(19, nameof(Tabs.SelectedKeyChanged), EventCallback.Factory.Create<string?>(this, value => SelectedKey = value));
        builder.AddComponentParameter(20, nameof(Tabs.SelectedIndex), SelectedIndex);
        builder.AddComponentParameter(21, nameof(Tabs.SelectedIndexChanged), EventCallback.Factory.Create<int>(this, value => SelectedIndex = value));
        builder.AddComponentParameter(22, nameof(Tabs.KeepMounted), true);
        builder.AddComponentParameter(23, nameof(Tabs.Overflow), Overflow);
        builder.AddComponentParameter(24, nameof(Tabs.ChildContent), (RenderFragment)RenderDeclarations);
        builder.AddComponentReferenceCapture(25, component => Strip = (Tabs)component);
        builder.CloseComponent();
        builder.OpenComponent<Button>(30);
        builder.AddComponentParameter(31, nameof(Control.Id), "outside");
        builder.AddComponentParameter(32, nameof(Button.ChildContent), (RenderFragment)(content => content.AddContent(0, "Outside")));
        builder.CloseComponent();
        builder.CloseElement();
    }

    private void RenderDeclarations(RenderTreeBuilder builder)
    {
        foreach (var document in Documents)
        {
            builder.OpenComponent<Tab>(0);
            builder.SetKey(document.Key);
            builder.AddComponentParameter(1, nameof(Tab.Key), SupplyKeys ? document.Key : null);
            builder.AddComponentParameter(2, nameof(Tab.Label), document.Label);
            builder.AddComponentParameter(3, nameof(Tab.Class), "document-" + document.Key);
            builder.AddComponentParameter(4, nameof(Tab.Closable), true);
            builder.AddComponentParameter(5, nameof(Tab.Disabled), document.Disabled);
            builder.AddComponentParameter(6, nameof(Tab.OnClose), CloseEnabled ? EventCallback.Factory.Create(this, () => Close(document)) : default);
            builder.AddComponentParameter(7, nameof(Tab.Leading), (RenderFragment)(content => content.AddContent(0, "D")));
            builder.AddComponentParameter(8, nameof(Tab.ChildContent), (RenderFragment)(content =>
            {
                content.OpenComponent<TextField>(0);
                content.AddComponentParameter(1, nameof(Control.Id), "editor-" + document.Key);
                content.AddComponentParameter(2, nameof(TextField.Value), "Local " + document.Key);
                content.CloseComponent();
                if (!Nested || document.Key != "a") return;
                content.OpenComponent<Tabs>(10);
                content.AddComponentParameter(11, nameof(Control.Id), "inner");
                content.AddComponentParameter(12, nameof(Tabs.ChildContent), (RenderFragment)(inner =>
                {
                    for (var index = 0; index < 2; index++)
                    {
                        inner.OpenComponent<Tab>(0);
                        inner.AddComponentParameter(1, nameof(Tab.Key), "inner-" + index.ToString(CultureInfo.InvariantCulture));
                        inner.AddComponentParameter(2, nameof(Tab.Label), "Inner " + index.ToString(CultureInfo.InvariantCulture));
                        inner.CloseComponent();
                    }
                }));
                content.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private void Add()
    {
        Adds++;
        var document = new Document("new-" + (next++).ToString(CultureInfo.InvariantCulture), "New document");
        Documents.Add(document);
        SelectedKey = document.Key;
        SelectedIndex = Documents.Count - 1;
    }

    private void Close(Document document) { Closed.Add(document.Key); Documents.Remove(document); }

    private void Reorder(ItemReorderRequest<Tab> request)
    {
        Reorders.Add(request);
        if (!ApplyReorder) return;
        var document = Documents[request.OldIndex];
        Documents.RemoveAt(request.OldIndex);
        Documents.Insert(request.NewIndex, document);
    }

    private void DragOut(TabDragOutRequest request)
    {
        DragOuts.Add(request);
        if (ApplyDragOut) Documents.RemoveAll(document => document.Key == request.Tab.Key);
    }
}
