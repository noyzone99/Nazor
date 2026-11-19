using Nazor.Controls;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Application.Tests;
public sealed class Toolbar : ComponentBase
{
    public List<string> Clicks { get; } = [];
    public bool ThirdDisabled { get; set; } = true;

    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Toolbar));
        builder.AddComponentParameter(1, "Class", "toolbar");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
        {
            AddButton(b, 3, "first");
            AddButton(b, 10, "second");
            AddButton(b, 20, "third", ThirdDisabled);
        }));
        builder.CloseComponent();
    }

    private void AddButton(RenderTreeBuilder builder, int sequence, string name, bool disabled = false)
    {
        builder.OpenComponent<Button>(sequence, typeof(Toolbar));
        builder.AddComponentParameter(sequence + 1, "Id", name);
        builder.AddComponentParameter(sequence + 2, "Disabled", disabled);
        builder.AddComponentParameter(sequence + 3, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, (PointerEventArgs e) => Clicks.Add($"{name}{(e.IsSynthetic ? ":key" : "")}")));
        builder.AddAttribute(sequence + 4, "ChildContent", (RenderFragment)(c => c.AddContent(sequence + 5, name)));
        builder.CloseComponent();
    }
}

public sealed class Form : ComponentBase
{
    public string Name { get; set; } = "Sensore";
    public bool Disabled { get; set; } = true;
    public int FreeMaxLength { get; set; }
    public List<string> Changes { get; } = [];
    public bool OwnContextMenu { get; set; }

    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Form));
        builder.AddComponentParameter(1, "Class", "form");
        if (OwnContextMenu)
            builder.AddAttribute(18, "oncontextmenu", EventCallback.Factory.Create<PointerEventArgs>(this, () => Changes.Add("menu")));
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
        {
            b.OpenComponent<global::Nazor.Controls.TextField>(3, typeof(Form));
            b.AddComponentParameter(4, "Id", "name");
            b.AddComponentParameter(5, "Value", Name);
            b.AddComponentParameter(6, "ValueChanged", EventCallback.Factory.Create<string>(this, (string value) => Name = value));
            b.CloseComponent();
            b.OpenComponent<TextBlock>(7, typeof(Form));
            b.AddAttribute(8, "ChildContent", (RenderFragment)(c => c.AddContent(9, $"Nome: {Name}")));
            b.CloseComponent();
            b.OpenComponent<global::Nazor.Controls.TextField>(10, typeof(Form));
            b.AddComponentParameter(11, "Id", "free");
            b.AddComponentParameter(16, "MaxLength", FreeMaxLength);
            b.AddAttribute(17, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e => Changes.Add(e.Value as string ?? "")));
            b.CloseComponent();
            b.OpenComponent<global::Nazor.Controls.TextField>(12, typeof(Form));
            b.AddComponentParameter(13, "Id", "locked");
            b.AddComponentParameter(14, "Value", "fisso");
            b.AddComponentParameter(15, "Disabled", Disabled);
            b.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class Settings : WindowComponentBase
{
    private static readonly Nazor.Style.StyleSheet Sheet = Nazor.Nss.NssCompiler.Compile("Settings", """
        .page { display: flex; flex-direction: column; gap: 6px; padding: 8px; height: 100%; }
        .list { flex: 1; overflow-y: auto; border: 1px solid #c8c8c8; }
        """).StyleSheet!;
    static Settings() => Nazor.Style.StyleRegistry.RegisterScoped(typeof(Settings), () => Sheet);
    public bool Active { get; set; }
    public int Saves { get; set; }
    public int Rows { get; set; } = 30;

    public void Refresh() => StateHasChanged();
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<Container>(3, typeof(Settings));
            window.AddComponentParameter(4, "Class", "page");
            window.AddAttribute(5, "ChildContent", (RenderFragment)(page =>
            {
                page.OpenComponent<CheckBox>(6, typeof(Settings));
                page.AddComponentParameter(7, "Id", "active");
                page.AddComponentParameter(8, "IsChecked", Active);
                page.AddComponentParameter(9, "IsCheckedChanged", EventCallback.Factory.Create<bool>(this, (bool value) => Active = value));
                page.AddAttribute(10, "ChildContent", (RenderFragment)(c => c.AddContent(11, "Attivo")));
                page.CloseComponent();
                page.OpenComponent<Button>(12, typeof(Settings));
                page.AddComponentParameter(13, "Id", "save");
                page.AddComponentParameter(14, "AccessibleName", "Salva le impostazioni");
                page.AddComponentParameter(15, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Saves++));
                page.AddAttribute(16, "ChildContent", (RenderFragment)(c => c.AddContent(17, "Salva")));
                page.CloseComponent();
                page.OpenComponent<Container>(18, typeof(Settings));
                page.AddComponentParameter(19, "AccessibleName", "Riepilogo");
                page.AddAttribute(20, "ChildContent", (RenderFragment)(group =>
                {
                    group.OpenComponent<TextBlock>(21, typeof(Settings));
                    group.AddAttribute(22, "ChildContent", (RenderFragment)(c => c.AddContent(23, $"Elementi: {Rows}")));
                    group.CloseComponent();
                }));
                page.CloseComponent();
                page.OpenComponent<Container>(24, typeof(Settings));
                page.AddComponentParameter(25, "Id", "list");
                page.AddComponentParameter(26, "Class", "list");
                page.AddAttribute(27, "ChildContent", (RenderFragment)(list =>
                {
                    for (var i = 1; i <= Rows; i++)
                    {
                        var row = i;
                        list.OpenComponent<TextBlock>(28, typeof(Settings));
                        list.SetKey(row);
                        list.AddComponentParameter(29, "Id", $"row{row}");
                        list.AddAttribute(30, "ChildContent", (RenderFragment)(c => c.AddContent(31, $"Riga {row}")));
                        list.CloseComponent();
                    }
                }));
                page.CloseComponent();
            }));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Impostazioni";
    }
}

