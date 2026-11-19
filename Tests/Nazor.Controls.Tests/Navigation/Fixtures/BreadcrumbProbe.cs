using Nazor.Input;

namespace Nazor.Controls.Tests.Navigation.Fixtures;

public sealed record Crumb(int Id, string Label);

public sealed class BreadcrumbProbe : ComponentBase
{
    public Crumb[] Path { get; set; } = [new(0, "Home"), new(1, "Documents"), new(2, "Design"), new(3, "Northwind"), new(4, "Images"), new(5, "Current")];
    public double Width { get; set; } = 320;
    public bool Disabled { get; set; }
    public bool Apply { get; set; } = true;
    public bool DuplicateKeys { get; set; }
    public BreadcrumbBar<Crumb> Bar { get; private set; } = null!;
    public List<BreadcrumbItemInvokedEventArgs<Crumb>> Requests { get; } = [];
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "Container");
        builder.AddAttribute(1, "style", "display: flex; flex-direction: column; gap: 12px; width: " + Width.ToString(System.Globalization.CultureInfo.InvariantCulture) + "px");
        builder.OpenComponent<BreadcrumbBar<Crumb>>(10);
        builder.AddComponentParameter(11, nameof(BreadcrumbBar<Crumb>.Items), Path);
        builder.AddComponentParameter(12, nameof(BreadcrumbBar<Crumb>.ItemKey), (Func<Crumb, object>)(item => DuplicateKeys ? 0 : item.Id));
        builder.AddComponentParameter(13, nameof(BreadcrumbBar<Crumb>.ItemText), (Func<Crumb, string>)(item => item.Label));
        builder.AddComponentParameter(14, nameof(BreadcrumbBar<Crumb>.Disabled), Disabled);
        builder.AddComponentParameter(15, nameof(BreadcrumbBar<Crumb>.OnItemInvoked), EventCallback.Factory.Create<BreadcrumbItemInvokedEventArgs<Crumb>>(this, request =>
        {
            Requests.Add(request);
            if (Apply) Path = Path.Take(request.Index + 1).ToArray();
        }));
        builder.AddComponentReferenceCapture(16, component => Bar = (BreadcrumbBar<Crumb>)component);
        builder.CloseComponent();
        builder.OpenComponent<Button>(20);
        builder.AddComponentParameter(21, nameof(Control.Id), "after-path");
        builder.AddComponentParameter(22, nameof(Button.ChildContent), (RenderFragment)(content => content.AddContent(0, "Next")));
        builder.CloseComponent();
        builder.CloseElement();
    }
}
