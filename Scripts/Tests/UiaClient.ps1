param([long]$Handle, [string]$ToggleId, [string]$InvokeId)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -ReferencedAssemblies UIAutomationClient, UIAutomationTypes, WindowsBase -TypeDefinition @"
using System.Collections.Concurrent;
using System.Windows.Automation;
public static class NazorEventLog
{
    public static readonly ConcurrentQueue<string> Items = new ConcurrentQueue<string>();
    public static void OnProperty(object sender, AutomationPropertyChangedEventArgs e)
    {
        Items.Enqueue("event|property|" + e.Property.ProgrammaticName + "|" + e.NewValue);
    }
    public static void OnEvent(object sender, AutomationEventArgs e)
    {
        Items.Enqueue("event|" + e.EventId.ProgrammaticName);
    }
}
"@

$root = [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$Handle)
"window|$($root.Current.ControlType.ProgrammaticName)|$($root.Current.Name)"
$children = $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
foreach ($child in $children) {
    $c = $child.Current
    "child|$($c.ControlType.ProgrammaticName)|$($c.Name)|$($c.AutomationId)|$($c.FrameworkId)|$($c.IsEnabled)"
}

function Find([string]$id) {
    $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

$check = Find $ToggleId
$button = Find $InvokeId
$onProperty = [System.Delegate]::CreateDelegate([System.Windows.Automation.AutomationPropertyChangedEventHandler], [NazorEventLog].GetMethod('OnProperty'))
$onEvent = [System.Delegate]::CreateDelegate([System.Windows.Automation.AutomationEventHandler], [NazorEventLog].GetMethod('OnEvent'))
[System.Windows.Automation.Automation]::AddAutomationPropertyChangedEventHandler($check, [System.Windows.Automation.TreeScope]::Element, $onProperty, [System.Windows.Automation.TogglePattern]::ToggleStateProperty)
[System.Windows.Automation.Automation]::AddAutomationEventHandler([System.Windows.Automation.InvokePattern]::InvokedEvent, $button, [System.Windows.Automation.TreeScope]::Element, $onEvent)

$toggle = $check.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
$watch = [System.Diagnostics.Stopwatch]::StartNew()
$toggle.Toggle()
"toggle|$($toggle.Current.ToggleState)|$($watch.ElapsedMilliseconds)"
$button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()

$deadline = [DateTime]::UtcNow.AddSeconds(5)
$seen = New-Object System.Collections.Generic.List[string]
while ($seen.Count -lt 2 -and [DateTime]::UtcNow -lt $deadline) {
    $item = $null
    if ([NazorEventLog]::Items.TryDequeue([ref]$item)) { $seen.Add($item) } else { Start-Sleep -Milliseconds 20 }
}
$seen
[System.Windows.Automation.Automation]::RemoveAllEventHandlers()