public sealed class Gallery : WindowComponentBase
{
    private static readonly Nazor.Style.StyleSheet Sheet = Nazor.Nss.NssCompiler.Compile("Gallery", """
        .strip { overflow-x: auto; overflow-y: hidden; width: 300px; height: 80px; }
        .row { display: flex; gap: 0; }
        .tile { width: 100px; height: 60px; flex-shrink: 0; }
        """).StyleSheet!;
    static Gallery() => Nazor.Style.StyleRegistry.RegisterScoped(typeof(Gallery), () => Sheet);
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<Container>(3, typeof(Gallery));
            window.AddComponentParameter(4, "Id", "strip");
            window.AddComponentParameter(5, "Class", "strip");
            window.AddAttribute(6, "ChildContent", (RenderFragment)(strip =>
            {
                strip.OpenComponent<Container>(7, typeof(Gallery));
                strip.AddComponentParameter(8, "Class", "row");
                strip.AddAttribute(9, "ChildContent", (RenderFragment)(row =>
                {
                    for (var i = 1; i <= 12; i++)
                    {
                        var tile = i;
                        row.OpenComponent<TextBlock>(10, typeof(Gallery));
                        row.SetKey(tile);
                        row.AddComponentParameter(11, "Id", $"tile{tile}");
                        row.AddComponentParameter(12, "Class", "tile");
                        row.AddAttribute(13, "ChildContent", (RenderFragment)(c => c.AddContent(14, $"Riquadro {tile}")));
                        row.CloseComponent();
                    }
                }));
                strip.CloseComponent();
            }));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Galleria";
    }
}

public sealed class Logos : WindowComponentBase
{
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            Image(window, 3, "logo", "Logo di Nazor", null);
            Image(window, 9, "decoration", null, null);
            Image(window, 15, "named", "", "Nome esplicito");
            window.OpenComponent<Icon>(21, typeof(Logos));
            window.AddComponentParameter(22, "Id", "add");
            window.AddComponentParameter(23, "Glyph", "+");
            window.AddComponentParameter(24, "AccessibleName", "Aggiungi");
            window.CloseComponent();
            window.OpenComponent<Icon>(25, typeof(Logos));
            window.AddComponentParameter(26, "Glyph", "*");
            window.CloseComponent();
        }));
    }

    private static void Image(RenderTreeBuilder builder, int sequence, string id, string? alt, string? name)
    {
        builder.OpenComponent<Image>(sequence, typeof(Logos));
        builder.AddComponentParameter(sequence + 1, "Id", id);
        builder.AddComponentParameter(sequence + 2, "Source", ImageSource.FromBytes([0]));
        builder.AddComponentParameter(sequence + 3, "Alt", alt);
        builder.AddComponentParameter(sequence + 4, "AccessibleName", name);
        builder.CloseComponent();
    }

    protected override void OnInitialized()
    {
        Window.Title = "Loghi";
    }
}

