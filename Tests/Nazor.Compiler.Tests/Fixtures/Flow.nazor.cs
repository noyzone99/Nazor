using System.Threading.Tasks;

namespace Nazor.Generated;

public partial class Flow
{
    private bool visible = true;
    private readonly int[] values = [3, 1, 2];
    private async Task Toggle()
    {
        await Task.Yield();
        visible = !visible;
    }
}
