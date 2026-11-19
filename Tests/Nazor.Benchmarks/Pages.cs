using Nazor.Controls;
using Nazor.Rendering;

namespace Nazor.Benchmarks;

internal sealed class DashboardPage : ComponentBase
{
    public const int Count = 300;

    public const string StyleSheet = """
        .page { height: 100%; overflow-y: auto; }
        .grid { display: flex; flex-direction: row; flex-wrap: wrap; gap: 8px; padding: 8px; }
        .card { width: 180px; padding: 12px; gap: 4px; background: white; border: 1px solid #d0d0d0; border-radius: 6px; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.2); }
        .card:hover { background: #eef4ff; }
        .title { font-weight: bold; }
        .value { font-size: 20px; }
        """;

    public List<CardView> Cards { get; } = [];

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0);
        builder.AddComponentParameter(1, "Class", "page");
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenComponent<Container>(0);
            content.AddComponentParameter(1, "Class", "grid");
            content.AddComponentParameter(2, "ChildContent", (RenderFragment)(grid =>
            {
                for (var i = 0; i < Count; i++)
                {
                    grid.OpenComponent<CardView>(0);
                    grid.SetKey(i);
                    grid.AddComponentParameter(1, "Index", i);
                    grid.AddComponentParameter(2, "Register", (Action<CardView>)Cards.Add);
                    grid.CloseComponent();
                }
            }));
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

internal sealed class CardView : ComponentBase
{
    [Parameter] public int Index { get; set; }

    [Parameter] public Action<CardView>? Register { get; set; }

    public string Value { get; private set; } = "";

    protected override void OnInitialized()
    {
        Value = $"{Index * 37 % 1000} pz";
        Register?.Invoke(this);
    }

    public void SetValue(string value)
    {
        Value = value;
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0);
        builder.AddComponentParameter(1, "Class", "card");
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(card =>
        {
            Text(card, 0, "title", $"Prodotto {Index}");
            Text(card, 10, "value", Value);
            card.OpenComponent<Button>(20);
            card.AddComponentParameter(21, "ChildContent", (RenderFragment)(button => button.AddContent(0, "Apri")));
            card.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private static void Text(RenderTreeBuilder builder, int sequence, string className, string text)
    {
        builder.OpenComponent<TextBlock>(sequence);
        builder.AddComponentParameter(sequence + 1, "Class", className);
        builder.AddComponentParameter(sequence + 2, "ChildContent", (RenderFragment)(content => content.AddContent(0, text)));
        builder.CloseComponent();
    }
}

internal sealed class OrdersPage : ComponentBase
{
    public const int Count = 100_000;

    private static readonly int[] Orders = Enumerable.Range(0, Count).ToArray();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<ListView<int>>(0);
        builder.AddComponentParameter(1, "Items", Orders);
        builder.AddComponentParameter(2, "ItemText", (Func<int, string>)(o => $"Ordine {o}"));
        builder.AddComponentParameter(3, "Style", "height: 600px; width: 400px");
        builder.CloseComponent();
    }
}

internal sealed class MotionPage : ComponentBase
{
    public const int Count = 60;

    public const string StyleSheet = """
        .boxes { display: flex; flex-direction: row; flex-wrap: wrap; gap: 6px; padding: 6px; }
        .box { width: 60px; height: 40px; background: #3a7bd5; border-radius: 4px; transition: transform 320ms linear, background-color 320ms linear; }
        .box.moved { transform: translateX(40px); background: #d53a7b; }
        """;

    private bool moved;

    public void Toggle()
    {
        moved = !moved;
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0);
        builder.AddComponentParameter(1, "Class", "boxes");
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(boxes =>
        {
            for (var i = 0; i < Count; i++)
            {
                boxes.OpenComponent<Container>(0);
                boxes.SetKey(i);
                boxes.AddComponentParameter(1, "Class", moved ? "box moved" : "box");
                boxes.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }
}

internal sealed class ArticlePage : ComponentBase
{
    public const int Count = 40;

    public const string StyleSheet = """
        .article { padding: 16px; gap: 8px; }
        """;

    private const string Paragraph = "Il gestionale degli ordini mostra per ogni cliente gli ordini aperti, le consegne previste e le fatture da emettere. " +
        "Ogni riga si apre nel dettaglio con i prodotti, le quantità e le note del magazzino; i filtri restano quando si cambia pagina.";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Container>(0);
        builder.AddComponentParameter(1, "Class", "article");
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(article =>
        {
            for (var i = 0; i < Count; i++)
            {
                article.OpenComponent<TextBlock>(0);
                article.SetKey(i);
                article.AddComponentParameter(1, "ChildContent", (RenderFragment)(text => text.AddContent(0, $"{i + 1}. {Paragraph}")));
                article.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }
}