public sealed class Commands : WindowComponentBase
{
    private ContextMenu? menu;
    public int Copies { get; private set; }

    public Task OpenMenu() => menu!.ShowAsync(new PointerEventArgs { X = 10, Y = 10 });
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<Button>(3, typeof(Commands));
            window.AddComponentParameter(4, "Id", "save");
            window.AddComponentParameter(5, "Tooltip", "Salva il file");
            window.AddAttribute(6, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Salva")));
            window.CloseComponent();
            window.OpenComponent<ContextMenu>(7, typeof(Commands));
            window.AddAttribute(8, "ChildContent", (RenderFragment)(items =>
            {
                items.OpenComponent<MenuItem>(9, typeof(Commands));
                items.AddComponentParameter(10, "Id", "copy");
                items.AddComponentParameter(11, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Copies++));
                items.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Copia")));
                items.CloseComponent();
                items.OpenComponent<MenuItem>(13, typeof(Commands));
                items.AddComponentParameter(14, "Id", "paste");
                items.AddComponentParameter(15, "Disabled", true);
                items.AddAttribute(16, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Incolla")));
                items.CloseComponent();
            }));
            window.AddComponentReferenceCapture(17, reference => menu = (ContextMenu)reference);
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Comandi";
    }
}

public sealed class Choices : WindowComponentBase
{
    public string? Size { get; private set; } = "M";
    public bool Notify { get; private set; }
    public double Volume { get; private set; } = 40;

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<RadioGroup<string>>(3, typeof(Choices));
            window.AddComponentParameter(4, "AccessibleName", "Taglia");
            window.AddComponentParameter(5, "Value", Size);
            window.AddComponentParameter(6, "ValueChanged", EventCallback.Factory.Create<string>(this, value => Size = value));
            window.AddAttribute(7, "ChildContent", (RenderFragment)(group =>
            {
                foreach (var(size, i)in new[]
                {
                    "S",
                    "M",
                    "L"
                }.Select((s, i) => (s, i)))
                {
                    group.OpenComponent<RadioButton<string>>(8, typeof(Choices));
                    group.SetKey(size);
                    group.AddComponentParameter(9, "Id", "size-" + size);
                    group.AddComponentParameter(10, "Value", size);
                    group.AddComponentParameter(11, "Disabled", i == 2);
                    group.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Taglia " + size)));
                    group.CloseComponent();
                }
            }));
            window.CloseComponent();
            window.OpenComponent<Switch>(13, typeof(Choices));
            window.AddComponentParameter(14, "Id", "notify");
            window.AddComponentParameter(15, "IsOn", Notify);
            window.AddComponentParameter(16, "IsOnChanged", EventCallback.Factory.Create<bool>(this, value => Notify = value));
            window.AddAttribute(17, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Notifiche")));
            window.CloseComponent();
            window.OpenComponent<Slider>(18, typeof(Choices));
            window.AddComponentParameter(19, "Id", "volume");
            window.AddComponentParameter(20, "AccessibleName", "Volume");
            window.AddComponentParameter(21, "Value", Volume);
            window.AddComponentParameter(22, "ValueChanged", EventCallback.Factory.Create<double>(this, value => Volume = value));
            window.AddComponentParameter(23, "Step", 10.0);
            window.AddComponentParameter(24, "LargeStep", 20.0);
            window.CloseComponent();
            window.OpenComponent<ProgressBar>(25, typeof(Choices));
            window.AddComponentParameter(26, "Id", "download");
            window.AddComponentParameter(27, "AccessibleName", "Download");
            window.AddComponentParameter(28, "Value", 30.0);
            window.AddComponentParameter(29, "Maximum", 60.0);
            window.CloseComponent();
            window.OpenComponent<Spinner>(30, typeof(Choices));
            window.AddComponentParameter(31, "Id", "busy");
            window.AddComponentParameter(32, "AccessibleName", "Attendere");
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Scelte";
    }
}

