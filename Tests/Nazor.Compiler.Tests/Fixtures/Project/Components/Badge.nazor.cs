using Nazor;

namespace ProjectSample.Components;

public partial class Badge
{
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public int Count { get; set; }
}
