namespace Nazor.Controls.Tests.Navigation.Fixtures;

public sealed class PipsPagerProbe : ComponentBase
{
    public int Count { get; set; } = 12;
    public int Selected { get; set; }
    public int MaxVisible { get; set; } = 5;
    public bool Disabled { get; set; }
    public bool Arrows { get; set; } = true;
    public Orientation Orientation { get; set; }
    public List<int> Changes { get; } = [];
    public PipsPager Pager { get; private set; } = null!;
    public void Refresh() => StateHasChanged();
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<PipsPager>(0);
        builder.AddComponentParameter(1, nameof(PipsPager.Count), Count);
        builder.AddComponentParameter(2, nameof(PipsPager.SelectedIndex), Selected);
        builder.AddComponentParameter(3, nameof(PipsPager.SelectedIndexChanged), EventCallback.Factory.Create<int>(this, value => { Selected = value; Changes.Add(value); }));
        builder.AddComponentParameter(4, nameof(PipsPager.MaxVisiblePips), MaxVisible);
        builder.AddComponentParameter(5, nameof(PipsPager.Disabled), Disabled);
        builder.AddComponentParameter(6, nameof(PipsPager.ShowNavigationButtons), Arrows);
        builder.AddComponentParameter(7, nameof(PipsPager.Orientation), Orientation);
        builder.AddComponentReferenceCapture(8, component => Pager = (PipsPager)component);
        builder.CloseComponent();
    }
}