public sealed class Booking : WindowComponentBase
{
    public DateOnly? Arrival { get; private set; } = new DateOnly(2026, 10, 15);

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<DatePicker>(3, typeof(Booking));
            window.AddComponentParameter(4, "Id", "arrival");
            window.AddComponentParameter(5, "AccessibleName", "Arrivo");
            window.AddComponentParameter(6, "Value", Arrival);
            window.AddComponentParameter(7, "ValueChanged", EventCallback.Factory.Create<DateOnly?>(this, value => Arrival = value));
            window.AddComponentParameter(8, "Culture", System.Globalization.CultureInfo.GetCultureInfo("it-IT"));
            window.AddComponentParameter(9, "Today", new DateOnly(2026, 10, 14));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Prenotazione";
    }
}

public sealed class Editor : WindowComponentBase
{
    public List<string> Log { get; } = [];

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<MenuBar>(3, typeof(Editor));
            window.AddComponentParameter(4, "Id", "menus");
            window.AddAttribute(5, "ChildContent", (RenderFragment)(bar =>
            {
                Menu(bar, 0, "file", "File", "Nuovo");
                Menu(bar, 10, "edit", "Modifica", "Annulla");
            }));
            window.CloseComponent();
            window.OpenComponent<Nazor.Controls.Toolbar>(6, typeof(Editor));
            window.AddComponentParameter(7, "Id", "tools");
            window.AddComponentParameter(8, "AccessibleName", "Formato");
            window.AddAttribute(9, "ChildContent", (RenderFragment)(tools =>
            {
                tools.OpenComponent<Button>(0, typeof(Editor));
                tools.AddComponentParameter(1, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Log.Add("Grassetto")));
                tools.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Grassetto")));
                tools.CloseComponent();
                tools.OpenComponent<Separator>(3, typeof(Editor));
                tools.CloseComponent();
                tools.OpenComponent<MenuButton>(4, typeof(Editor));
                tools.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Allinea")));
                tools.AddAttribute(6, "Menu", (RenderFragment)(menu => Item(menu, 0, "A sinistra")));
                tools.CloseComponent();
            }));
            window.CloseComponent();
        }));
    }

    private void Menu(RenderTreeBuilder builder, int sequence, string id, string title, string item)
    {
        builder.OpenComponent<MenuBarItem>(sequence, typeof(Editor));
        builder.AddComponentParameter(sequence + 1, "Id", id);
        builder.AddComponentParameter(sequence + 2, "ChildContent", (RenderFragment)(label => label.AddContent(0, title)));
        builder.AddComponentParameter(sequence + 3, "Content", (RenderFragment)(menu => Item(menu, 0, item)));
        builder.CloseComponent();
    }

    private void Item(RenderTreeBuilder builder, int sequence, string name)
    {
        builder.OpenComponent<MenuItem>(sequence, typeof(Editor));
        builder.AddComponentParameter(sequence + 1, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Log.Add(name)));
        builder.AddAttribute(sequence + 2, "ChildContent", (RenderFragment)(b => b.AddContent(0, name)));
        builder.CloseComponent();
    }

    protected override void OnInitialized()
    {
        Window.Title = "Editor";
    }
}

public sealed class SaveCommands : WindowComponentBase
{
    public List<string> Log { get; } = [];

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<SplitButton>(3, typeof(SaveCommands));
            window.AddComponentParameter(4, "Id", "save");
            window.AddComponentParameter(5, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Log.Add("save")));
            window.AddAttribute(6, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Salva")));
            window.AddAttribute(7, "Menu", (RenderFragment)(menu => Item(menu, 0, "Salva con nome")));
            window.CloseComponent();
            window.OpenComponent<MenuButton>(8, typeof(SaveCommands));
            window.AddComponentParameter(9, "Id", "sort");
            window.AddAttribute(10, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Ordina")));
            window.AddAttribute(11, "Menu", (RenderFragment)(menu => Item(menu, 0, "Per nome")));
            window.CloseComponent();
        }));
    }

    private void Item(RenderTreeBuilder builder, int sequence, string name)
    {
        builder.OpenComponent<MenuItem>(sequence, typeof(SaveCommands));
        builder.AddComponentParameter(sequence + 1, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Log.Add(name)));
        builder.AddAttribute(sequence + 2, "ChildContent", (RenderFragment)(b => b.AddContent(0, name)));
        builder.CloseComponent();
    }

    protected override void OnInitialized()
    {
        Window.Title = "Documento";
    }
}

