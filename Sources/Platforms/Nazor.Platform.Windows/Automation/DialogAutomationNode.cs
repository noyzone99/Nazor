using System.Runtime.InteropServices.Marshalling;
using Nazor.Accessibility;

namespace Nazor.Platform.Windows.Automation;

[GeneratedComClass]
internal sealed partial class DialogAutomationNode(AutomationBridge bridge,SemanticNode node) : AutomationNode(bridge,node),IWindowProvider
{
    public override object? GetPatternProvider(int patternId) => patternId == 10009 ? this : base.GetPatternProvider(patternId);
    public void SetVisualState(int state)
    {
        if (state != 0) throw new InvalidOperationException("Window-local dialogs cannot be maximized or minimized.");
    }
    public void Close() => Ui(()=>
    {
        if (!Bridge.Tree.Dismiss(Node)) throw new InvalidOperationException("This dialog requires an explicit action.");
    });
    public bool WaitForInputIdle(int milliseconds) => Ui(()=>true);
    public bool GetCanMaximize() => false;
    public bool GetCanMinimize() => false;
    public bool GetIsModal() => Ui(()=>Node.Element?.Attributes.GetValueOrDefault("modal") is true);
    public int GetWindowVisualState() => 0;
    public int GetWindowInteractionState() => 2;
    public bool GetIsTopmost() => false;
}
