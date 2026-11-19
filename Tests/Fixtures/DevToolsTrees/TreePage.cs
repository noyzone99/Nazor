using Nazor;
using Nazor.Controls;
using Nazor.Rendering;

namespace Nazor.DevTools.Fixtures;

public sealed class TreePage : ComponentBase
{
    public bool Reverse { get; set; }
    public bool Show { get; set; } = true;
    public int Version { get; set; }
    public static int GetterCalls;
    public string Dangerous { get { GetterCalls++; throw new InvalidOperationException("Do not inspect application getters."); } }
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<PassThrough>(0, typeof(TreePage), this);
        builder.AddComponentParameter(1, "ChildContent", (RenderFragment)(b =>
        {
            foreach (var key in Reverse ? new[] { "right", "left" } : new[] { "left", "right" })
            {
                b.OpenComponent<MultiOutput>(0, typeof(TreePage), this);
                b.SetKey(key);
                b.AddComponentParameter(1, "Text", key + "-" + Version);
                b.CloseComponent();
            }
            b.OpenComponent<Container>(10, typeof(TreePage), this);
            b.AddComponentParameter(11, "ChildContent", (RenderFragment)(projected =>
            {
                if (Show)
                {
                    projected.OpenComponent<MultiOutput>(0, typeof(TreePage), this);
                    projected.SetKey("projected");
                    projected.AddComponentParameter(1, "Text", "projected");
                    projected.CloseComponent();
                    projected.OpenComponent<Empty>(5, typeof(TreePage), this);
                    projected.CloseComponent();
                }
            }));
            b.CloseComponent();
        }));
        builder.CloseComponent();
        builder.OpenComponent<Empty>(20, typeof(TreePage), this);
        builder.SetKey(PoisonValue.Instance);
        builder.CloseComponent();
        builder.OpenElement(30, "payload");
        builder.AddAttribute(31, "payload", PoisonValue.Instance);
        builder.CloseElement();
    }
}

public sealed class PassThrough : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
}

public sealed class MultiOutput : ComponentBase
{
    [Parameter] public string Text { get; set; } = "";
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Piece");
        builder.AddAttribute(10, "id", Text);
        builder.AddAttribute(11, "class", "piece");
        builder.AddAttribute(12, "style", "width: 80px; height: 24px; margin: 4px; border: 2px solid red; transform: translateX(10px)");
        builder.AddContent(1, Text);
        builder.CloseElement();
        builder.OpenElement(2, "Hidden");
        builder.AddAttribute(3, "style", "display: none");
        builder.CloseElement();
    }
}

public sealed class Empty : ComponentBase;

public sealed class PoisonValue : IFormattable
{
    public static readonly PoisonValue Instance = new();
    public static int Calls;
    public override string ToString() { Calls++; throw new InvalidOperationException("Unknown ToString executed."); }
    public string ToString(string? format, IFormatProvider? provider) { Calls++; throw new InvalidOperationException("Unknown formatter executed."); }
}

public sealed class LargeTree : ComponentBase
{
    [Parameter] public int Count { get; set; }
    [Parameter] public string Text { get; set; } = "";
    [Parameter] public int Depth { get; set; }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        for (var i = 0; i < Depth; i++) builder.OpenRegion(0);
        for (var i = 0; i < Count; i++) { builder.OpenElement(0, "n"); builder.CloseElement(); }
        if (Text.Length > 0) builder.AddContent(1, Text);
        for (var i = 0; i < Depth; i++) builder.CloseRegion();
    }
}

public sealed class PoisonParameter : ComponentBase
{
    public static int GetterCalls;
    [Parameter] public object? Value { get { GetterCalls++; throw new InvalidOperationException("Do not inspect parameter getters."); } set { } }
}