public sealed class Notices : WindowComponentBase
{
    public int Clicks { get; private set; }
    public bool Open { get; private set; } = true;

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<Link>(3, typeof(Notices));
            window.AddComponentParameter(4, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Clicks++));
            window.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Guida")));
            window.CloseComponent();
            window.OpenComponent<InlineMessage>(6, typeof(Notices));
            window.AddComponentParameter(7, "Severity", Severity.Success);
            window.AddComponentParameter(8, "Title", "Salvato");
            window.AddComponentParameter(99, "IsClosable", true);
            window.AddComponentParameter(9, "IsOpen", Open);
            window.AddComponentParameter(10, "IsOpenChanged", EventCallback.Factory.Create<bool>(this, value => Open = value));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Avvisi";
    }
}

public sealed class Pages : WindowComponentBase
{
    public int Selected { get; private set; }

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<Tabs>(3, typeof(Pages));
            window.AddComponentParameter(4, "SelectedIndex", Selected);
            window.AddComponentParameter(5, "SelectedIndexChanged", EventCallback.Factory.Create<int>(this, value => Selected = value));
            window.AddAttribute(6, "ChildContent", (RenderFragment)(tabs =>
            {
                foreach (var(name, i)in new[]
                {
                    "Uno",
                    "Due"
                }.Select((n, i) => (n, i)))
                {
                    tabs.OpenComponent<Tab>(7, typeof(Pages));
                    tabs.SetKey(name);
                    tabs.AddComponentParameter(8, "Label", name);
                    tabs.AddAttribute(9, "ChildContent", (RenderFragment)(b =>
                    {
                        b.OpenComponent<TextBlock>(0, typeof(Pages));
                        b.AddAttribute(1, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Pagina " + name)));
                        b.CloseComponent();
                    }));
                    tabs.CloseComponent();
                }
            }));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Schede";
    }
}

public sealed class Panels : WindowComponentBase
{
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<Expander>(3, typeof(Panels));
            window.AddComponentParameter(4, "Header", "Avanzate");
            window.AddAttribute(5, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<TextBlock>(0, typeof(Panels));
                b.AddAttribute(1, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Dentro")));
                b.CloseComponent();
            }));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Pannelli";
    }
}

public sealed class FolderTree : WindowComponentBase
{
    public string? Selected { get; private set; }

    private static IReadOnlyList<string>? Children(string folder) => folder == "Documenti" ? ["Fatture", "Contratti"] : null;
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<TreeView<string>>(3, typeof(FolderTree));
            window.AddComponentParameter(4, "Id", "folders");
            window.AddComponentParameter(5, "AccessibleName", "Cartelle");
            window.AddComponentParameter(6, "Roots", new[] { "Documenti", "Immagini" });
            window.AddComponentParameter(7, "Children", (Func<string, IReadOnlyList<string>?>)Children);
            window.AddComponentParameter(8, "SelectedItem", Selected);
            window.AddComponentParameter(9, "SelectedItemChanged", EventCallback.Factory.Create<string?>(this, value => Selected = value));
            window.AddComponentParameter(10, "Style", "height: 160px");
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Cartelle";
    }
}

public sealed class Inbox : WindowComponentBase
{
    public string? Selected { get; private set; }

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<ListView<string>>(3, typeof(Inbox));
            window.AddComponentParameter(4, "Id", "messages");
            window.AddComponentParameter(5, "AccessibleName", "Messaggi");
            window.AddComponentParameter(6, "Items", Enumerable.Range(0, 50).Select(i => $"Messaggio {i}").ToArray());
            window.AddComponentParameter(7, "Style", "height: 120px");
            window.AddComponentParameter(8, "SelectedItem", Selected);
            window.AddComponentParameter(9, "SelectedItemChanged", EventCallback.Factory.Create<string?>(this, value => Selected = value));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Posta";
    }
}

public sealed class Article : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Article));
        builder.AddComponentParameter(1, "Style", "display: flex; flex-direction: column; align-items: flex-start; gap: 8px");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            Text(page, 3, "body", "Ordine numero 4521 spedito", true, null);
            Text(page, 10, "locked", "Testo non selezionabile", true, "user-select: none");
            Text(page, 20, "code", "AB-77-XY", true, "user-select: all");
            Text(page, 30, "plain", "Testo normale", false, null);
            page.OpenComponent<Button>(40, typeof(Article));
            page.AddComponentParameter(41, "Id", "ok");
            page.AddAttribute(42, "ChildContent", (RenderFragment)(b => b.AddContent(0, "OK")));
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private static void Text(RenderTreeBuilder builder, int sequence, string id, string text, bool selectable, string? style)
    {
        builder.OpenComponent<TextBlock>(sequence, typeof(Article));
        builder.AddComponentParameter(sequence + 1, "Id", id);
        builder.AddComponentParameter(sequence + 2, "IsTextSelectionEnabled", selectable);
        builder.AddComponentParameter(sequence + 3, "Style", style);
        builder.AddAttribute(sequence + 4, "ChildContent", (RenderFragment)(b => b.AddContent(0, text)));
        builder.CloseComponent();
    }
}

public sealed class Notes : ComponentBase
{
    public string Note { get; set; } = "";
    public List<string> Changes { get; } = [];

    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0, typeof(Notes));
        builder.AddComponentParameter(1, "Style", "display: flex; flex-direction: column; align-items: flex-start; gap: 8px");
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(page =>
        {
            page.OpenComponent<TextArea>(3, typeof(Notes));
            page.AddComponentParameter(4, "Id", "note");
            page.AddComponentParameter(6, "Style", "width: 160px");
            page.AddComponentParameter(7, "Value", Note);
            page.AddComponentParameter(8, "ValueChanged", EventCallback.Factory.Create<string>(this, value => Note = value));
            page.AddAttribute(9, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e => Changes.Add(e.Value as string ?? "")));
            page.CloseComponent();
            page.OpenComponent<TextArea>(10, typeof(Notes));
            page.AddComponentParameter(11, "Id", "fixed");
            page.AddComponentParameter(13, "Style", "width: 160px; height: 72px");
            page.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public sealed class Quantities : WindowComponentBase
{
    public double? Quantity { get; set; } = 2;

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<NumberField>(3, typeof(Quantities));
            window.AddComponentParameter(4, "Id", "quantity");
            window.AddComponentParameter(5, "AccessibleName", "Quantità");
            window.AddComponentParameter(6, "Value", Quantity);
            window.AddComponentParameter(7, "ValueChanged", EventCallback.Factory.Create<double?>(this, value => Quantity = value));
            window.AddComponentParameter(8, "Minimum", 0.0);
            window.AddComponentParameter(9, "Maximum", 100.0);
            window.AddComponentParameter(10, "Format", "0.##");
            window.CloseComponent();
            window.OpenComponent<Button>(11, typeof(Quantities));
            window.AddComponentParameter(12, "Id", "ok");
            window.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "OK")));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Quantità";
    }
}

public sealed class Login : WindowComponentBase
{
    public string User { get; set; } = "anna";
    public string Password { get; set; } = "";

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<global::Nazor.Controls.TextField>(3, typeof(Login));
            window.AddComponentParameter(4, "Id", "user");
            window.AddComponentParameter(5, "Value", User);
            window.AddComponentParameter(6, "ValueChanged", EventCallback.Factory.Create<string>(this, value => User = value));
            window.CloseComponent();
            window.OpenComponent<PasswordField>(7, typeof(Login));
            window.AddComponentParameter(8, "Id", "password");
            window.AddComponentParameter(9, "AccessibleName", "Password");
            window.AddComponentParameter(10, "Value", Password);
            window.AddComponentParameter(11, "ValueChanged", EventCallback.Factory.Create<string>(this, value => Password = value));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Accesso";
    }
}

public sealed class CountryPicker : WindowComponentBase
{
    public string? Country { get; private set; } = "FR";

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<Select<string>>(3, typeof(CountryPicker));
            window.AddComponentParameter(4, "Id", "country");
            window.AddComponentParameter(5, "AccessibleName", "Paese");
            window.AddComponentParameter(6, "Value", Country);
            window.AddComponentParameter(7, "ValueChanged", EventCallback.Factory.Create<string>(this, value => Country = value));
            window.AddAttribute(8, "ChildContent", (RenderFragment)(items =>
            {
                foreach (var(code, name)in new[]
                {
                    ("IT", "Italia"),
                    ("FR", "Francia"),
                    ("ES", "Spagna"),
                    ("DE", "Germania")
                }

                )
                {
                    items.OpenComponent<SelectItem<string>>(9, typeof(CountryPicker));
                    items.SetKey(code);
                    items.AddComponentParameter(10, "Value", code);
                    items.AddComponentParameter(11, "Disabled", code == "ES");
                    items.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, name)));
                    items.CloseComponent();
                }
            }));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Ordine";
    }
}

public sealed class MenuWindow : WindowComponentBase
{
    private ContextMenu? menu;
    public int Copies { get; private set; }

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(0, "style", "height: 100%");
        builder.AddAttribute(1, "oncontextmenu", EventCallback.Factory.Create<PointerEventArgs>(this, e => menu?.ShowAsync(e) ?? Task.CompletedTask));
        builder.AddContent(3, (RenderFragment)(window =>
        {
            window.OpenComponent<Button>(4, typeof(MenuWindow));
            window.AddComponentParameter(5, "Id", "save");
            window.AddComponentParameter(6, "Tooltip", "Salva il file");
            window.AddAttribute(7, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Salva")));
            window.CloseComponent();
            window.OpenComponent<ContextMenu>(8, typeof(MenuWindow));
            window.AddAttribute(9, "ChildContent", (RenderFragment)(items =>
            {
                items.OpenComponent<MenuItem>(10, typeof(MenuWindow));
                items.AddComponentParameter(11, "Id", "copy");
                items.AddComponentParameter(12, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, () => Copies++));
                items.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Copia")));
                items.CloseComponent();
                items.OpenComponent<Submenu>(20, typeof(MenuWindow));
                items.AddComponentParameter(21, "Id", "more");
                items.AddComponentParameter(22, "ChildContent", (RenderFragment)(label => label.AddContent(0, "More")));
                items.AddComponentParameter(23, "Content", (RenderFragment)(nested =>
                {
                    nested.OpenComponent<MenuItem>(0, typeof(MenuWindow));
                    nested.AddComponentParameter(1, "Id", "nested");
                    nested.AddComponentParameter(2, "ChildContent", (RenderFragment)(label => label.AddContent(0, "Nested command")));
                    nested.CloseComponent();
                }));
                items.CloseComponent();
            }));
            window.AddComponentReferenceCapture(14, reference => menu = (ContextMenu)reference);
            window.CloseComponent();
        }));
        builder.CloseElement();
    }

    protected override void OnInitialized()
    {
        Window.Title = "Menu";

    }
}

public sealed class Picture : WindowComponentBase
{
    public static string Path { get; set; } = "";

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(1, (RenderFragment)(window =>
        {
            window.OpenComponent<Image>(2, typeof(Picture));
            window.AddComponentParameter(2, "Style", "align-self: flex-start");
            window.AddComponentParameter(3, "Source", ImageSource.FromFile(Path));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
    }
}

public sealed class Described : WindowComponentBase
{
    public string Label { get; set; } = "Nome del sensore";

    public void Refresh() => StateHasChanged();
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<TextBlock>(3, typeof(Described));
            window.AddComponentParameter(4, "Id", "sensor-label");
            window.AddAttribute(5, "ChildContent", (RenderFragment)(t => t.AddContent(0, Label)));
            window.CloseComponent();
            window.OpenComponent<global::Nazor.Controls.TextField>(6, typeof(Described));
            window.AddComponentParameter(7, "Id", "sensor");
            window.AddComponentParameter(8, "LabeledBy", "sensor-label");
            window.AddComponentParameter(9, "AccessibleDescription", "Il nome che compare nei report");
            window.CloseComponent();
            window.OpenComponent<Button>(10, typeof(Described));
            window.AddComponentParameter(11, "Id", "refresh");
            window.AddComponentParameter(12, "Tooltip", "Aggiorna");
            window.CloseComponent();
            window.OpenComponent<Button>(13, typeof(Described));
            window.AddComponentParameter(14, "Id", "save");
            window.AddComponentParameter(15, "Tooltip", "Salva le modifiche (Ctrl+S)");
            window.AddAttribute(16, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Salva")));
            window.CloseComponent();
            window.OpenComponent<global::Nazor.Controls.TextField>(17, typeof(Described));
            window.AddComponentParameter(18, "Id", "orphan");
            window.AddComponentParameter(19, "LabeledBy", "missing");
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Sensore";
    }
}

public sealed class FlatTree : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Tree");
        builder.AddAttribute(1, "role", "tree");
        builder.AddAttribute(2, "id", "flat");
        foreach (var(name, level)in new[]
        {
            ("A", 1),
            ("A1", 2),
            ("B", 1),
            ("B1", 2),
            ("B2", 2),
            ("C", 1)
        }

        )
        {
            builder.OpenElement(3, "Item");
            builder.SetKey(name);
            builder.AddAttribute(4, "role", "treeitem");
            builder.AddAttribute(5, "level", level);
            builder.AddContent(6, name);
            builder.CloseElement();
        }

        builder.CloseElement();
    }
}

public sealed class LiveNotices : WindowComponentBase
{
    public bool InfoOpen { get; set; }
    public string Message { get; set; } = "Nuovo tentativo tra 5 secondi";
    public string Status { get; set; } = "Pronto";

    public void Refresh() => StateHasChanged();
    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<InlineMessage>(3, typeof(LiveNotices));
            window.AddComponentParameter(4, "Id", "connection");
            window.AddComponentParameter(5, "Severity", Severity.Error);
            window.AddComponentParameter(6, "Title", "Connessione persa");
            window.AddComponentParameter(7, "ChildContent", (RenderFragment)(b => b.AddContent(0, Message)));
            window.AddComponentParameter(8, "IsOpen", InfoOpen);
            window.CloseComponent();
            window.OpenComponent<Container>(9, typeof(LiveNotices));
            window.AddComponentParameter(10, "Id", "status");
            window.AddAttribute(11, "live", "polite");
            window.AddAttribute(12, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<TextBlock>(0, typeof(LiveNotices));
                b.AddAttribute(1, "ChildContent", (RenderFragment)(t => t.AddContent(0, Status)));
                b.CloseComponent();
            }));
            window.CloseComponent();
            window.OpenComponent<Button>(13, typeof(LiveNotices));
            window.AddComponentParameter(14, "Id", "save");
            window.AddAttribute(15, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Salva")));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Notifiche";
    }
}

public sealed class Recipients : WindowComponentBase
{
    public IReadOnlyCollection<string> Chosen { get; private set; } = ["Anna", "Carla"];

    protected override void BuildWindowContent(RenderTreeBuilder builder)
    {
        builder.AddContent(2, (RenderFragment)(window =>
        {
            window.OpenComponent<ListView<string>>(3, typeof(Recipients));
            window.AddComponentParameter(4, "Id", "recipients");
            window.AddComponentParameter(5, "AccessibleName", "Destinatari");
            window.AddComponentParameter(6, "Items", new[] { "Anna", "Bruno", "Carla", "Dario" });
            window.AddComponentParameter(7, "SelectionMode", Nazor.Controls.Primitives.SelectionMode.Multiple);
            window.AddComponentParameter(8, "SelectedItems", Chosen);
            window.AddComponentParameter(9, "SelectedItemsChanged", EventCallback.Factory.Create<IReadOnlyCollection<string>>(this, value => Chosen = value));
            window.CloseComponent();
        }));
    }

    protected override void OnInitialized()
    {
        Window.Title = "Destinatari";
    }
}
